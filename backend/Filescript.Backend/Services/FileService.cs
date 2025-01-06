using Filescript.Backend.Exceptions;
using Filescript.Backend.Models;
using Filescript.Backend.Services;
using Filescript.Backend.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Filescript.Backend.Services
{
    /// <summary>
    /// Service handling file operations within a specified container.
    /// </summary>
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;
        private readonly ContainerManager _containerManager;
        private string _containerName;
        
        // Local references after initialization
        private ContainerMetadata _metadata;
        private FileIOHelper _fileIOHelper;
        private Superblock _superblock;

        // Constructor for DI
        public FileService(ILogger<FileService> logger, ContainerManager containerManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _containerManager = containerManager ?? throw new ArgumentNullException(nameof(containerManager));
        }

        /// <summary>
        /// Method to initialize container-specific data (metadata, file IO, etc.)
        /// </summary>
        public void Initialize(string containerName)
        {
            _containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
            InitializeMetadataAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Initializes metadata, FileIOHelper, and Superblock for the container.
        /// Delegates multi-block metadata loading to ContainerManager.LoadMetadataAsync().
        /// </summary>
        private async Task InitializeMetadataAsync()
        {
            // Retrieve the basic references from ContainerManager
            _metadata = _containerManager.GetContainer(_containerName);
            _fileIOHelper = _containerManager.GetFileIOHelper(_containerName);
            _superblock = _containerManager.GetSuperblock(_containerName);

            // Instead of reading from a single block, 
            // ask ContainerManager to load all metadata (multi-block).
            _metadata = await _containerManager.LoadMetadataAsync(_containerName);

            _logger.LogInformation(
                "FileService: Metadata initialized for container '{ContainerName}'. " +
                "Blocks={TotalBlocks}, BlockSize={BlockSize}.",
                _containerName, _metadata.TotalBlocks, _metadata.BlockSize
            );
        }

        /// <summary>
        /// Saves the current state of metadata to the container (multi-block or single-block),
        /// by delegating to ContainerManager.
        /// </summary>
        private async Task SaveMetadataAsync()
        {
            // ContainerManager handles the logic of splitting or single-block writing
            await _containerManager.SaveMetadataAsync(_containerName);
        }

        /// <inheritdoc />
        public async Task<bool> CreateFileAsync(string fileName, string path, byte[] content)
        {
            _logger.LogInformation(
                "FileService: Creating file '{FileName}' at path '{Path}' in container '{ContainerName}'.",
                fileName, path, _containerName
            );

            // Validate inputs
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or whitespace.", nameof(fileName));

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));

            if (content == null || content.Length == 0)
                throw new ArgumentException("Content cannot be null or empty.", nameof(content));

            try
            {
                // Construct the full internal path
                string fullPath = Path.Combine(path, fileName).Replace("\\", "/");

                // Check if file already exists
                if (_metadata.Files.ContainsKey(fullPath))
                    throw new FileAlreadyExistsException($"File '{fullPath}' already exists.");

                // Calculate how many blocks are needed
                int requiredBlocks = (int)Math.Ceiling((double)content.Length / _superblock.BlockSize);

                // Allocate blocks
                var blockIndices = new List<int>();
                for (int i = 0; i < requiredBlocks; i++)
                {
                    blockIndices.Add(_metadata.AllocateBlock());
                }

                // Write content to those blocks
                for (int i = 0; i < requiredBlocks; i++)
                {
                    // Each block is exactly _superblock.BlockSize in length
                    byte[] blockData = new byte[_superblock.BlockSize];

                    // Determine how many bytes go into this block
                    int startIndex = i * _superblock.BlockSize;
                    int bytesToCopy = Math.Min(_superblock.BlockSize, content.Length - startIndex);

                    if (bytesToCopy > 0)
                    {
                        Array.Copy(content, startIndex, blockData, 0, bytesToCopy);
                    }

                    // Write this block to container
                    await _fileIOHelper.WriteBlockAsync(blockIndices[i], blockData);
                }

                // Create a new FileEntry
                var fileEntry = new FileEntry(
                    fileName, 
                    fullPath, 
                    blockIndices[0], 
                    requiredBlocks
                )
                {
                    Length = content.Length // actual content length in bytes
                };
                _metadata.Files.Add(fullPath, fileEntry);

                // Save (multi-block) metadata
                await SaveMetadataAsync();

                _logger.LogInformation(
                    "FileService: File '{FileName}' created successfully at path '{Path}' in container '{ContainerName}'.",
                    fileName, path, _containerName
                );
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "FileService: Failed to create file '{FileName}' at path '{Path}' in container '{ContainerName}'",
                    fileName, path, _containerName
                );
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<byte[]> ReadFileAsync(string fileName, string path)
        {
            _logger.LogInformation(
                "FileService: Reading file '{FileName}' at path '{Path}' in container '{ContainerName}'.",
                fileName, path, _containerName
            );

            string fullPath = Path.Combine(path, fileName).Replace("\\", "/");

            if (!_metadata.Files.ContainsKey(fullPath))
                throw new Exceptions.FileNotFoundException($"File '{fullPath}' does not exist.");

            var fileEntry = _metadata.Files[fullPath];

            // We'll read each block and accumulate the data in memory.
            // If you must avoid loading the entire file in memory, 
            // you'd do a chunk-based approach here as well.
            byte[] content = new byte[fileEntry.Length];
            int bytesCopied = 0;
            
            // We'll figure out how many blocks we need to read
            // (fileEntry.BlockCount). 
            // But some code uses 'fileEntry.Length' as blockCount. 
            // Double-check your design; we assume 'fileEntry.BlockCount' 
            // is the # of blocks. 
            int blockCount = fileEntry.BlockCount;

            for (int i = 0; i < blockCount; i++)
            {
                int currentBlockIndex = fileEntry.StartBlock + i;
                byte[] blockData = await _fileIOHelper.ReadBlockAsync(currentBlockIndex);

                // The last block might only have partial data
                int remaining = fileEntry.Length - bytesCopied;
                int bytesToCopy = Math.Min(remaining, _superblock.BlockSize);

                Array.Copy(blockData, 0, content, bytesCopied, bytesToCopy);
                bytesCopied += bytesToCopy;

                if (bytesCopied >= fileEntry.Length)
                    break;
            }

            _logger.LogInformation(
                "FileService: File '{FileName}' read successfully from path '{Path}' in container '{ContainerName}'.",
                fileName, path, _containerName
            );
            return content;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteFileAsync(string fileName, string path)
        {
            _logger.LogInformation(
                "FileService: Deleting file '{FileName}' at path '{Path}' in container '{ContainerName}'.",
                fileName, path, _containerName
            );

            string fullPath = Path.Combine(path, fileName).Replace("\\", "/");

            if (!_metadata.Files.ContainsKey(fullPath))
                throw new Exceptions.FileNotFoundException($"File '{fullPath}' does not exist.");

            var fileEntry = _metadata.Files[fullPath];

            // Free allocated blocks
            int blockCount = fileEntry.BlockCount;
            for (int i = 0; i < blockCount; i++)
            {
                int blockIndex = fileEntry.StartBlock + i;
                _metadata.FreeBlock(blockIndex);
            }

            // Remove file entry
            _metadata.Files.Remove(fullPath);

            // Save updated metadata
            await SaveMetadataAsync();

            _logger.LogInformation(
                "FileService: File '{FileName}' deleted successfully from path '{Path}' in container '{ContainerName}'.",
                fileName, path, _containerName
            );
            return true;
        }

        /// <summary>
        /// Performs a basic health check on the container.
        /// </summary>
        public async Task<bool> BasicHealthCheckAsync()
        {
            _logger.LogInformation("FileService: Performing basic health check.");

            try
            {
                // If not initialized, that might just mean no container name set
                if (_containerName == null)
                {
                    return true;
                }

                // Ensure metadata is loaded
                if (_metadata == null)
                {
                    _logger.LogError("FileService: Metadata is null.");
                    return false;
                }

                // Check if the container file physically exists
                if (!File.Exists(_fileIOHelper.GetContainerFilePath()))
                {
                    _logger.LogError(
                        "FileService: Container file does not exist at {Path}.",
                        _fileIOHelper.GetContainerFilePath()
                    );
                    return false;
                }

                _logger.LogInformation("FileService: Basic health check passed.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FileService: Exception during basic health check.");
                return false;
            }
        }
    }
}
