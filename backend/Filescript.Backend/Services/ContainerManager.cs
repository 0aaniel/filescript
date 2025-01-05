using Filescript.Backend.Exceptions;
using Filescript.Backend.Models;
using Filescript.Backend.Utilities;
using Filescript.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Filescript.Backend.Services
{
    /// <summary>
    /// Manages multiple file system containers.
    /// </summary>
    public class ContainerManager
    {
        private readonly ILogger<ContainerManager> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ConcurrentDictionary<string, ContainerMetadata> _containers;
        private readonly ConcurrentDictionary<string, FileIOHelper> _fileIOHelpers;
        private readonly ConcurrentDictionary<string, Superblock> _superblocks;
        private readonly ConcurrentDictionary<string, DirectoryService> _directoryServices;
        private readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerManager"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for logging.</param>
        /// <param name="loggerFactory">Factory to create loggers for other classes.</param>
        public ContainerManager(ILogger<ContainerManager> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _containers = new ConcurrentDictionary<string, ContainerMetadata>();
            _fileIOHelpers = new ConcurrentDictionary<string, FileIOHelper>();
            _superblocks = new ConcurrentDictionary<string, Superblock>();
            _directoryServices = new ConcurrentDictionary<string, DirectoryService>();
        }

        /// <summary>
        /// Creates a new container with the specified name and file path.
        /// </summary>
        public async Task<bool> CreateContainerAsync(string containerName, string containerFilePath, int totalBlocks, int blockSize = 4096)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("Container name cannot be null or whitespace.", nameof(containerName));

            if (string.IsNullOrWhiteSpace(containerFilePath))
                throw new ArgumentException("Container file path cannot be null or whitespace.", nameof(containerFilePath));

            if (_containers.ContainsKey(containerName))
                throw new ContainerAlreadyExistsException($"A container with the name '{containerName}' already exists.");

            lock (_lock)
            {
                if (_containers.ContainsKey(containerName))
                    throw new ContainerAlreadyExistsException($"A container with the name '{containerName}' already exists.");

                // Create a separate logger for FileIOHelper
                var fileIOLogger = _loggerFactory.CreateLogger<FileIOHelper>();

                // Initialize FileIOHelper for the new container
                var newFileIOHelper = new FileIOHelper(fileIOLogger, containerFilePath, blockSize);
                _fileIOHelpers.TryAdd(containerName, newFileIOHelper);

                // Initialize ContainerMetadata
                var metadata = new ContainerMetadata(totalBlocks, blockSize)
                {
                    ContainerFilePath = containerFilePath
                };
                _containers.TryAdd(containerName, metadata);

                // Initialize Superblock with required parameters
                var superblock = new Superblock(totalBlocks, blockSize)
                {
                    MetadataStartBlock = 1 // Assuming block 0 is used for some other purpose
                };
                _superblocks.TryAdd(containerName, superblock);

                // Initialize the container file
                newFileIOHelper.InitializeContainerAsync(totalBlocks).GetAwaiter().GetResult();

                // Serialize and write metadata to the container's metadata block
                byte[] metadataBytes = metadata.Serialize();
                newFileIOHelper.WriteBlockAsync(superblock.MetadataStartBlock, metadataBytes).Wait();

                // Create a separate logger for DirectoryService
                var directoryServiceLogger = _loggerFactory.CreateLogger<DirectoryService>();

                // Initialize DirectoryService for the container with the correct logger
                var directoryService = new DirectoryService(directoryServiceLogger, this, containerName);
                _directoryServices.TryAdd(containerName, directoryService);

                _logger.LogInformation("ContainerManager: Container '{ContainerName}' created successfully at '{ContainerFilePath}'.", containerName, containerFilePath);
                return true;
            }
        }

        /// <summary>
        /// Creates a new file within a specified container and path.
        /// </summary>
        public async Task<bool> CreateFileAsync(string containerName, string fileName, string path, byte[] content)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("Container name cannot be null or whitespace.", nameof(containerName));

            var fileService = GetFileService(containerName);
            return await fileService.CreateFileAsync(fileName, path, content);
        }

        /// <summary>
        /// Retrieves the FileService for the specified container.
        /// </summary>
        private FileService GetFileService(string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("Container name cannot be null or whitespace.", nameof(containerName));

            if (_fileIOHelpers.TryGetValue(containerName, out FileIOHelper helper))
            {
                if (_containers.TryGetValue(containerName, out ContainerMetadata metadata) &&
                    _superblocks.TryGetValue(containerName, out Superblock superblock))
                {
                    // Create a separate logger for FileService
                    var fileServiceLogger = _loggerFactory.CreateLogger<FileService>();

                    // Initialize FileService with necessary dependencies
                    var fileService = new FileService(fileServiceLogger, this, containerName);
                    return fileService;
                }
                else
                {
                    throw new ContainerNotFoundException($"Metadata or Superblock for container '{containerName}' does not exist.");
                }
            }
            else
            {
                throw new ContainerNotFoundException($"FileIOHelper for container '{containerName}' does not exist.");
            }
        }

        /// <summary>
        /// Lists all existing containers.
        /// </summary>
        public List<string> ListContainers()
        {
            return new List<string>(_containers.Keys);
        }

        /// <summary>
        /// Retrieves the metadata for the specified container.
        /// </summary>
        public ContainerMetadata GetContainer(string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("Container name cannot be null or whitespace.", nameof(containerName));

            if (_containers.TryGetValue(containerName, out ContainerMetadata metadata))
            {
                return metadata;
            }
            else
            {
                throw new ContainerNotFoundException($"Container '{containerName}' does not exist.");
            }
        }

        /// <summary>
        /// Retrieves the FileIOHelper for the specified container.
        /// </summary>
        public FileIOHelper GetFileIOHelper(string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("Container name cannot be null or whitespace.", nameof(containerName));

            if (_fileIOHelpers.TryGetValue(containerName, out FileIOHelper helper))
            {
                return helper;
            }
            else
            {
                throw new ContainerNotFoundException($"FileIOHelper for container '{containerName}' does not exist.");
            }
        }

        /// <summary>
        /// Retrieves the Superblock for the specified container.
        /// </summary>
        public Superblock GetSuperblock(string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("Container name cannot be null or whitespace.", nameof(containerName));

            if (_superblocks.TryGetValue(containerName, out Superblock superblock))
            {
                return superblock;
            }
            else
            {
                throw new ContainerNotFoundException($"Superblock for container '{containerName}' does not exist.");
            }
        }

        /// <summary>
        /// Retrieves the DirectoryService for the specified container.
        /// </summary>
        public DirectoryService GetDirectoryService(string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("Container name cannot be null or whitespace.", nameof(containerName));

            if (_directoryServices.TryGetValue(containerName, out DirectoryService service))
            {
                return service;
            }
            else
            {
                throw new ContainerNotFoundException($"DirectoryService for container '{containerName}' does not exist.");
            }
        }

        /// <summary>
        /// Deletes the specified container.
        /// </summary>
        public bool DeleteContainer(string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("Container name cannot be null or whitespace.", nameof(containerName));

            if (_containers.TryRemove(containerName, out ContainerMetadata metadata) &&
                _fileIOHelpers.TryRemove(containerName, out FileIOHelper helper) &&
                _superblocks.TryRemove(containerName, out Superblock superblock) &&
                _directoryServices.TryRemove(containerName, out DirectoryService service))
            {
                // Optionally, delete the container file from the filesystem
                // System.IO.File.Delete(metadata.ContainerFilePath);
                _logger.LogInformation("ContainerManager: Container '{ContainerName}' deleted successfully.", containerName);
                return true;
            }
            else
            {
                throw new ContainerNotFoundException($"Container '{containerName}' does not exist.");
            }
        }

        /// <summary>
        /// Creates a directory within the specified container.
        /// </summary>
        public async Task<bool> CreateDirectoryAsync(string containerName, string directoryName, string path)
        {
            var directoryService = GetDirectoryService(containerName);
            return await directoryService.MakeDirectoryAsync(directoryName, path);
        }

        /// <summary>
        /// Changes the current directory within the specified container.
        /// </summary>
        public async Task<bool> ChangeDirectoryAsync(string containerName, string targetDirectory)
        {
            var directoryService = GetDirectoryService(containerName);
            return await directoryService.ChangeDirectoryAsync(targetDirectory);
        }

        /// <summary>
        /// Removes a directory within the specified container.
        /// </summary>
        public async Task<bool> RemoveDirectoryAsync(string containerName, string directoryName, string path)
        {
            var directoryService = GetDirectoryService(containerName);
            return await directoryService.RemoveDirectoryAsync(directoryName, path);
        }

        /// <summary>
        /// Lists all directories within the specified container.
        /// </summary>
        public List<DirectoryEntry> ListDirectories(string containerName)
        {
            var directoryService = GetDirectoryService(containerName);
            return directoryService.ListDirectories();
        }

        /// <summary>
        /// Gets the current directory of the specified container.
        /// </summary>
        public string GetCurrentDirectory(string containerName)
        {
            var directoryService = GetDirectoryService(containerName);
            return directoryService.GetCurrentDirectory();
        }

        /// <summary>
        /// Performs a basic health check on the specified container.
        /// </summary>
        public async Task<bool> BasicHealthCheckAsync(string containerName)
        {
            var directoryService = GetDirectoryService(containerName);
            return await directoryService.BasicHealthCheckAsync();
        }
    }
}
