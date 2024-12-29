namespace Filescript.Backend.Models.RequestModels
{
    /// <summary>
    /// Request model for copying a file out of the container.
    /// </summary>
    public class CpoutRequest
    {
        /// <summary>
        /// Name of the source file within the container.
        /// </summary>
        public required string SourceName { get; set; }

        /// <summary>
        /// Full path of the destination file in the external file system.
        /// </summary>
        public required string DestPath { get; set; }
    }
}