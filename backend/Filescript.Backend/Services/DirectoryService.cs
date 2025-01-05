using Filescript.Backend.Exceptions;
using Filescript.Backend.Models;
using Filescript.Backend.Utilities;
using Filescript.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Generic;

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
<<<<<<< HEAD
=======
        private ContainerMetadata _metadata;
        private FileIOHelper _fileIOHelper;
        private Superblock _superblock;
>>>>>>> 2c7fc1d452f3d8b7ae4ae8da4de75d31f912fdd3

        public DirectoryService(ILogger<DirectoryService> logger, ContainerManager containerManager, string containerName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _containerManager = containerManager ?? throw new ArgumentNullException(nameof(containerManager));
            _containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));

<<<<<<< HEAD
            // Ensure the container exists
            if (!_containerManager.ListContainers().Contains(_containerName))
            {
                throw new ContainerNotFoundException($"Container '{_containerName}' does not exist.");
            }
        }

        /// <inheritdoc />
        public async Task<bool> MakeDirectoryAsync(string directoryName, string path)
        {
            _logger.LogInformation("DirectoryService: Attempting to create directory '{DirectoryName}' at path '{Path}' in container '{ContainerName}'.", directoryName, path, _containerName);

            var metadata = _containerManager.GetContainer(_containerName);
            var fileIOHelper = _containerManager.GetFileIOHelper(_containerName);
            var superblock = _containerManager.GetSuperblock(_containerName);

            string targetPath = string.IsNullOrWhiteSpace(path) ? metadata.CurrentDirectory : path;

            // Validate target path exists
            if (!DirectoryExists(metadata, targetPath))
            {
                _logger.LogError("DirectoryService: Target path '{TargetPath}' does not exist in container '{ContainerName}'.", targetPath, _containerName);
                throw new Exceptions.DirectoryNotFoundException($"The target path '{targetPath}' does not exist.");
            }
=======
            // Initialize metadata, FileIOHelper, and Superblock
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
        private async void SaveMetadata()
        {
            byte[] metadataBytes = _metadata.Serialize();
            await _fileIOHelper.WriteBlockAsync(_superblock.MetadataStartBlock, metadataBytes);
        }
>>>>>>> 2c7fc1d452f3d8b7ae4ae8da4de75d31f912fdd3

        /// <inheritdoc />
        public async Task<bool> MakeDirectoryAsync(string directoryName, string path)
        {
            _logger.LogInformation("DirectoryService: Creating directory '{DirectoryName}' at path '{Path}' in container '{ContainerName}'.", directoryName, path, _containerName);

            // Validate inputs
            if (string.IsNullOrWhiteSpace(directoryName))
                throw new ArgumentException("Directory name cannot be null or whitespace.", nameof(directoryName));

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));

            // Construct the full path
            string fullPath = System.IO.Path.Combine(path, directoryName).Replace("\\", "/");

            // Check if directory already exists
<<<<<<< HEAD
            if (DirectoryExists(metadata, fullPath))
            {
                _logger.LogWarning("DirectoryService: Directory '{FullPath}' already exists in container '{ContainerName}'.", fullPath, _containerName);
                throw new DirectoryAlreadyExistsException($"The directory '{fullPath}' already exists.");
            }

            // Create directory entry in metadata
            var newDirectory = new DirectoryEntry(directoryName, fullPath);
            metadata.AddDirectory(newDirectory);

            // Update parent directory's subdirectories
            var parentDirectory = GetDirectoryEntry(metadata, targetPath);
            parentDirectory.AddSubDirectory(fullPath);
            // metadata.SaveMetadata(); // Removed this line

            // Serialize and write metadata back to metadata block
            byte[] metadataBytes = metadata.Serialize();
            await fileIOHelper.WriteBlockAsync(superblock.MetadataStartBlock, metadataBytes);

            _logger.LogInformation("DirectoryService: Directory '{FullPath}' created successfully in container '{ContainerName}'.", fullPath, _containerName);
