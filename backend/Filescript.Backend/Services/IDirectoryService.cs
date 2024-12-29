using Filescript.Models;

namespace Filescript.Backend.Services {
    /// <summary>
    /// Interface defining directory operations within the container.
    /// </summary>
    public interface IDirectoryService
    {
        Task MakeDirectoryAsync(string directoryName, string path);
        Task ChangeDirectoryAsync(string targetDirectory);
        Task RemoveDirectoryAsync(string directoryName, string path);
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