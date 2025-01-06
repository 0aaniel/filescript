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
        public List<string> Files { get; set; }

        public DirectoryEntry(string name, string path)
        {
            Name = name;
            Path = path;
            SubDirectories = new List<string>();
            Files = new List<string>();
        }

        public void AddSubDirectory(string subDirectoryPath)
        {
            if (!SubDirectories.Contains(subDirectoryPath))
                SubDirectories.Add(subDirectoryPath);
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
