using Filescript.Models;

namespace Filescript.Backend.Services {
    /// <summary>
    /// Interface defining directory operations within the container.
    /// </summary>
    public interface IDirectoryService
    {
        Task<bool> MakeDirectoryAsync(string directoryName);
        Task<bool> ChangeDirectoryAsync(string targetDirectory);
        Task<bool> RemoveDirectoryAsync(string directoryName);
        List<DirectoryEntry> ListDirectories();

        /// <summary>
        /// Performs a basic health check of the DirectoryService.
        /// </summary>
        /// <returns>True if healthy; otherwise, false.</returns>
        Task<bool> BasicHealthCheckAsync();
    }
}