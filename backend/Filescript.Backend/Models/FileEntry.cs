using System;
using System.Collections.Generic;

namespace Filescript.Backend.Models
{
    /// <summary>
    /// Represents a file within the container.
    /// </summary>
    public class FileEntry
    {
        /// <summary>
        /// Name of the file within the container.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Size of the file in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// List of block indices where the file's data is stored.
        /// </summary>
        public List<int> BlockIndices { get; set; }

        /// <summary>
        /// Timestamp of file creation.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp of the last modification.
        /// </summary>
        public DateTime ModifiedAt { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FileEntry"/> class.
        /// </summary>
        public FileEntry()
        {
            BlockIndices = new List<int>();
            CreatedAt = DateTime.UtcNow;
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileEntry"/> class with specified parameters.
        /// </summary>
        /// <param name="name">Name of the file.</param>
        /// <param name="size">Size of the file in bytes.</param>
        /// <param name="blockIndices">List of block indices.</param>
        public FileEntry(string name, long size, List<int> blockIndices)
        {
            Name = name;
            Size = size;
            BlockIndices = blockIndices ?? new List<int>();
            CreatedAt = DateTime.UtcNow;
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the modification timestamp to the current UTC time.
        /// </summary>
        public void UpdateModificationTime()
        {
            ModifiedAt = DateTime.UtcNow;
        }
    }
}