using System.Threading.Tasks;

namespace Filescript.Backend.Services
{
    /// <summary>
    /// Interface for file-related operations.
    /// </summary>
    public interface IFileService
    {
        Task<bool> CreateFileAsync(string fileName, string path, byte[] content);
        Task<byte[]> ReadFileAsync(string fileName, string path);
        Task<bool> DeleteFileAsync(string fileName, string path);
        // Add other file-related methods as needed
        Task<bool> BasicHealthCheckAsync();
    }
}
