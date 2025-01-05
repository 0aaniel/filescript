using System.Collections.Generic;
using System.Text.Json;

namespace Filescript.Backend.Models
{
    /// <summary>
    /// Represents a directory entry in the container metadata.
    /// </summary>
    public class DirectoryEntry
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public List<string> SubDirectories { get; set; }
<<<<<<< HEAD

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
=======
        public List<string> Files { get; set; }

        public DirectoryEntry(string name, string path)
        {
            Name = name;
            Path = path;
>>>>>>> 2c7fc1d452f3d8b7ae4ae8da4de75d31f912fdd3
            SubDirectories = new List<string>();
            Files = new List<string>();
        }

<<<<<<< HEAD
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
=======
        public void AddSubDirectory(string subDirectoryPath)
        {
            if (!SubDirectories.Contains(subDirectoryPath))
                SubDirectories.Add(subDirectoryPath);
>>>>>>> 2c7fc1d452f3d8b7ae4ae8da4de75d31f912fdd3
        }

        public void RemoveSubDirectory(string subDirectoryPath)
        {
            if (SubDirectories.Contains(subDirectoryPath))
                SubDirectories.Remove(subDirectoryPath);
        }

        public byte[] Serialize()
        {
            return JsonSerializer.SerializeToUtf8Bytes(this);
        }
    }
}
