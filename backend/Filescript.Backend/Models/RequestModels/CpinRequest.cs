namespace Filescript.Backend.Models.RequestModels
{
    /// <summary>
    /// Request model for copying a file into the container.
    /// </summary>
    public class CopyInRequest
    {
        /// <summary>
        /// Path to the file on the external file system, e.g. "C:\aaa.txt"
        /// </summary>
        public string ExternalFilePath { get; set; }

        /// <summary>
        /// The desired name of the file inside the container, e.g. "bbb.txt"
        /// </summary>
        public string ContainerFileName { get; set; }

        /// <summary>
        /// (Optional) Path in the container's directory structure, if you are using subdirectories.
        /// For simplicity, you could store everything at root ("/").
        /// </summary>
        public string ContainerPath { get; set; } = "/";
    }
}