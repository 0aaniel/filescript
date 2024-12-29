using System.IO.Enumeration;
using System.Text.Json;
using Filescript.Backend.Models;
using Filescript.Utilities;

namespace Filescript.Backend.Services {
    /// <summary>
    /// Service handling file operations within the container.
    /// </summary>
    public class FileService : IFileService {
        private readonly ILogger<FileService> _logger;
        private readonly FileIOHelper _fileIOHelper;
        private readonly IDeduplicationService _deduplicationService;
        private readonly IResiliencyService _resiliencyService;
        private readonly ContainerMetadata _metadata;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for logging.</param>
        /// <param name="fileIOHelper">Helper for file I/O operations.</param>
        /// <param name="deduplicationService">Service for handling deduplication.</param>
        /// <param name="resiliencyService">Service for handling resiliency checks.</param>
        public FileService(
            ILogger<FileService> logger,
            FileIOHelper fileIOHelper,
            IDeduplicationService deduplicationService,
            IResiliencyService resiliencyService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileIOHelper = fileIOHelper ?? throw new ArgumentNullException(nameof(fileIOHelper));
            _deduplicationService = deduplicationService ?? throw new ArgumentNullException(nameof(deduplicationService));
            _resiliencyService = resiliencyService ?? throw new ArgumentNullException(nameof(resiliencyService));

            _metadata = LoadMetadata();
        }

        /// <inheritdoc />
        public async Task<bool> CopyInAsync(string sourcePath, string destName) {
            _logger.LogInformation("CopyInAsync: Copying file from {SourcePath} to {DestName}.", sourcePath, destName);

            if (!File.Exists(sourcePath)) {
                _logger.LogError("CopyInAsync: Source file {SourcePath} does not exist.", sourcePath);
                throw new FileNotFoundException("Source file does not exist.", sourcePath);
            }

            try {
                FileInfo fileInfo = new FileInfo(sourcePath);
                long fileSize = fileInfo.Length;
                int blockSize = _fileIOHelper.BlockSize;
                int totalBlocks = (int)Math.Ceiling((double)fileSize / blockSize);

                using (FileStream fs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read)) {
                    byte[] buffer = new byte[blockSize];
                    int bytesRead;

                    while ((bytesRead = await fs.ReadAsync(buffer, 0, blockSize)) > 0) {
                        byte[] actualData = bytesRead == blockSize ? buffer : buffer[..bytesRead];
                        int blockIndex = await _deduplicationService.StoreBlockAsync(actualData);
                        _metadata.FreeBlocks.Remove(blockIndex);
                        _metadata.Files.Add(destName, new FileEntry {
                            Name = destName,
                            Size = fileSize,
                            BlockIndices = new List<int> { blockIndex},
                            CreatedAt = DateTime.UtcNow,
                            ModifiedAt = DateTime.UtcNow
                        });
                    }
                }

                SaveMetadata();

                _logger.LogInformation("CopyInAsync: Successfully copied {DestName} into the container.", destName);
                return true;
            } catch (UnauthorizedAccessException ex) {
                _logger.LogError(ex, "Unauthorized access when copying in file.");
                throw;

            } catch (Exception ex) {
                _logger.LogError(ex, "An error occurred during CopyInAsync.");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> CopyOutAsync(string sourceName, string destPath)
        {
            _logger.LogInformation("CopyOutAsync: Copying file from {SourceName} to {DestPath}", sourceName, destPath);

            if (!_metadata.Files.TryGetValue(sourceName, out FileEntry fileEntry))
            {
                _logger.LogError("File not found in container: {SourceName}", sourceName);
                throw new FileNotFoundException("File not found in container.", sourceName);
            }

            try
            {
                using (FileStream fs = new FileStream(destPath, FileMode.Create, FileAccess.Write))
                {
                    foreach (int blockIndex in fileEntry.BlockIndices)
                    {
                        // Check block integrity before reading
                        bool isValid = await _resiliencyService.CheckBlockIntegrityAsync(blockIndex);
                        if (!isValid)
                        {
                            _logger.LogError("Block integrity check failed for block index {BlockIndex}.", blockIndex);
                            throw new IOException($"Block integrity check failed for block index {blockIndex}.");
                        }

                        byte[] data = await _fileIOHelper.ReadBlockAsync(blockIndex);
                        await fs.WriteAsync(data, 0, data.Length);
                    }
                }

                _logger.LogInformation("CopyOutAsync: Successfully copied {SourceName} out to {DestPath}.", sourceName, destPath);
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access when copying out file.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during CopyOutAsync.");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> RemoveFileAsync(string fileName)
        {
            _logger.LogInformation("RemoveFileAsync: Removing file {FileName}", fileName);

            if (!_metadata.Files.TryGetValue(fileName, out FileEntry fileEntry))
            {
                _logger.LogWarning("RemoveFileAsync: File not found: {FileName}", fileName);
                return false;
            }

            try
            {
                foreach (int blockIndex in fileEntry.BlockIndices)
                {
                    _deduplicationService.RemoveBlock(blockIndex);
                    _metadata.FreeBlocks.Add(blockIndex);
                }

                _metadata.Files.Remove(fileName);

                // Update metadata on disk
                SaveMetadata();

                _logger.LogInformation("RemoveFileAsync: Successfully removed {FileName}.", fileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during RemoveFileAsync.");
                throw;
            }
        }

        /// <inheritdoc />
        public List<FileEntry> ListFiles()
        {
            _logger.LogInformation("ListFiles: Listing files in the current directory.");

            try
            {
                // Assuming current directory context is managed within the service or metadata
                var files = new List<FileEntry>(_metadata.Files.Values);
                _logger.LogInformation("ListFiles: Retrieved {FileCount} files.", files.Count);
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during ListFiles.");
                throw;
            }
        }

        /// <summary>
        /// Loads the container metadata from the container file.
        /// </summary>
        /// <returns>The loaded <see cref="ContainerMetadata"/>.</returns>
        private ContainerMetadata LoadMetadata()
        {
            _logger.LogInformation("Loading container metadata.");

            if (!File.Exists(_fileIOHelper.ContainerFilePath))
            {
                _logger.LogWarning("Container file does not exist. Initializing new metadata.");
                return new ContainerMetadata();
            }

            try
            {
                using (FileStream fs = new FileStream(_fileIOHelper.ContainerFilePath, FileMode.Open, FileAccess.Read))
                {
                    // Assume metadata is stored at the beginning of the container file
                    const int metadataSize = 1024; // Define metadataSize appropriately
                    byte[] buffer = new byte[metadataSize];
                    fs.Read(buffer, 0, buffer.Length);
                    string json = System.Text.Encoding.UTF8.GetString(buffer);
                    var metadata = JsonSerializer.Deserialize<ContainerMetadata>(json);
                    return metadata ?? new ContainerMetadata();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load container metadata.");
                throw;
            }
        }

        /// <summary>
        /// Saves the container metadata to the container file.
        /// </summary>
        private void SaveMetadata()
        {
           _logger.LogInformation("Saving container metadata.");

            try
            {
                string json = JsonSerializer.Serialize(_metadata);
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(json);

                using (FileStream fs = new FileStream(_fileIOHelper.ContainerFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    // Assume metadata is stored at the beginning of the container file
                    fs.Write(buffer, 0, buffer.Length);
                    // Optionally, pad the remaining space reserved for metadata
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save container metadata.");
                throw;
            }
        }

        /// <inheritdoc />
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
                if (!File.Exists(_fileIOHelper.ContainerFilePath))
                {
                    _logger.LogError("FileService: Container file does not exist at {Path}.", _fileIOHelper.ContainerFilePath);
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