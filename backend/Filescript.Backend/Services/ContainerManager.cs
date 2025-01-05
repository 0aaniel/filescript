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

        public ContainerManager(ILogger<ContainerManager> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _containers = new ConcurrentDictionary<string, ContainerMetadata>();
            _fileIOHelpers = new ConcurrentDictionary<string, FileIOHelper>();
            _superblocks = new ConcurrentDictionary<string, Superblock>();
            _directoryServices = new ConcurrentDictionary<string, DirectoryService>();
        }

        private bool IsFileLocked(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogDebug("File '{FilePath}' does not exist. Skipping file lock check.", filePath);
                    return false;
                }

                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
                
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }
        /// <summary>
        /// Creates a new container with the specified name and file path.
        /// </summary>
        public async Task<bool> CreateContainerAsync(string containerName, string containerFilePath, int totalBlocks = 1, int blockSize = 4096)
        {
            const int MinBlockSize = 512; // Define a minimum block size

            _logger.LogInformation("Starting container creation: Name={ContainerName}, FilePath={ContainerFilePath}, TotalBlocks={TotalBlocks}, BlockSize={BlockSize}", containerName, containerFilePath, totalBlocks, blockSize);

            if (blockSize < MinBlockSize)
            {
                _logger.LogError("Invalid block size: {BlockSize}. It must be at least {MinBlockSize} bytes.", blockSize, MinBlockSize);
                throw new ArgumentException($"Block size must be at least {MinBlockSize} bytes.", nameof(blockSize));
            }

            if (totalBlocks <= 0)
            {
                _logger.LogError("Invalid total blocks: {TotalBlocks}. It must be a positive integer.", totalBlocks);
                throw new ArgumentException("Total blocks must be a positive integer.", nameof(totalBlocks));
            }
                try
                {
                    if (IsFileLocked(containerFilePath))
                    {
                        _logger.LogWarning("File '{FilePath}' is locked by another process. Retrying...", containerFilePath);
                    }

                    if (string.IsNullOrWhiteSpace(containerName))
                        throw new ArgumentException("Container name cannot be null or whitespace.", nameof(containerName));

                    if (string.IsNullOrWhiteSpace(containerFilePath))
                        throw new ArgumentException("Container file path cannot be null or whitespace.", nameof(containerFilePath));

                    containerName = containerName.Trim(); // Trim whitespaces

                    if (_containers.ContainsKey(containerName))
                        throw new ContainerAlreadyExistsException($"A container with the name '{containerName}' already exists.");

                    var metadata = new ContainerMetadata(totalBlocks, blockSize)
                    {
                        ContainerName = containerName,
                        ContainerFilePath = containerFilePath
                    };

                    byte[] metadataBytes = metadata.Serialize();
                    
                    // Create padded array
                    byte[] paddedMetadataBytes = new byte[blockSize];
                    if (metadataBytes.Length > blockSize)
                    {
                        _logger.LogError("Serialized metadata size ({Size} bytes) exceeds block size ({BlockSize} bytes).", metadataBytes.Length, blockSize);
                        throw new InvalidOperationException("Metadata data exceeds block size.");
                    }

                    // Copy metadata bytes to padded array
                    Array.Copy(metadataBytes, paddedMetadataBytes, metadataBytes.Length);
                    
                    lock (_lock)
                    {
                        var directoryPath = Path.GetDirectoryName(containerFilePath);
                        if (!Directory.Exists(directoryPath))
                        {
                            if (directoryPath != null)
                            {
                                Directory.CreateDirectory(directoryPath);
                            }
                            else
                            {
                                throw new ArgumentException("Directory path cannot be null.", nameof(directoryPath));
                            }
                            _logger.LogInformation("Directory '{DirectoryPath}' created successfully.", directoryPath);
                        }

                        var fileIOLogger = _loggerFactory.CreateLogger<FileIOHelper>();
                        var newFileIOHelper = new FileIOHelper(fileIOLogger, containerFilePath, blockSize);
                        _fileIOHelpers.TryAdd(containerName, newFileIOHelper);

                        _containers.TryAdd(containerName, metadata);

                        var superblock = new Superblock(totalBlocks, blockSize)
                        {
                            TotalBlocks = totalBlocks,
                            BlockSize = blockSize,
                            MetadataStartBlock = 1
                        };
                        _superblocks.TryAdd(containerName, superblock);

                        try
                        {
                            newFileIOHelper.InitializeContainerAsync(totalBlocks).GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Error initializing container '{ContainerName}': {ErrorMessage}", containerName, ex.Message);
                            throw;
                        }

                        _logger.LogInformation("Container file '{ContainerFilePath}' initialized with {TotalBlocks} blocks.", containerFilePath, totalBlocks);

                        newFileIOHelper.WriteBlockAsync(superblock.MetadataStartBlock, paddedMetadataBytes).Wait();
                        _logger.LogInformation("Metadata written to block {MetadataStartBlock}.", superblock.MetadataStartBlock);

                        var directoryServiceLogger = _loggerFactory.CreateLogger<DirectoryService>();
                        var directoryService = new DirectoryService(directoryServiceLogger, this, containerName);
                        _directoryServices.TryAdd(containerName, directoryService);

                        _logger.LogInformation("ContainerManager: Container '{ContainerName}' created successfully at '{ContainerFilePath}'.", containerName, containerFilePath);
                        return true;
                    }
                }
                catch (IOException ex)
                {
                    _logger.LogError($"Error creating container '{containerName}': {ex.Message}. Retrying in {1000}ms...");
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    _logger.LogError("ContainerManager: Error creating container '{ContainerName}': {ErrorMessage}\nAttempting cleanup:", containerName, ex.Message);
                    try
                    {
                        File.Delete(containerFilePath);
                        _logger.LogInformation("ContainerManager: Deleting container file '{ContainerFilePath}' due to errors.", containerFilePath);
                    }
                    catch (Exception ex2)
                    {
                        _logger.LogError("ContainerManager: Error deleting container file '{ContainerFilePath}': {ErrorMessage}", containerFilePath, ex2.Message);
                    }
                }
            _logger.LogWarning("Failed to create container '{ContainerName}' after {RetryCount} attempts.", containerName, 3);
            return false;
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
        public FileService GetFileService(string containerName)
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
