using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;
using System.IO;
using System.Linq;
using Filescript.Backend.Exceptions;
using Filescript.Backend.Models;
using System.IO.Compression;

namespace Filescript.Models
{
    /// <summary>
    /// Represents the metadata of the file system container.
    /// </summary>
    public class ContainerMetadata
    {
        public Dictionary<string, DirectoryEntry> Directories { get; set; }
        public Dictionary<string, FileEntry> Files { get; set; }
        public List<int> FreeBlocks { get; set; }
        public string CurrentDirectory { get; set; }
        public string ContainerFilePath { get; set; } // Added property
        public string ContainerName { get; set; }
        public long TotalBlocks { get; set; }
        public int BlockSize { get; set; }

        private readonly int _blockSize;

        public ContainerMetadata(int totalBlocks, int blockSize = 4096)
        {
            _blockSize = blockSize;
            Directories = new Dictionary<string, DirectoryEntry>(StringComparer.OrdinalIgnoreCase);
            Files = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase);
            FreeBlocks = new List<int>();

            // Initialize root directory
            string rootPath = "/";
            var rootDirectory = new DirectoryEntry("root", rootPath);
            Directories.Add(rootPath, rootDirectory);

            // Reserve blocks 0 and 1 for superblock and metadata
            for (int i = 0; i < 2; i++)
            {
                FreeBlocks.Add(i);
            }

            // Add remaining blocks to free list
            for (int i = 2; i < totalBlocks; i++)
            {
                FreeBlocks.Add(i);
            }

            CurrentDirectory = rootPath;
        }

        /// <summary>
        /// Adds a new directory to the metadata.
        /// </summary>
        public void AddDirectory(DirectoryEntry directory)
        {
            if (!Directories.ContainsKey(directory.Path))
            {
                Directories.Add(directory.Path, directory);
            }
            else
            {
                throw new DirectoryAlreadyExistsException($"Directory '{directory.Path}' already exists.");
            }
        }

        /// <summary>
        /// Removes a directory from the metadata.
        /// </summary>
        public void RemoveDirectory(string directoryPath)
        {
            if (Directories.ContainsKey(directoryPath))
            {
                Directories.Remove(directoryPath);
            }
            else
            {
                throw new Backend.Exceptions.DirectoryNotFoundException($"Directory '{directoryPath}' not found.");
            }
        }

        /// <summary>
        /// Allocates a free block.
        /// </summary>
        /// <returns>The index of the allocated block.</returns>
        public int AllocateBlock()
        {
            if (FreeBlocks.Count == 0)
                throw new InvalidOperationException("No free blocks available.");

            int blockIndex = FreeBlocks[0];
            FreeBlocks.RemoveAt(0);
            return blockIndex;
        }

        /// <summary>
        /// Frees an allocated block.
        /// </summary>
        /// <param name="blockIndex">The index of the block to free.</param>
        public void FreeBlock(int blockIndex)
        {
            if (!FreeBlocks.Contains(blockIndex))
            {
                FreeBlocks.Add(blockIndex);
            }
        }

        /// <summary>
        /// Serializes the metadata to a byte array.
        /// </summary>
        public byte[] Serialize()
        {
            var data = new
            {
                ContainerName,
                ContainerFilePath,
                TotalBlocks,
                BlockSize,
                CurrentDirectory,
                Files = Files ?? new Dictionary<string, FileEntry>(),
                Directories = Directories ?? new Dictionary<string, DirectoryEntry>(),
                FreeBlocks = FreeBlocks ?? new List<int>()
            };

            string jsonString = JsonSerializer.Serialize(data);
            return Encoding.UTF8.GetBytes(jsonString);
        }

        public static ContainerMetadata Deserialize(byte[] bytes)
        {
            // Skip empty or invalid data
            if (bytes == null || bytes.Length == 0 || bytes.All(b => b == 0))
            {
                return new ContainerMetadata(1, 4096); // Default values
            }

            try
            {
                // Remove trailing zeros if any
                int lastNonZeroIndex = Array.FindLastIndex(bytes, b => b != 0);
                if (lastNonZeroIndex >= 0)
                {
                    bytes = bytes.Take(lastNonZeroIndex + 1).ToArray();
                }

                string jsonString = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
                var data = JsonSerializer.Deserialize<JsonElement>(jsonString);

                var metadata = new ContainerMetadata(
                    data.GetProperty("TotalBlocks").GetInt32(),
                    data.GetProperty("BlockSize").GetInt32()
                )
                {
                    ContainerName = data.GetProperty("ContainerName").GetString(),
                    ContainerFilePath = data.GetProperty("ContainerFilePath").GetString(),
                    CurrentDirectory = data.GetProperty("CurrentDirectory").GetString()
                };

                // Add error handling for optional properties
                if (data.TryGetProperty("Files", out JsonElement filesElement))
                {
                    metadata.Files = JsonSerializer.Deserialize<Dictionary<string, FileEntry>>(filesElement.GetRawText());
                }

                if (data.TryGetProperty("Directories", out JsonElement directoriesElement))
                {
                    metadata.Directories = JsonSerializer.Deserialize<Dictionary<string, DirectoryEntry>>(directoriesElement.GetRawText());
                }

                if (data.TryGetProperty("FreeBlocks", out JsonElement freeBlocksElement))
                {
                    metadata.FreeBlocks = JsonSerializer.Deserialize<List<int>>(freeBlocksElement.GetRawText());
                }

                return metadata;
            }
            catch (Exception)
            {
                // If deserialization fails, return a new metadata instance
                return new ContainerMetadata(1, 4096);
            }
        }
    }

    /// <summary>
    /// Data Transfer Object for ContainerMetadata serialization.
    /// </summary>
    public class ContainerMetadataDto
    {
        public Dictionary<string, DirectoryEntry> Directories { get; set; }
        public Dictionary<string, FileEntry> Files { get; set; }
        public int[] FreeBlockBitmap { get; set; }
        public string CurrentDirectory { get; set; }
        public string ContainerFilePath { get; set; } // Added property
    }
}
