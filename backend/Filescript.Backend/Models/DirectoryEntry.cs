using System;
using System.Collections.Generic;

namespace Filescript.Backend.Models
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
        /// The full (or relative) path of the directory.
        /// </summary>
        public string Path { get; set; }

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
            Name = string.Empty;
            Path = string.Empty;
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
            Name = name ?? string.Empty;
            Path = name ?? string.Empty;
            SubDirectories = new List<string>();
            Files = new List<string>();
            CreatedAt = DateTime.UtcNow;
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryEntry"/> class with a specified name and path.
        /// </summary>
        /// <param name="name">Friendly or display name of the directory.</param>
        /// <param name="path">Full or relative path of the directory.</param>
        public DirectoryEntry(string name, string path)
        {
            Name = name ?? string.Empty;
            Path = path ?? string.Empty;
            SubDirectories = new List<string>();
            Files = new List<string>();
            CreatedAt = DateTime.UtcNow;
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds a subdirectory to this directory.
        /// </summary>
        /// <param name="subDirectoryName">Name of the subdirectory to add.</param>
        public void AddSubDirectory(string subDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(subDirectoryPath))
                throw new ArgumentException("Subdirectory path cannot be null or whitespace.", nameof(subDirectoryPath));

            if (!SubDirectories.Contains(subDirectoryPath, StringComparer.OrdinalIgnoreCase))
            {
                SubDirectories.Add(subDirectoryPath);
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
