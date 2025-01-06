namespace Filescript.Backend.Models.RequestModels
{
    /// <summary>
    /// Request model for copying a file out of the container.
    /// </summary>
    public class CopyOutRequest
    {
        /// <summary>
        /// The name of the file as it exists in the container, e.g. "bbb.txt"
        /// </summary>
        public string ContainerFileName { get; set; }

        /// <summary>
        /// The external file path to write the container file to, e.g. "C:\ttt.txt"
        /// </summary>
        public string ExternalFilePath { get; set; }

        /// <summary>
        /// (Optional) Path in the container's directory structure.
        /// For simplicity, you could store everything at root ("/").
        /// </summary>
        public string ContainerPath { get; set; } = "/";
    }
}