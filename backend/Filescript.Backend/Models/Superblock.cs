using System;
using System.Text.Json;
using System.Text;

namespace Filescript.Backend.Models
{
    /// <summary>
    /// Represents the superblock of the file system container.
    /// </summary>
    public class Superblock
    {
        public int TotalBlocks { get; set; }
        public int BlockSize { get; set; }
        public int MetadataHeadBlock { get; set; } = -1;
        public int Magic { get; set; } = 0x2023_1234;

        public Superblock(int totalBlocks, int blockSize)
        {
            TotalBlocks = totalBlocks;
            BlockSize = blockSize;
        }
    }
}
