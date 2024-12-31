using System;

namespace Filescript.Backend.Exceptions
{
    /// <summary>
    /// Exception thrown when attempting to create a file that already exists.
    /// </summary>
    public class FileAlreadyExistsException : Exception
    {
        public FileAlreadyExistsException() { }

        public FileAlreadyExistsException(string message) : base(message) { }

        public FileAlreadyExistsException(string message, Exception inner) : base(message, inner) { }
    }
}
