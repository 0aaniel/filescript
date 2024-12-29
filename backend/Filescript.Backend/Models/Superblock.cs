using System;
using System.Text.Json;
using System.Text;
using System.IO;

namespace Filescript.Models
{
    /// <summary>
    /// Represents the superblock of the file system.
    /// </summary>
    public class Superblock
    {
        public long TotalBlocks { get; set; }
        public int BlockSize { get; set; }
        public int FreeBlockCount { get; set; }
        public int MetadataStartBlock { get; set; }
        public int MetadataBlockCount { get; set; }

        /// <summary>
        /// Serializes the superblock to a byte array.
        /// </summary>
        public byte[] Serialize()
        {
            string json = JsonSerializer.Serialize(this);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            if (bytes.Length > 4096) // Assuming block size is 4KB
                throw new InvalidOperationException("Superblock data exceeds block size.");
            Array.Resize(ref bytes, 4096); // Pad with zeros if necessary
            return bytes;
        }

        /// <summary>
        /// Deserializes the superblock from a byte array.
        /// </summary>
        public static Superblock Deserialize(byte[] data)
        {
            string json = Encoding.UTF8.GetString(data).TrimEnd('\0');
            return JsonSerializer.Deserialize<Superblock>(json);
        }
    }
}
