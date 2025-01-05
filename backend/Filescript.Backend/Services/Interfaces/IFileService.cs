<<<<<<< HEAD
using Filescript.Backend.Models;
using System.Collections.Generic;
using System.IO.Enumeration;
using System.Threading.Tasks;

namespace Filescript.Backend.Services.Interfaces {

    /// <summary>
    /// Interface defining file operations within the container.
    /// </summary>
    public interface IFileService {

        /// <summary>
        /// Copies a file from the external file system into the container.
        /// </summary>
        /// <param name="sourcePath">Full path of the source file.</param>
        /// <param name="destName">Name of the destination file within the container.</param>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>
        Task<bool> CopyInAsync(string sourcePath, string destName);
        
        /// <summary>
        /// Copies a file from the container to the external file system.
        /// </summary>
        /// <param name="sourceName">Name of the source file within the container.</param>
        /// <param name="destPath">Full path of the destination file.</param>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>        /// <returns>True if the operation succeeds; otherwise, false.</returns>        Task<bool> CopyInAsync(string sourcePath, string destName);
        Task<bool> CopyOutAsync(string sourceName, string destPath);

        /// <summary>
        /// Removes a file from the container.
        /// </summary>
        /// <param name="fileName">Name of the file to remove.</param>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>        /// <returns>True if the operation succeeds; otherwise, false.</returns>
        Task<bool> RemoveFileAsync(string fileName);

        /// <summary>
        /// Lists all files in the current directory of the container.
        /// </summary>
        /// <returns>List of <see cref="FileEntry"/> objects representing the files.</returns>
        List<FileEntry> ListFiles();

        /// <summary>
        /// Performs a basic health check of the FileService.
        /// </summary>
        /// <returns>True if healthy; otherwise, false.</returns>
        Task<bool> BasicHealthCheckAsync();
    }
}
=======
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
>>>>>>> 2c7fc1d452f3d8b7ae4ae8da4de75d31f912fdd3
