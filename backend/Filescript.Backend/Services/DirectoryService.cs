using Filescript.Backend.Exceptions;
using Filescript.Backend.Models;
using Filescript.Backend.Utilities;
using Filescript.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Filescript.Backend.Services
{
    /// <summary>
    /// Service handling directory operations within a specified container.
    /// </summary>
    public class DirectoryService : IDirectoryService
    {
        private readonly ILogger<DirectoryService> _logger;
        private readonly ContainerManager _containerManager;
        private readonly string _containerName;
        private ContainerMetadata _metadata;
        private FileIOHelper _fileIOHelper;
        private Superblock _superblock;

        public DirectoryService(
            ILogger<DirectoryService> logger,
            ContainerManager containerManager,
            string containerName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _containerManager = containerManager ?? throw new ArgumentNullException(nameof(containerManager));
            _containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
            InitializeMetadata();
        }

        /// <summary>
        /// Initializes metadata, FileIOHelper, and Superblock for the container.
        /// </summary>
        private async void InitializeMetadata()
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
        private async Task SaveMetadata()
        {
            try 
            {
                byte[] metadataBytes = _metadata.Serialize();
                
                // Create padded array
                byte[] paddedMetadataBytes = new byte[_superblock.BlockSize];
                if (metadataBytes.Length > _superblock.BlockSize)
                {
                    _logger.LogError("Serialized metadata size ({Size} bytes) exceeds block size ({BlockSize} bytes).", 
                        metadataBytes.Length, _superblock.BlockSize);
                    throw new InvalidOperationException($"Metadata size ({metadataBytes.Length} bytes) exceeds block size ({_superblock.BlockSize} bytes)");
                }

                // Copy metadata bytes to padded array
                Array.Copy(metadataBytes, paddedMetadataBytes, metadataBytes.Length);
                
                await _fileIOHelper.WriteBlockAsync(_superblock.MetadataStartBlock, paddedMetadataBytes);
                _logger.LogDebug("Directory metadata saved successfully. Size: {Size} bytes", metadataBytes.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save directory metadata");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> MakeDirectoryAsync(string directoryName, string path)
        {
            _logger.LogInformation("DirectoryService: Creating directory '{DirectoryName}' at path '{Path}' in container '{ContainerName}'.", 
                directoryName, path, _containerName);

            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(directoryName))
                    throw new ArgumentException("Directory name cannot be null or whitespace.", nameof(directoryName));

                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));

                // Construct the full path
                string fullPath = System.IO.Path.Combine(path, directoryName).Replace("\\", "/");

                // Check if directory already exists
                if (_metadata.Directories.ContainsKey(fullPath))
                    throw new DirectoryAlreadyExistsException($"Directory '{fullPath}' already exists.");

                // Create a new DirectoryEntry
                var directoryEntry = new DirectoryEntry(directoryName, fullPath);
                _metadata.Directories.Add(fullPath, directoryEntry);

                // Save metadata
                await SaveMetadata();

                _logger.LogInformation("DirectoryService: Directory '{DirectoryName}' created successfully at path '{Path}' in container '{ContainerName}'.", 
                    directoryName, path, _containerName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DirectoryService: Failed to create directory '{DirectoryName}' at path '{Path}' in container '{ContainerName}'", 
                    directoryName, path, _containerName);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ChangeDirectoryAsync(string targetPath)
        {
            _logger.LogInformation("DirectoryService: Changing current directory to '{TargetPath}' in container '{ContainerName}'.", targetPath, _containerName);

            // Validate input
            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException("Target path cannot be null or whitespace.", nameof(targetPath));

            // Check if target directory exists
            if (!_metadata.Directories.ContainsKey(targetPath))
                throw new Exceptions.DirectoryNotFoundException($"Directory '{targetPath}' does not exist.");

            // Update current directory
            _metadata.CurrentDirectory = targetPath;

            // Save metadata
            await SaveMetadata();

            _logger.LogInformation("DirectoryService: Current directory changed to '{TargetPath}' in container '{ContainerName}'.", targetPath, _containerName);
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveDirectoryAsync(string directoryName, string path)
        {
            _logger.LogInformation("DirectoryService: Removing directory '{DirectoryName}' at path '{Path}' in container '{ContainerName}'.", directoryName, path, _containerName);

            // Construct the full path
            string fullPath = System.IO.Path.Combine(path, directoryName).Replace("\\", "/");

            // Check if directory exists
            if (!_metadata.Directories.ContainsKey(fullPath))
                throw new Exceptions.DirectoryNotFoundException($"Directory '{fullPath}' does not exist.");

            // Remove the directory
            _metadata.Directories.Remove(fullPath);

            // Save metadata
            await SaveMetadata();

            _logger.LogInformation("DirectoryService: Directory '{DirectoryName}' removed successfully from path '{Path}' in container '{ContainerName}'.", directoryName, path, _containerName);
            return true;
        }

        /// <inheritdoc />
        public List<DirectoryEntry> ListDirectories()
        {
            _logger.LogInformation("DirectoryService: Listing directories in container '{ContainerName}'.", _containerName);

            return new List<DirectoryEntry>(_metadata.Directories.Values);
        }

        /// <inheritdoc />
        public string GetCurrentDirectory()
        {
            _logger.LogInformation("DirectoryService: Getting current directory in container '{ContainerName}'.", _containerName);

            return _metadata.CurrentDirectory;
        }

        /// <inheritdoc />
        public async Task<bool> BasicHealthCheckAsync()
        {
            _logger.LogInformation("DirectoryService: Performing basic health check on container '{ContainerName}'.", _containerName);

            // Implement health check logic as needed
            // For example, verify that metadata is consistent

            // Placeholder: Assume health check passes
            return true;
        }
    }
}