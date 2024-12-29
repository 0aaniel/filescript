using System.ComponentModel.DataAnnotations;

namespace Filescript.Models.Requests
{
    /// <summary>
    /// Represents a request to change the current working directory.
    /// </summary>
    public class ChangeDirectoryRequest
    {
        /// <summary>
        /// Gets or sets the target directory path to navigate to.
        /// </summary>
        [Required]
        [StringLength(1024, MinimumLength = 1, ErrorMessage = "Path must be between 1 and 1024 characters.")]
        public required string TargetDirectory { get; set; }
    }
}
