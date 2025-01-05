using Filescript.Backend.Exceptions;
using Filescript.Backend.Models;
using Filescript.Backend.Services;
using Filescript.Backend.Utilities;
using Filescript.Models;
using Microsoft.Extensions.Logging;
using System;
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
        private readonly string _containerName;
        private ContainerMetadata _metadata;
        private FileIOHelper _fileIOHelper;
        private Superblock _superblock;

        public FileService(ILogger<FileService> logger, ContainerManager containerManager, string containerName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _containerManager = containerManager ?? throw new ArgumentNullException(nameof(containerManager));
            _containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));

            // Initialize metadata, FileIOHelper, and Superblock
            InitializeMetadataAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Initializes metadata, FileIOHelper, and Superblock for the container.
        /// </summary>
        private async Task InitializeMetadataAsync()
        {
            // Retrieve metadata, FileIOHelper, and Superblock from ContainerManager
            _metadata = _containerManager.GetContainer(_containerName);
            _fileIOHelper = _containerManager.GetFileIOHelper(_containerName);
            _superblock = _containerManager.GetSuperblock(_containerName);

            // Load metadata from the metadata block
            byte[] metadataBytes = await _fileIOHelper.ReadBlockAsync(_superblock.MetadataStartBlock);
            _metadata = ContainerMetadata.Deserialize(metadataBytes);
        }

        /// <summary>
        /// Saves the current state of metadata to the metadata block.
        /// </summary>
        private async Task SaveMetadataAsync()
        {
            byte[] metadataBytes = _metadata.Serialize();
            await _fileIOHelper.WriteBlockAsync(_superblock.MetadataStartBlock, metadataBytes);
        }

        /// <inheritdoc />
        public async Task<bool> CreateFileAsync(string fileName, string path, byte[] content)
        {
            _logger.LogInformation("FileService: Creating file '{FileName}' at path '{Path}' in container '{ContainerName}'.", fileName, path, _containerName);

            // Validate inputs
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or whitespace.", nameof(fileName));

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));

            if (content == null || content.Length == 0)
                throw new ArgumentException("Content cannot be null or empty.", nameof(content));

            // Construct the full path
            string fullPath = System.IO.Path.Combine(path, fileName).Replace("\\", "/");

            // Check if file already exists
            if (_metadata.Files.ContainsKey(fullPath))
                throw new FileAlreadyExistsException($"File '{fullPath}' already exists.");

            // Allocate necessary blocks
            int requiredBlocks = (int)Math.Ceiling((double)content.Length / _superblock.BlockSize);
            int startBlock = _metadata.AllocateBlock();

            // Write content to blocks
            for (int i = 0; i < requiredBlocks; i++)
            {
                int blockIndex = startBlock + i;
                int bytesToWrite = Math.Min(_superblock.BlockSize, content.Length - (i * _superblock.BlockSize));
                byte[] blockData = new byte[_superblock.BlockSize];
                Array.Copy(content, i * _superblock.BlockSize, blockData, 0, bytesToWrite);
                await _fileIOHelper.WriteBlockAsync(blockIndex, blockData);
            }

            // Create a new FileEntry
            var fileEntry = new FileEntry(fileName, fullPath, startBlock, requiredBlocks);
            _metadata.Files.Add(fullPath, fileEntry);

            // Save metadata
            await SaveMetadataAsync();

            _logger.LogInformation("FileService: File '{FileName}' created successfully at path '{Path}' in container '{ContainerName}'.", fileName, path, _containerName);
            return true;
        }

        /// <inheritdoc />
        public async Task<byte[]> ReadFileAsync(string fileName, string path)
        {
            _logger.LogInformation("FileService: Reading file '{FileName}' at path '{Path}' in container '{ContainerName}'.", fileName, path, _containerName);

            string fullPath = System.IO.Path.Combine(path, fileName).Replace("\\", "/");

            if (!_metadata.Files.ContainsKey(fullPath))
                throw new Exceptions.FileNotFoundException($"File '{fullPath}' does not exist.");

            var fileEntry = _metadata.Files[fullPath];
            byte[] content = new byte[fileEntry.Length * _superblock.BlockSize];
            int bytesRead = 0;

            for (int i = 0; i < fileEntry.Length; i++)
            {
                int blockIndex = fileEntry.StartBlock + i;
                byte[] blockData = await _fileIOHelper.ReadBlockAsync(blockIndex);
                Array.Copy(blockData, 0, content, bytesRead, _superblock.BlockSize);
                bytesRead += _superblock.BlockSize;
            }

            // Trim any excess bytes
            Array.Resize(ref content, bytesRead);

            _logger.LogInformation("FileService: File '{FileName}' read successfully from path '{Path}' in container '{ContainerName}'.", fileName, path, _containerName);
            return content;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteFileAsync(string fileName, string path)
        {
            _logger.LogInformation("FileService: Deleting file '{FileName}' at path '{Path}' in container '{ContainerName}'.", fileName, path, _containerName);

            string fullPath = System.IO.Path.Combine(path, fileName).Replace("\\", "/");

            if (!_metadata.Files.ContainsKey(fullPath))
                throw new Exceptions.FileNotFoundException($"File '{fullPath}' does not exist.");

            var fileEntry = _metadata.Files[fullPath];

            // Free allocated blocks
            for (int i = 0; i < fileEntry.Length; i++)
            {
                int blockIndex = fileEntry.StartBlock + i;
                _metadata.FreeBlock(blockIndex);
            }

            // Remove file entry from metadata
            _metadata.Files.Remove(fullPath);

            // Save metadata
            await SaveMetadataAsync();

            _logger.LogInformation("FileService: File '{FileName}' deleted successfully from path '{Path}' in container '{ContainerName}'.", fileName, path, _containerName);
            return true;
        }

        public async Task<bool> BasicHealthCheckAsync()
        {
            _logger.LogInformation("FileService: Performing basic health check.");

            try
            {
                // Example health check: Verify if metadata is loaded and accessible
                if (_metadata == null)
                {
                    _logger.LogError("FileService: Metadata is null.");
                    return false;
                }

                // Additional checks can be added here, such as verifying container file accessibility
                if (!File.Exists(_fileIOHelper.GetContainerFilePath()))
                {
                    _logger.LogError("FileService: Container file does not exist at {Path}.", _fileIOHelper.GetContainerFilePath());
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