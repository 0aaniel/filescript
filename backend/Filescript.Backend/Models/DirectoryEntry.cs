using System;
using System.Collections.Generic;

namespace Filescript.Models
{
    /// <summary>
    /// Represents a directory within the container.
    /// </summary>
    public class DirectoryEntry
    {
        /// <summary>
        /// Name of the directory.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// List of subdirectories within this directory.
        /// </summary>
        public List<string> SubDirectories { get; set; }

        /// <summary>
        /// List of files within this directory.
        /// </summary>
        public List<string> Files { get; set; }

        /// <summary>
        /// Timestamp of directory creation.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp of the last modification.
        /// </summary>
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryEntry"/> class.
        /// </summary>
        public DirectoryEntry()
        {
            SubDirectories = new List<string>();
            Files = new List<string>();
            CreatedAt = DateTime.UtcNow;
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryEntry"/> class with a specified name.
        /// </summary>
        /// <param name="name">Name of the directory.</param>
        public DirectoryEntry(string name)
        {
            Name = name;
            SubDirectories = new List<string>();
            Files = new List<string>();
            CreatedAt = DateTime.UtcNow;
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds a subdirectory to this directory.
        /// </summary>
        /// <param name="subDirectoryName">Name of the subdirectory to add.</param>
        public void AddSubDirectory(string subDirectoryName)
        {
            if (!SubDirectories.Contains(subDirectoryName, StringComparer.OrdinalIgnoreCase))
            {
                SubDirectories.Add(subDirectoryName);
                UpdateModificationTime();
            }
        }

        /// <summary>
        /// Removes a subdirectory from this directory.
        /// </summary>
        /// <param name="subDirectoryName">Name of the subdirectory to remove.</param>
        public void RemoveSubDirectory(string subDirectoryName)
        {
            if (SubDirectories.Remove(subDirectoryName))
            {
                UpdateModificationTime();
            }
        }

        /// <summary>
        /// Adds a file to this directory.
        /// </summary>
        /// <param name="fileName">Name of the file to add.</param>
        public void AddFile(string fileName)
        {
            if (!Files.Contains(fileName, StringComparer.OrdinalIgnoreCase))
            {
                Files.Add(fileName);
                UpdateModificationTime();
            }
        }

        /// <summary>
        /// Removes a file from this directory.
        /// </summary>
        /// <param name="fileName">Name of the file to remove.</param>
        public void RemoveFile(string fileName)
        {
            if (Files.Remove(fileName))
            {
                UpdateModificationTime();
            }
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
