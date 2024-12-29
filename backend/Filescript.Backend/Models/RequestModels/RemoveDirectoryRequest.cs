using System.ComponentModel.DataAnnotations;

namespace Filescript.Models.Requests
{
    /// <summary>
    /// Represents a request to remove an existing directory.
    /// </summary>
    public class RemoveDirectoryRequest
    {
        /// <summary>
        /// Gets or sets the name of the directory to be removed.
        /// </summary>
        [Required]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "Directory name must be between 1 and 255 characters.")]
        public required string DirectoryName { get; set; }

        /// <summary>
        /// Gets or sets the path of the directory to be removed.
        /// If null or empty, the directory is removed from the current directory.
        /// </summary>
        public required string Path { get; set; }
    }
}
