namespace Filescript.Models
{
    /// <summary>
    /// Represents a file entry in the container metadata.
    /// </summary>
    public class FileEntry
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int StartBlock { get; set; }
        public int Length { get; set; }
        public List<int> BlockIndices { get; set; } = new List<int>();

        public FileEntry(string name, string path, int startBlock, int length)
        {
            Name = name;
            Path = path;
            StartBlock = startBlock;
            Length = length;
            
        }
    }
}
