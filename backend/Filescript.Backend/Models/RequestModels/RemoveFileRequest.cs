namespace Filescript.Backend.Models.RequestModels
{
    /// <summary>
    /// Request model for removing a file from the container.
    /// </summary>
    public class RemoveFileRequest
    {
        /// <summary>
        /// Name of the file to remove from the container.
        /// </summary>
        public required string FileName { get; set; }
    }
}