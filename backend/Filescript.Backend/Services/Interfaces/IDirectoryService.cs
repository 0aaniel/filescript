using Filescript.Backend.Models;
using Filescript.Models;
<<<<<<< HEAD

namespace Filescript.Backend.Services.Interfaces {
    /// <summary>
    /// Interface defining directory operations within the container.
=======
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Filescript.Backend.Services
{
    /// <summary>
    /// Interface for directory-related operations.
>>>>>>> 2c7fc1d452f3d8b7ae4ae8da4de75d31f912fdd3
    /// </summary>
    public interface IDirectoryService
    {
        Task<bool> MakeDirectoryAsync(string directoryName, string path);
<<<<<<< HEAD
        Task<bool> ChangeDirectoryAsync(string targetDirectory);
        Task<bool> RemoveDirectoryAsync(string directoryName, string path);
        List<DirectoryEntry> ListDirectories();

        /// <summary>
        /// Retrieves the current directory path.
        /// </summary>
        /// <returns>The current directory path as a string.</returns>
        string GetCurrentDirectory();

        /// <summary>
        /// Performs a basic health check of the DirectoryService.
        /// </summary>
        /// <returns>True if healthy; otherwise, false.</returns>
        Task<bool> BasicHealthCheckAsync();
    }
}
=======
        Task<bool> ChangeDirectoryAsync(string targetPath);
        Task<bool> RemoveDirectoryAsync(string directoryName, string path);
        List<DirectoryEntry> ListDirectories();
        string GetCurrentDirectory();
        Task<bool> BasicHealthCheckAsync();
    }
}
>>>>>>> 2c7fc1d452f3d8b7ae4ae8da4de75d31f912fdd3
