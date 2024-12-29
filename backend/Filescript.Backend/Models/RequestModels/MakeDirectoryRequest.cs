using System.ComponentModel.DataAnnotations;

namespace Filescript.Models.RequestModels
{
    /// <summary>
    /// Represents a request to create a new directory.
    /// </summary>
    public class MakeDirectoryRequest
    {
        /// <summary>
        /// Gets or sets the name of the directory to be created.
        /// </summary>
        [Required]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "Directory name must be between 1 and 255 characters.")]
        public required string DirectoryName { get; set; }

        /// <summary>
        /// Gets or sets the path where the directory should be created.
        /// If null or empty, the directory is created in the current directory.
        /// </summary>
        public required string Path { get; set; }
    }
}
