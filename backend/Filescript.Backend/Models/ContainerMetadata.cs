using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;
using System.IO;
using System.Linq;
using Filescript.Backend.Exceptions;
using Filescript.Backend.Models;
using System.IO.Compression;
using System.Reflection.PortableExecutable;

namespace Filescript.Backend.Models
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

        public ContainerMetadata(int totalBlocks, int blockSize)
        {
            TotalBlocks = totalBlocks;
            BlockSize = blockSize;

            Directories = new Dictionary<string, DirectoryEntry>(StringComparer.OrdinalIgnoreCase);
            Files = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase);
            FreeBlocks = new List<int>();

            for (int i = 1; i < totalBlocks; i++)
            {
                FreeBlocks.Add(i);
            }

            // Initialize root directory
            string rootPath = "/";
            var rootDirectory = new DirectoryEntry("root", rootPath);
            Directories[rootPath] = rootDirectory;

            CurrentDirectory = rootPath;
        }

        /// <summary>
        /// Allocate the first free block from the list (naive).
        /// </summary>
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
        /// Convert this entire ContainerMetadata to a single JSON string.
        /// Typically used internally, then we chunk it.
        /// </summary>
        public string ToFullJson()
        {
            var dto = new
            {
                ContainerName,
                ContainerFilePath,
                TotalBlocks,
                BlockSize,
                CurrentDirectory,
                Directories,
                Files,
                FreeBlocks
            };

            return JsonSerializer.Serialize(dto);
        }

        /// <summary>
        /// Rebuild a ContainerMetadata object from a single JSON string.
        /// </summary>
        public static ContainerMetadata FromFullJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            JsonElement root = JsonSerializer.Deserialize<JsonElement>(json);

            int totalBlocks = root.GetProperty("TotalBlocks").GetInt32();
            int blockSize   = root.GetProperty("BlockSize").GetInt32();
            var meta        = new ContainerMetadata(totalBlocks, blockSize)
            {
                ContainerName     = root.GetProperty("ContainerName").GetString(),
                ContainerFilePath = root.GetProperty("ContainerFilePath").GetString(),
                CurrentDirectory  = root.GetProperty("CurrentDirectory").GetString()
            };

            if (root.TryGetProperty("Directories", out JsonElement dirsElem))
            {
                meta.Directories = JsonSerializer
                    .Deserialize<Dictionary<string, DirectoryEntry>>(dirsElem.GetRawText());
            }
            if (root.TryGetProperty("Files", out JsonElement filesElem))
            {
                meta.Files = JsonSerializer
                    .Deserialize<Dictionary<string, FileEntry>>(filesElem.GetRawText());
            }
            if (root.TryGetProperty("FreeBlocks", out JsonElement freeElem))
            {
                meta.FreeBlocks = JsonSerializer
                    .Deserialize<List<int>>(freeElem.GetRawText());
            }

            return meta;
        }
    }
}
