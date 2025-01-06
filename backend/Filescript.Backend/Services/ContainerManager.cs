using Filescript.Backend.Exceptions;
using Filescript.Backend.Models;
using Filescript.Backend.Utilities;
using Filescript.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text;

namespace Filescript.Backend.Services
{
    /// <summary>
    /// Manages multiple file system containers.
    /// </summary>
    public class ContainerManager
    {
        private readonly ILogger<ContainerManager> _logger;
        private readonly ILoggerFactory _loggerFactory;

        // These dictionaries keep track of container objects in memory
        private readonly ConcurrentDictionary<string, ContainerMetadata> _containers;
        private readonly ConcurrentDictionary<string, FileIOHelper> _fileIOHelpers;
        private readonly ConcurrentDictionary<string, Superblock> _superblocks;
        private readonly ConcurrentDictionary<string, DirectoryService> _directoryServices;

        private readonly object _lock = new object();

        public ContainerManager(ILogger<ContainerManager> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;

            _containers        = new ConcurrentDictionary<string, ContainerMetadata>();
            _fileIOHelpers     = new ConcurrentDictionary<string, FileIOHelper>();
            _superblocks       = new ConcurrentDictionary<string, Superblock>();
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
                    // If we can open it exclusively, it's not locked
                    stream.Close();
                }
                return false;
            }
            catch (IOException)
            {
                // If we get an IOException, it means the file is locked
                return true;
            }
        }

        /// <summary>
        /// Creates a new container with the specified name and file path.
        /// </summary>
        public async Task<bool> CreateContainerAsync(
            string containerName,
            string containerFilePath,
            int totalBlocks = 1,
            int blockSize = 4096)
        {
            const int MinBlockSize = 512; // Define a minimum block size
            _logger.LogInformation(
                "Starting container creation: Name={ContainerName}, FilePath={ContainerFilePath}, " +
                "TotalBlocks={TotalBlocks}, BlockSize={BlockSize}",
                containerName, containerFilePath, totalBlocks, blockSize
            );

            if (blockSize < MinBlockSize)
            {
                _logger.LogError("Invalid block size: {BlockSize}. Must be >= {MinBlockSize} bytes.", blockSize, MinBlockSize);
                throw new ArgumentException($"Block size must be at least {MinBlockSize} bytes.");
            }

            if (totalBlocks <= 0)
            {
                _logger.LogError("Invalid total blocks: {TotalBlocks}. Must be a positive integer.", totalBlocks);
                throw new ArgumentException("Total blocks must be a positive integer.");
            }

            try
            {
                // Check if file is locked
                if (IsFileLocked(containerFilePath))
                {
                    _logger.LogWarning("File '{FilePath}' is locked by another process.", containerFilePath);
                }

                if (string.IsNullOrWhiteSpace(containerName))
                    throw new ArgumentException("Container name cannot be null or whitespace.", nameof(containerName));

                if (string.IsNullOrWhiteSpace(containerFilePath))
                    throw new ArgumentException("Container file path cannot be null or whitespace.", nameof(containerFilePath));

                containerName = containerName.Trim();

                // Check if container already exists in memory
                if (_containers.ContainsKey(containerName))
                {
                    throw new ContainerAlreadyExistsException(
                        $"A container with the name '{containerName}' already exists."
                    );
                }

                // Create new ContainerMetadata
                var metadata = new ContainerMetadata(totalBlocks, blockSize)
                {
                    ContainerName     = containerName,
                    ContainerFilePath = containerFilePath
                };

                // Create superblock
                var superblock = new Superblock(totalBlocks, blockSize)
                {
                    TotalBlocks       = totalBlocks,
                    BlockSize         = blockSize,
                    MetadataHeadBlock = -1 // no metadata pages yet
                };

                lock (_lock)
                {
                    // Make sure directory exists
                    var directoryPath = Path.GetDirectoryName(containerFilePath);
                    if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                        _logger.LogInformation("Directory '{DirectoryPath}' created successfully.", directoryPath);
                    }

                    // Create FileIOHelper
                    var fileIOLogger = _loggerFactory.CreateLogger<FileIOHelper>();
                    var newFileIOHelper = new FileIOHelper(fileIOLogger, containerFilePath, blockSize);

                    // Add to our dictionaries
                    _fileIOHelpers.TryAdd(containerName, newFileIOHelper);
                    _containers.TryAdd(containerName, metadata);
                    _superblocks.TryAdd(containerName, superblock);

                    // Initialize container file
                    try
                    {
                        newFileIOHelper.InitializeContainerAsync(totalBlocks).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error initializing container '{ContainerName}': {ErrorMessage}", containerName, ex.Message);
                        throw;
                    }

                    _logger.LogInformation(
                        "Container file '{ContainerFilePath}' initialized with {TotalBlocks} blocks.",
                        containerFilePath, totalBlocks
                    );

                    // Optionally, we can do an immediate SaveMetadataAsync to store the initial state
                    // (That sets up the multi-block chain with any initial data.)
                    // Or we can wait until first write. We'll do it now for demonstration:
                    SaveMetadataAsync(containerName).GetAwaiter().GetResult();

                    // DirectoryService
                    var directoryServiceLogger = _loggerFactory.CreateLogger<DirectoryService>();
                    var directoryService = new DirectoryService(directoryServiceLogger, this, containerName);
                    _directoryServices.TryAdd(containerName, directoryService);

                    _logger.LogInformation(
                        "ContainerManager: Container '{ContainerName}' created successfully at '{ContainerFilePath}'.",
                        containerName, containerFilePath
                    );
                    return true;
                }
            }
            catch (IOException ex)
            {
                _logger.LogError(
                    $"Error creating container '{containerName}': {ex.Message}. Retrying in {1000}ms..."
                );
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "ContainerManager: Error creating container '{ContainerName}': {ErrorMessage}\nAttempting cleanup:",
                    containerName, ex.Message
                );
                try
                {
                    File.Delete(containerFilePath);
                    _logger.LogInformation(
                        "ContainerManager: Deleted container file '{ContainerFilePath}' due to errors.",
                        containerFilePath
                    );
                }
                catch (Exception ex2)
                {
                    _logger.LogError(
                        "ContainerManager: Error deleting container file '{ContainerFilePath}': {ErrorMessage}",
                        containerFilePath, ex2.Message
                    );
                }
            }

            _logger.LogWarning("Failed to create container '{ContainerName}' after attempts.", containerName);
            return false;
        }

        /// <summary>
        /// Creates a new file within a specified container and path (delegates to FileService).
        /// </summary>
        public async Task<bool> CreateFileAsync(
            string containerName,
            string fileName,
            string path,
            byte[] content)
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
                    var fileService = new FileService(fileServiceLogger, this);
                    fileService.Initialize(containerName);  // Initialize with the container name
                    return fileService;
                }
                else
                {
                    throw new ContainerNotFoundException(
                        $"Metadata or Superblock for container '{containerName}' does not exist."
                    );
                }
            }
            else
            {
                throw new ContainerNotFoundException(
                    $"FileIOHelper for container '{containerName}' does not exist."
                );
            }
        }

        /// <summary>
        /// Retrieves the DirectoryService for the specified container.
        /// </summary>
        public DirectoryService GetDirectoryService(string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("Container name cannot be null or whitespace.", nameof(containerName));

            // Try to get existing
            if (_directoryServices.TryGetValue(containerName, out DirectoryService service))
            {
                return service;
            }

            // If container exists but service doesn't, create new
            if (_containers.ContainsKey(containerName))
            {
                var directoryServiceLogger = _loggerFactory.CreateLogger<DirectoryService>();
                var newService = new DirectoryService(directoryServiceLogger, this, containerName);

                if (_directoryServices.TryAdd(containerName, newService))
                {
                    _logger.LogInformation(
                        "Created new DirectoryService for container '{ContainerName}'", containerName
                    );
                    return newService;
                }

                // In case another thread created the service while we were creating ours
                if (_directoryServices.TryGetValue(containerName, out service))
                {
                    return service;
                }
            }

            throw new ContainerNotFoundException($"Container '{containerName}' does not exist.");
        }

        /// <summary>
        /// Lists all existing containers by name.
        /// </summary>
        public List<string> ListContainers()
        {
            return new List<string>(_containers.Keys);
        }

        /// <summary>
        /// Retrieves the metadata for the specified container (the in-memory object).
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
        /// Retrieves the FileIOHelper for the specified container (low-level block I/O).
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
                throw new ContainerNotFoundException(
                    $"FileIOHelper for container '{containerName}' does not exist."
                );
            }
        }

        /// <summary>
        /// Retrieves the Superblock for the specified container (overall container info).
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
                throw new ContainerNotFoundException(
                    $"Superblock for container '{containerName}' does not exist."
                );
            }
        }

        /// <summary>
        /// Deletes the specified container from in-memory dictionaries.
        /// (Optionally also deletes the container file from disk.)
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
                // File.Delete(metadata.ContainerFilePath);

                _logger.LogInformation(
                    "ContainerManager: Container '{ContainerName}' deleted successfully.",
                    containerName
                );
                return true;
            }
            else
            {
                throw new ContainerNotFoundException($"Container '{containerName}' does not exist.");
            }
        }

        /// <summary>
        /// Creates a directory within the specified container (delegates to DirectoryService).
        /// </summary>
        public async Task<bool> CreateDirectoryAsync(
            string containerName,
            string directoryName,
            string path)
        {
            var directoryService = GetDirectoryService(containerName);
            return await directoryService.MakeDirectoryAsync(directoryName, path);
        }

        /// <summary>
        /// Changes the current directory within the specified container (DirectoryService).
        /// </summary>
        public async Task<bool> ChangeDirectoryAsync(string containerName, string targetDirectory)
        {
            var directoryService = GetDirectoryService(containerName);
            return await directoryService.ChangeDirectoryAsync(targetDirectory);
        }

        /// <summary>
        /// Removes a directory (DirectoryService).
        /// </summary>
        public async Task<bool> RemoveDirectoryAsync(
            string containerName,
            string directoryName,
            string path)
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
        /// Gets the current directory of the specified container (DirectoryService).
        /// </summary>
        public string GetCurrentDirectory(string containerName)
        {
            var directoryService = GetDirectoryService(containerName);
            return directoryService.GetCurrentDirectory();
        }

        /// <summary>
        /// Performs a basic health check on the specified container (DirectoryService).
        /// </summary>
        public async Task<bool> BasicHealthCheckAsync(string containerName)
        {
            var directoryService = GetDirectoryService(containerName);
            return await directoryService.BasicHealthCheckAsync();
        }

        // -------------------------------------------------------------------
        // MULTI-BLOCK METADATA LOGIC
        // -------------------------------------------------------------------

        /// <summary>
        /// Saves the in-memory ContainerMetadata of 'containerName' into multiple blocks.
        /// </summary>
        public async Task SaveMetadataAsync(string containerName)
        {
            if (!_containers.TryGetValue(containerName, out var meta))
                throw new ContainerNotFoundException($"Container '{containerName}' not found.");

            if (!_superblocks.TryGetValue(containerName, out var superblock))
                throw new ContainerNotFoundException($"No superblock for container '{containerName}'.");

            if (!_fileIOHelpers.TryGetValue(containerName, out var fileIO))
                throw new ContainerNotFoundException($"No FileIOHelper for container '{containerName}'.");

            // 1) Convert entire metadata to JSON
            string fullJson = meta.ToFullJson();

            // 2) Split JSON into block-sized pages
            List<string> segments = SplitIntoChunks(fullJson, superblock.BlockSize - 256);
            // "256" overhead for JSON structure, next-page pointer, etc.

            // 3) Allocate blocks for each page
            //    If we had previous metadata blocks, we might want to free them first.
            //    For simplicity, assume we do that or we rely on a more advanced "re-allocation" logic.
            var allocated = new List<int>();
            foreach (var seg in segments)
            {
                int blockIndex = meta.AllocateBlock();
                allocated.Add(blockIndex);
            }

            // 4) Build & write each page
            for (int i = 0; i < segments.Count; i++)
            {
                var page = new MetadataPage
                {
                    JsonChunk = segments[i],
                    NextPageBlock = (i < segments.Count - 1) ? allocated[i + 1] : -1
                };

                byte[] pageBytes = MetadataPageSerializer.Serialize(page, superblock.BlockSize);
                await fileIO.WriteBlockAsync(allocated[i], pageBytes);
            }

            // 5) Update superblock to point to the first page
            superblock.MetadataHeadBlock = (segments.Count > 0) ? allocated[0] : -1;

            // 6) Persist the superblock
            await WriteSuperblockAsync(containerName);

            _logger.LogInformation("SaveMetadataAsync: Updated metadata for container '{0}'", containerName);
        }

        /// <summary>
        /// Loads the container's metadata from multiple blocks.
        /// </summary>
        public async Task<ContainerMetadata> LoadMetadataAsync(string containerName)
        {
            if (!_superblocks.TryGetValue(containerName, out var superblock))
                throw new ContainerNotFoundException($"No superblock for container '{containerName}'.");

            if (!_containers.TryGetValue(containerName, out var existingMeta))
                throw new ContainerNotFoundException($"Container '{containerName}' not found in memory.");

            if (!_fileIOHelpers.TryGetValue(containerName, out var fileIO))
                throw new ContainerNotFoundException($"No FileIOHelper for container '{containerName}'.");

            // 1) Start at superblock.MetadataHeadBlock
            int current = superblock.MetadataHeadBlock;
            if (current < 0)
            {
                // No metadata pages => return the existing (empty) metadata
                return existingMeta;
            }

            // 2) Read each page, follow NextPageBlock
            var allChunks = new StringBuilder();
            while (current != -1)
            {
                byte[] blockData = await fileIO.ReadBlockAsync(current);
                var page = MetadataPageSerializer.Deserialize(blockData);

                if (!string.IsNullOrEmpty(page.JsonChunk))
                {
                    allChunks.Append(page.JsonChunk);
                }

                current = page.NextPageBlock;
            }

            // 3) Merge into a new ContainerMetadata object
            string fullJson = allChunks.ToString();
            var newMeta = ContainerMetadata.FromFullJson(fullJson);

            // 4) Overwrite the in-memory reference
            _containers[containerName] = newMeta;

            _logger.LogInformation("LoadMetadataAsync: Loaded metadata for container '{0}'", containerName);
            return newMeta;
        }

        /// <summary>
        /// Splits a string into chunks of at most 'chunkSize' each.
        /// </summary>
        private List<string> SplitIntoChunks(string data, int chunkSize)
        {
            var list = new List<string>();
            int pos = 0;
            while (pos < data.Length)
            {
                int len = Math.Min(chunkSize, data.Length - pos);
                list.Add(data.Substring(pos, len));
                pos += len;
            }
            return list;
        }

        /// <summary>
        /// Writes the superblock to block 0 (or wherever).
        /// </summary>
        private async Task WriteSuperblockAsync(string containerName)
        {
            if (!_superblocks.TryGetValue(containerName, out var sb))
                throw new ContainerNotFoundException($"No superblock for container '{containerName}'.");

            if (!_fileIOHelpers.TryGetValue(containerName, out var fileIO))
                throw new ContainerNotFoundException($"No FileIOHelper for container '{containerName}'.");

            // Convert superblock to JSON
            string json = System.Text.Json.JsonSerializer.Serialize(sb);
            byte[] raw  = System.Text.Encoding.UTF8.GetBytes(json);

            byte[] superblockData = raw;

            if (superblockData.Length < sb.BlockSize)
            {
                byte[] padded = new byte[sb.BlockSize];
                Array.Copy(superblockData, padded, superblockData.Length);
                superblockData = padded;
            }
            else if (superblockData.Length > sb.BlockSize)
            {
                throw new InvalidOperationException("Superblock data is too big for one block!");
            }

            await fileIO.WriteBlockAsync(0, superblockData);

        }
    }
}
