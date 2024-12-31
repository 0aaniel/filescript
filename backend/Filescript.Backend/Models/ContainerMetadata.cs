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
            var options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.SerializeToUtf8Bytes(this, options);
        }

        public static ContainerMetadata Deserialize(byte[] compressedBytes)
        {
            using (var inputStream = new MemoryStream(compressedBytes))
            using (var gzip = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var decompressedStream = new MemoryStream())
            {
                gzip.CopyTo(decompressedStream);
                return JsonSerializer.Deserialize<ContainerMetadata>(decompressedStream.ToArray());
            }
        }
        /// <summary>
        /// Deserializes the metadata from a byte array.
        /// </summary>
        public static ContainerMetadata Deserialize(byte[] data, int blockSize = 4096)
        {
            string json = Encoding.UTF8.GetString(data).TrimEnd('\0');
            var metadataDto = JsonSerializer.Deserialize<ContainerMetadataDto>(json);
            var metadata = new ContainerMetadata(metadataDto.FreeBlockBitmap.Length, blockSize)
            {
                Directories = metadataDto.Directories,
                Files = metadataDto.Files,
                CurrentDirectory = metadataDto.CurrentDirectory,
                FreeBlocks = metadataDto.FreeBlockBitmap.ToList(),
                ContainerFilePath = metadataDto.ContainerFilePath // Assign the file path
            };
            return metadata;
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
