using System;

namespace Filescript.Backend.Exceptions
{
    /// <summary>
    /// Exception thrown when a specified directory is not found.
    /// </summary>
    public class DirectoryNotFoundException : Exception
    {
        public DirectoryNotFoundException() { }

        public DirectoryNotFoundException(string message) : base(message) { }

        public DirectoryNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