=======
            if (_metadata.Directories.ContainsKey(fullPath))
                throw new DirectoryAlreadyExistsException($"Directory '{fullPath}' already exists.");

            // Create a new DirectoryEntry
            var directoryEntry = new DirectoryEntry(directoryName, fullPath);
            _metadata.Directories.Add(fullPath, directoryEntry);

            // Save metadata
            SaveMetadata();

            _logger.LogInformation("DirectoryService: Directory '{DirectoryName}' created successfully at path '{Path}' in container '{ContainerName}'.", directoryName, path, _containerName);
>>>>>>> 2c7fc1d452f3d8b7ae4ae8da4de75d31f912fdd3
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> ChangeDirectoryAsync(string targetPath)
        {
<<<<<<< HEAD
            _logger.LogInformation("DirectoryService: Attempting to change directory to '{TargetPath}' in container '{ContainerName}'.", targetPath, _containerName);

            var metadata = _containerManager.GetContainer(_containerName);
            var fileIOHelper = _containerManager.GetFileIOHelper(_containerName);
            var superblock = _containerManager.GetSuperblock(_containerName);

            string resolvedPath = ResolvePath(metadata, targetPath);

            if (!DirectoryExists(metadata, resolvedPath))
            {
                _logger.LogError("DirectoryService: Target directory '{ResolvedPath}' does not exist in container '{ContainerName}'.", resolvedPath, _containerName);
                throw new Exceptions.DirectoryNotFoundException($"The directory '{resolvedPath}' does not exist.");
            }

            metadata.CurrentDirectory = resolvedPath;
            // metadata.SaveMetadata(); // Removed this line

            // Serialize and write metadata back to metadata block
            byte[] metadataBytes = metadata.Serialize();
            await fileIOHelper.WriteBlockAsync(superblock.MetadataStartBlock, metadataBytes);

            _logger.LogInformation("DirectoryService: Current directory changed to '{ResolvedPath}' in container '{ContainerName}'.", resolvedPath, _containerName);
=======
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
            SaveMetadata();

            _logger.LogInformation("DirectoryService: Current directory changed to '{TargetPath}' in container '{ContainerName}'.", targetPath, _containerName);
>>>>>>> 2c7fc1d452f3d8b7ae4ae8da4de75d31f912fdd3
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveDirectoryAsync(string directoryName, string path)
        {
<<<<<<< HEAD
            _logger.LogInformation("DirectoryService: Attempting to remove directory '{DirectoryName}' at path '{Path}' in container '{ContainerName}'.", directoryName, path, _containerName);

            var metadata = _containerManager.GetContainer(_containerName);
            var superblock = _containerManager.GetSuperblock(_containerName);
            var fileIOHelper = _containerManager.GetFileIOHelper(_containerName);

            string targetPath = string.IsNullOrWhiteSpace(path) ? metadata.CurrentDirectory : path;
            string fullPath = PathCombine(targetPath, directoryName);

            // Check if directory exists
            if (!DirectoryExists(metadata, fullPath))
            {
                _logger.LogError("DirectoryService: Directory '{FullPath}' does not exist in container '{ContainerName}'.", fullPath, _containerName);
                throw new Exceptions.DirectoryNotFoundException($"The directory '{fullPath}' does not exist.");
            }

            // Check if directory is empty
            var directoryEntry = GetDirectoryEntry(metadata, fullPath);
            if (directoryEntry.SubDirectories.Count > 0 || directoryEntry.Files.Count > 0)
            {
                _logger.LogWarning("DirectoryService: Directory '{FullPath}' is not empty in container '{ContainerName}'.", fullPath, _containerName);
                throw new DirectoryNotEmptyException($"The directory '{fullPath}' is not empty.");
            }

            // Remove directory from metadata
            metadata.RemoveDirectory(fullPath);

            // Update parent directory's subdirectories
            var parentDirectory = GetDirectoryEntry(metadata, targetPath);
            parentDirectory.RemoveSubDirectory(fullPath);
            // metadata.SaveMetadata(); // Removed this line

            // Serialize and write metadata back to metadata block
            byte[] metadataBytes = metadata.Serialize();
            await fileIOHelper.WriteBlockAsync(superblock.MetadataStartBlock, metadataBytes);

            _logger.LogInformation("DirectoryService: Directory '{FullPath}' removed successfully from container '{ContainerName}'.", fullPath, _containerName);
            return true;
        }

        /// <inheritdoc />
        public List<DirectoryEntry> ListDirectories()
        {
            _logger.LogInformation("DirectoryService: Listing directories in container '{ContainerName}'.", _containerName);

            var metadata = _containerManager.GetContainer(_containerName);
            string currentDir = GetCurrentDirectory();
            var currentDirectory = GetDirectoryEntry(metadata, currentDir);

            var subDirs = new List<DirectoryEntry>();
            foreach (var subDirPath in currentDirectory.SubDirectories)
            {
                if (metadata.Directories.TryGetValue(subDirPath, out DirectoryEntry dirEntry))
                {
                    subDirs.Add(dirEntry);
                }
            }

            _logger.LogInformation("DirectoryService: Retrieved {DirectoryCount} directories in container '{ContainerName}'.", subDirs.Count, _containerName);
            return subDirs;
        }

        /// <inheritdoc />
        public string GetCurrentDirectory()
        {
            var metadata = _containerManager.GetContainer(_containerName);
            return metadata.CurrentDirectory;
        }

        /// <inheritdoc />
        public async Task<bool> BasicHealthCheckAsync()
        {
            // Implement a basic health check logic
            _logger.LogInformation("DirectoryService: Performing basic health check for container '{ContainerName}'.", _containerName);

            bool isHealthy = _containerManager != null && _containerManager.GetContainer(_containerName) != null;
            _logger.LogInformation("DirectoryService: Health check result for container '{ContainerName}' - {IsHealthy}.", _containerName, isHealthy);
            return isHealthy;
        }

        /// <summary>
        /// Checks if a directory exists in the metadata.
        /// </summary>
        private bool DirectoryExists(ContainerMetadata metadata, string path)
        {
            return metadata.Directories.ContainsKey(path);
        }

        /// <summary>
        /// Retrieves a directory entry from the metadata.
        /// </summary>
        private DirectoryEntry GetDirectoryEntry(ContainerMetadata metadata, string path)
        {
            if (metadata.Directories.TryGetValue(path, out DirectoryEntry directory))
            {
                return directory;
            }
            else
            {
                throw new Exceptions.DirectoryNotFoundException($"The directory '{path}' does not exist.");
            }
        }

        /// <summary>
        /// Resolves the provided path to an absolute path based on the current directory.
        /// </summary>
        private string ResolvePath(ContainerMetadata metadata, string path)
        {
            if (Path.IsPathRooted(path))
            {
                return path.Replace("\\", "/");
            }
            else
            {
                return PathCombine(metadata.CurrentDirectory, path);
            }
        }

        /// <summary>
        /// Combines two path segments, handling directory separators.
        /// </summary>
        private string PathCombine(string path1, string path2)
        {
            return Path.Combine(path1, path2).Replace("\\", "/");
        }
=======
            _logger.LogInformation("DirectoryService: Removing directory '{DirectoryName}' at path '{Path}' in container '{ContainerName}'.", directoryName, path, _containerName);

            // Construct the full path
            string fullPath = System.IO.Path.Combine(path, directoryName).Replace("\\", "/");

            // Check if directory exists
            if (!_metadata.Directories.ContainsKey(fullPath))
                throw new Exceptions.DirectoryNotFoundException($"Directory '{fullPath}' does not exist.");

            // Remove the directory
            _metadata.Directories.Remove(fullPath);

            // Save metadata
            SaveMetadata();

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
>>>>>>> 2c7fc1d452f3d8b7ae4ae8da4de75d31f912fdd3
    }
}
