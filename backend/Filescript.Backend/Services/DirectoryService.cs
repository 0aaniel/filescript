using Filescript.Backend.Exceptions;
using Filescript.Backend.Models;
using Filescript.Backend.Utilities;
using Filescript.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Filescript.Backend.Services
{
    /// <summary>
    /// Service handling directory operations within a specified container,
    /// using multi-block metadata via ContainerManager.
    /// </summary>
    public class DirectoryService : IDirectoryService
    {
        private readonly ILogger<DirectoryService> _logger;
        private readonly ContainerManager _containerManager;
        private readonly string _containerName;

        // These references get set once we load the metadata
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

            // Initialize metadata synchronously. 
            // (In a real app, you'd want to avoid async calls in constructor.)
            InitializeMetadata();
        }

        /// <summary>
        /// Initializes metadata, FileIOHelper, and Superblock for the container by calling ContainerManager.
        /// </summary>
        private void InitializeMetadata()
        {
            // Retrieve in-memory references
            _metadata = _containerManager.GetContainer(_containerName);
            _fileIOHelper = _containerManager.GetFileIOHelper(_containerName);
            _superblock = _containerManager.GetSuperblock(_containerName);

            // Now load the latest multi-block metadata from the container. 
            // This is an async call, but we'll block to keep constructor synchronous.
            // Alternatively, you could refactor to use an async factory pattern.
            try
            {
                _metadata = _containerManager.LoadMetadataAsync(_containerName)
                    .GetAwaiter().GetResult(); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading metadata in DirectoryService constructor.");
                throw;
            }
        }

        /// <summary>
        /// Saves the current state of metadata by delegating to ContainerManager (multi-block),
        /// instead of manually writing blocks ourselves.
        /// </summary>
        private async Task SaveMetadata()
        {
            try
            {
                // Delegates the entire multi-block write to ContainerManager.
                await _containerManager.SaveMetadataAsync(_containerName);

                _logger.LogDebug("DirectoryService: Metadata saved successfully for container '{ContainerName}'.",
                    _containerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DirectoryService: Failed to save directory metadata for '{ContainerName}'.", 
                    _containerName);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> MakeDirectoryAsync(string directoryName, string path)
        {
            _logger.LogInformation(
                "DirectoryService: Creating directory '{DirectoryName}' at path '{Path}' in container '{ContainerName}'.",
                directoryName, path, _containerName
            );

            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(directoryName))
                    throw new ArgumentException("Directory name cannot be null or whitespace.", nameof(directoryName));

                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));

                // Normalize path
                path = path.Replace("\\", "/");
                if (!path.StartsWith("/"))
                    path = "/" + path;
                if (!path.EndsWith("/"))
                    path += "/";

                // Construct the full path for the new directory
                string fullPath = (path + directoryName).Replace("//", "/");

                // Check if directory already exists
                if (_metadata.Directories.ContainsKey(fullPath))
                    throw new DirectoryAlreadyExistsException($"Directory '{fullPath}' already exists.");

                // Create a new DirectoryEntry
                var directoryEntry = new DirectoryEntry(directoryName, fullPath);
                _metadata.Directories.Add(fullPath, directoryEntry);

                // Find parent directory
                string parentPath = path;
                if (_metadata.Directories.TryGetValue(parentPath, out DirectoryEntry parentDir))
                {
                    // Add this directory to parent's subdirectories
                    if (!parentDir.SubDirectories.Contains(fullPath))
                    {
                        parentDir.SubDirectories.Add(fullPath);
                    }
                }
                else
                {
                    _logger.LogWarning("Parent directory '{ParentPath}' not found. Creating directory without parent.", parentPath);
                }

                // Save updated metadata
                await SaveMetadata();

                _logger.LogInformation(
                    "DirectoryService: Directory '{DirectoryName}' created successfully at path '{Path}' in container '{ContainerName}'.",
                    directoryName, path, _containerName
                );
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "DirectoryService: Failed to create directory '{DirectoryName}' at path '{Path}' in container '{ContainerName}'",
                    directoryName, path, _containerName
                );
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ChangeDirectoryAsync(string targetPath)
        {
            _logger.LogInformation(
                "DirectoryService: Changing current directory to '{TargetPath}' in container '{ContainerName}'.",
                targetPath, _containerName
            );

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

            _logger.LogInformation(
                "DirectoryService: Current directory changed to '{TargetPath}' in container '{ContainerName}'.",
                targetPath, _containerName
            );
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveDirectoryAsync(string directoryName, string path)
        {
            _logger.LogInformation(
                "DirectoryService: Removing directory '{DirectoryName}' at path '{Path}' in container '{ContainerName}'.",
                directoryName, path, _containerName
            );

            // Construct the full path
            string fullPath = Path.Combine(path, directoryName).Replace("\\", "/");

            // Check if directory exists
            if (!_metadata.Directories.ContainsKey(fullPath))
                throw new Exceptions.DirectoryNotFoundException($"Directory '{fullPath}' does not exist.");

            // Remove the directory
            _metadata.Directories.Remove(fullPath);

            // Optionally, remove it from its parent's subdirectories (if you track that):
            // (Similar logic to MakeDirectoryAsync's "Add" approach.)

            // Save metadata
            await SaveMetadata();

            _logger.LogInformation(
                "DirectoryService: Directory '{DirectoryName}' removed successfully from path '{Path}' in container '{ContainerName}'.",
                directoryName, path, _containerName
            );
            return true;
        }

        /// <inheritdoc />
        public List<DirectoryEntry> ListDirectories()
        {
            _logger.LogInformation(
                "DirectoryService: Listing directories in container '{ContainerName}'.",
                _containerName
            );
            return new List<DirectoryEntry>(_metadata.Directories.Values);
        }

        public List<FileEntry> ListFiles()
        {
            _logger.LogInformation(
                "DirectoryService: Listing files in container '{ContainerName}'.",
                _containerName
            );
            return new List<FileEntry>(_metadata.Files.Values);
        }

        /// <inheritdoc />
        public string GetCurrentDirectory()
        {
            _logger.LogInformation(
                "DirectoryService: Getting current directory in container '{ContainerName}'.",
                _containerName
            );
            return _metadata.CurrentDirectory;
        }

        /// <inheritdoc />
        public async Task<bool> BasicHealthCheckAsync()
        {
            _logger.LogInformation(
                "DirectoryService: Performing basic health check on container '{ContainerName}'.",
                _containerName
            );

            // Implement additional health checks if needed
            return await Task.FromResult(true);
        }
    }
}
