namespace Filescript.Backend.Models.RequestModels
{
    /// <summary>
    /// Request model for copying a file into the container.
    /// </summary>
    public class CpinRequest {
        /// <summary>
        /// Full path of the source file in the external file system.
        /// </summary>
        public required string SourcePath { get; set; }

        /// <summary>
        /// Name of the destination file within the container.
        /// </summary>
        public required string DestName { get; set; }
    }
}