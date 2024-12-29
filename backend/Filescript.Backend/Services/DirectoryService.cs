using Filescript.Backend.Exceptions;
using Filescript.Backend.Models;
using Filescript.Backend.Services;
using Filescript.Models;
using Filescript.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Filescript.Services
{
    /// <summary>
    /// Service handling directory operations within the container.
    /// </summary>
    public class DirectoryService : IDirectoryService
    {
        private readonly ILogger<DirectoryService> _logger;
        private readonly ContainerMetadata _metadata;
        private readonly FileIOHelper _fileIOHelper;
        private readonly Superblock _superblock;

        public DirectoryService(ILogger<DirectoryService> logger, ContainerMetadata metadata, FileIOHelper fileIOHelper)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            _fileIOHelper = fileIOHelper ?? throw new ArgumentNullException(nameof(fileIOHelper));

            // Load superblock from block 0
            byte[] superblockData = _fileIOHelper.ReadBlockAsync(0).Result;
            _superblock = Superblock.Deserialize(superblockData);
        }

        public async Task MakeDirectoryAsync(string directoryName, string path)
        {
            _logger.LogInformation("DirectoryService: Attempting to create directory '{DirectoryName}' at path '{Path}'.", directoryName, path);

            string targetPath = string.IsNullOrEmpty(path) ? _metadata.CurrentDirectory : path;

            // Validate target path exists
            if (!DirectoryExists(targetPath))
            {
                _logger.LogError("DirectoryService: Target path '{TargetPath}' does not exist.", targetPath);
                throw new DirectoryNotFoundException($"The target path '{targetPath}' does not exist.");
            }

            string fullPath = PathCombine(targetPath, directoryName);

            // Check if directory already exists
            if (DirectoryExists(fullPath))
            {
                _logger.LogWarning("DirectoryService: Directory '{FullPath}' already exists.", fullPath);
                throw new DirectoryAlreadyExistsException($"The directory '{fullPath}' already exists.");
            }

            // Create directory entry in metadata
            var newDirectory = new DirectoryEntry(fullPath);
            _metadata.AddDirectory(newDirectory);

            // Update parent directory's subdirectories
            var parentDirectory = GetDirectoryEntry(targetPath);
            parentDirectory.AddSubDirectory(fullPath);
            _metadata.SaveMetadata();

            // Serialize and write metadata back to metadata block
            byte[] metadataBytes = _metadata.Serialize();
            await _fileIOHelper.WriteBlockAsync(_superblock.MetadataStartBlock, metadataBytes);

            _logger.LogInformation("DirectoryService: Directory '{FullPath}' created successfully.", fullPath);
        }

        public async Task ChangeDirectoryAsync(string targetPath)
        {
            _logger.LogInformation("DirectoryService: Attempting to change directory to '{TargetPath}'.", targetPath);

            string resolvedPath = ResolvePath(targetPath);

            if (!DirectoryExists(resolvedPath))
            {
                _logger.LogError("DirectoryService: Target directory '{ResolvedPath}' does not exist.", resolvedPath);
                throw new DirectoryNotFoundException($"The directory '{resolvedPath}' does not exist.");
            }

            _metadata.CurrentDirectory = resolvedPath;
            _metadata.SaveMetadata();

            // Serialize and write metadata back to metadata block
            byte[] metadataBytes = _metadata.Serialize();
            await _fileIOHelper.WriteBlockAsync(_superblock.MetadataStartBlock, metadataBytes);

            _logger.LogInformation("DirectoryService: Current directory changed to '{ResolvedPath}'.", resolvedPath);
        }

        public async Task RemoveDirectoryAsync(string directoryName, string path)
        {
            _logger.LogInformation("DirectoryService: Attempting to remove directory '{DirectoryName}' at path '{Path}'.", directoryName, path);

            string targetPath = string.IsNullOrEmpty(path) ? _metadata.CurrentDirectory : path;

            // Validate target path exists
            if (!DirectoryExists(targetPath))
            {
                _logger.LogError("DirectoryService: Target path '{TargetPath}' does not exist.", targetPath);
                throw new DirectoryNotFoundException($"The target path '{targetPath}' does not exist.");
            }

            string fullPath = PathCombine(targetPath, directoryName);

            // Check if directory exists
            if (!DirectoryExists(fullPath))
            {
                _logger.LogError("DirectoryService: Directory '{FullPath}' does not exist.", fullPath);
                throw new DirectoryNotFoundException($"The directory '{fullPath}' does not exist.");
            }

            // Check if directory is empty
            var directoryEntry = GetDirectoryEntry(fullPath);
            if (directoryEntry.SubDirectories.Count > 0 || directoryEntry.Files.Count > 0)
            {
                _logger.LogWarning("DirectoryService: Directory '{FullPath}' is not empty.", fullPath);
                throw new DirectoryNotEmptyException($"The directory '{fullPath}' is not empty.");
            }

            // Remove directory from metadata
            _metadata.RemoveDirectory(fullPath);

            // Update parent directory's subdirectories
            var parentDirectory = GetDirectoryEntry(targetPath);
            parentDirectory.RemoveSubDirectory(fullPath);
            _metadata.SaveMetadata();

            // Serialize and write metadata back to metadata block
            byte[] metadataBytes = _metadata.Serialize();
            await _fileIOHelper.WriteBlockAsync(_superblock.MetadataStartBlock, metadataBytes);

            _logger.LogInformation("DirectoryService: Directory '{FullPath}' removed successfully.", fullPath);
        }

        /// <summary>
        /// Checks if a directory exists in the metadata.
        /// </summary>
        private bool DirectoryExists(string path)
        {
            return _metadata.Directories.ContainsKey(path);
        }

        /// <summary>
        /// Retrieves a directory entry from the metadata.
        /// </summary>
        private DirectoryEntry GetDirectoryEntry(string path)
        {
            if (_metadata.Directories.TryGetValue(path, out DirectoryEntry directory))
            {
                return directory;
            }
            else
            {
                throw new DirectoryNotFoundException($"The directory '{path}' does not exist.");
            }
        }

        /// <summary>
        /// Resolves the provided path to an absolute path based on the current directory.
        /// </summary>
        private string ResolvePath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }
            else
            {
                return PathCombine(_metadata.CurrentDirectory, path);
            }
        }

        /// <summary>
        /// Combines two path segments, handling directory separators.
        /// </summary>
        private string PathCombine(string path1, string path2)
        {
            return Path.Combine(path1, path2).Replace("\\", "/");
        }

        public List<DirectoryEntry> ListDirectories()
        {
            _logger.LogInformation("DirectoryService: Listing directories in the current directory.");

            var directories = new List<DirectoryEntry>();

            foreach (var directory in _metadata.Directories.Values)
            {
                if (directory.Path.StartsWith(_metadata.CurrentDirectory))
                {
                    directories.Add(directory);
                }
            }

            return directories;
        }

        public string GetCurrentDirectory()
        {
            throw new NotImplementedException();
        }

        public Task<bool> BasicHealthCheckAsync()
        {
            throw new NotImplementedException();
        }
    }
}
