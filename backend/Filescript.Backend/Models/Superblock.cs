using System;
using System.Text.Json;
using System.Text;

namespace Filescript.Models
{
    /// <summary>
    /// Represents the superblock of the file system container.
    /// </summary>
    public class Superblock
    {
        public int TotalBlocks { get; set; }
        public int BlockSize { get; set; }
        public int MetadataStartBlock { get; set; }

        public Superblock(int totalBlocks, int blockSize)
        {
            TotalBlocks = totalBlocks;
            BlockSize = blockSize;
            MetadataStartBlock = 1; // Default value; can be overridden
        }

        /// <summary>
        /// Serializes the superblock to a byte array.
        /// </summary>
        public byte[] Serialize()
        {
            var superblockDto = new SuperblockDto
            {
                TotalBlocks = this.TotalBlocks,
                BlockSize = this.BlockSize,
                MetadataStartBlock = this.MetadataStartBlock
            };

            string json = JsonSerializer.Serialize(superblockDto);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            // Pad or truncate to match block size if necessary
            // Assuming block size is handled elsewhere
            return bytes;
        }

        /// <summary>
        /// Deserializes the superblock from a byte array.
        /// </summary>
        public static Superblock Deserialize(byte[] data)
        {
            string json = Encoding.UTF8.GetString(data).TrimEnd('\0');
            var superblockDto = JsonSerializer.Deserialize<SuperblockDto>(json);
            var superblock = new Superblock(superblockDto.TotalBlocks, superblockDto.BlockSize)
            {
                MetadataStartBlock = superblockDto.MetadataStartBlock
            };
            return superblock;
        }
    }

    /// <summary>
    /// Data Transfer Object for Superblock serialization.
    /// </summary>
    public class SuperblockDto
    {
        public int TotalBlocks { get; set; }
        public int BlockSize { get; set; }
        public int MetadataStartBlock { get; set; }
    }
}
