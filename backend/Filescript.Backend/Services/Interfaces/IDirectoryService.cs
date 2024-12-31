using Filescript.Backend.Models;
using Filescript.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Filescript.Backend.Services
{
    /// <summary>
    /// Interface for directory-related operations.
    /// </summary>
    public interface IDirectoryService
    {
        Task<bool> MakeDirectoryAsync(string directoryName, string path);
        Task<bool> ChangeDirectoryAsync(string targetPath);
        Task<bool> RemoveDirectoryAsync(string directoryName, string path);
        List<DirectoryEntry> ListDirectories();
        string GetCurrentDirectory();
        Task<bool> BasicHealthCheckAsync();
    }
}
