using System.Text.Json;
using Filescript.Backend.Services;
using Filescript.Models;
using Filescript.Utilities;

namespace Filescript.Backend.Models {
    /// <summary>
    /// Represents the metadata of the file system container.
    /// This includes information about files, directories, free blocks, and other essential data structures.
    /// </summary>
    public class ContainerMetadata {
        /// <summary>
        /// Dictionary mapping file names to their corresponding <see cref="FileEntry"/> objects.
        /// </summary>
        public Dictionary<string, FileEntry> Files { get; set; }

        /// <summary>
        /// Dictionary mapping directory names to their corresponding <see cref="DirectoryEntry"/> objects.
        /// </summary>
        public Dictionary<string, DirectoryEntry> Directories { get; set; }

        /// <summary>
        /// List of free block indices available for storage.
        /// </summary>
        public List<int> FreeBlocks { get; set; }

        /// <summary>
        /// The current working directory in the container.
        /// </summary>
        public string CurrentDirectory { get; set; }

        /// <summary>
        /// Total number of blocks in the container.
        /// </summary>
        public int TotalBlocks { get; set; }

        /// <summary>
        /// Size of each block in bytes.
        /// </summary>
        public int BlockSize { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerMetadata"/> class with default values.
        /// </summary>
        /// 
        private readonly ILogger<FileService> _logger;
        private readonly FileIOHelper _fileIOHelper;

        public ContainerMetadata()
        {
            Files = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase);
            Directories = new Dictionary<string, DirectoryEntry>(StringComparer.OrdinalIgnoreCase);
            FreeBlocks = new List<int>();
            CurrentDirectory = "/"; // Root directory
            TotalBlocks = 0;
            BlockSize = 4096; // Default block size (4KB)
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerMetadata"/> class with specified parameters.
        /// </summary>
        /// <param name="totalBlocks">Total number of blocks in the container.</param>
        /// <param name="blockSize">Size of each block in bytes.</param>
        public ContainerMetadata(int totalBlocks, int blockSize = 4096)
        {
            Files = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase);
            Directories = new Dictionary<string, DirectoryEntry>(StringComparer.OrdinalIgnoreCase);
            FreeBlocks = new List<int>();
            CurrentDirectory = "/"; // Root directory
            TotalBlocks = totalBlocks;
            BlockSize = blockSize;

            // Initialize all blocks as free
            for (int i = 0; i < TotalBlocks; i++)
            {
                FreeBlocks.Add(i);
            }
        }

        /// <summary>
        /// Adds a new file to the metadata.
        /// </summary>
        /// <param name="fileEntry">The <see cref="FileEntry"/> object representing the file.</param>
        public void AddFile(FileEntry fileEntry)
        {
            if (fileEntry == null)
                throw new ArgumentNullException(nameof(fileEntry));

            if (Files.ContainsKey(fileEntry.Name))
                throw new ArgumentException($"A file with the name '{fileEntry.Name}' already exists.");

            Files.Add(fileEntry.Name, fileEntry);
        }

        /// <summary>
        /// Removes a file from the metadata.
        /// </summary>
        /// <param name="fileName">The name of the file to remove.</param>
        public void RemoveFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

            Files.Remove(fileName);
        }

        /// <summary>
        /// Adds a new directory to the metadata.
        /// </summary>
        /// <param name="directoryEntry">The <see cref="DirectoryEntry"/> object representing the directory.</param>
        public void AddDirectory(DirectoryEntry directoryEntry)
        {
            if (directoryEntry == null)
                throw new ArgumentNullException(nameof(directoryEntry));

            if (Directories.ContainsKey(directoryEntry.Name))
                throw new ArgumentException($"A directory with the name '{directoryEntry.Name}' already exists.");

            Directories.Add(directoryEntry.Name, directoryEntry);
        }

        /// <summary>
        /// Removes a directory from the metadata.
        /// </summary>
        /// <param name="directoryName">The name of the directory to remove.</param>
        public void RemoveDirectory(string directoryName)
        {
            if (string.IsNullOrWhiteSpace(directoryName))
                throw new ArgumentException("Directory name cannot be null or empty.", nameof(directoryName));

            Directories.Remove(directoryName);
        }

        /// <summary>
        /// Allocates a block for storage and removes it from the free blocks list.
        /// </summary>
        /// <returns>The index of the allocated block.</returns>
        public int AllocateBlock()
        {
            if (FreeBlocks.Count == 0)
                throw new InvalidOperationException("No free blocks available for allocation.");

            int blockIndex = FreeBlocks[FreeBlocks.Count - 1];
            FreeBlocks.RemoveAt(FreeBlocks.Count - 1);
            return blockIndex;
        }

        /// <summary>
        /// Frees a block by adding its index back to the free blocks list.
        /// </summary>
        /// <param name="blockIndex">The index of the block to free.</param>
        public void FreeBlock(int blockIndex)
        {
            if (blockIndex < 0 || blockIndex >= TotalBlocks)
                throw new ArgumentOutOfRangeException(nameof(blockIndex), "Block index is out of range.");

            if (!FreeBlocks.Contains(blockIndex))
            {
                FreeBlocks.Add(blockIndex);
            }
        }

        /// <summary>
        /// Retrieves all file entries.
        /// </summary>
        /// <returns>A collection of all <see cref="FileEntry"/> objects.</returns>
        public IEnumerable<FileEntry> GetAllFiles()
        {
            return Files.Values;
        }

        /// <summary>
        /// Retrieves all directory entries.
        /// </summary>
        /// <returns>A collection of all <see cref="DirectoryEntry"/> objects.</returns>
        public IEnumerable<DirectoryEntry> GetAllDirectories()
        {
            return Directories.Values;
        }

        public ContainerMetadata LoadMetadata()
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
        public void SaveMetadata()
        {
           _logger.LogInformation("Saving container metadata.");

            try
            {
                string json = JsonSerializer.Serialize(this);
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

        public byte[] Serialize() {
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(this);
        }
    }
}