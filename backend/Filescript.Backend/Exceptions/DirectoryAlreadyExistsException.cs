using System;

namespace Filescript.Backend.Exceptions
{
    /// <summary>
    /// Exception thrown when attempting to create a directory that already exists.
    /// </summary>
    public class DirectoryAlreadyExistsException : Exception
    {
        public DirectoryAlreadyExistsException() { }

        public DirectoryAlreadyExistsException(string message) : base(message) { }

        public DirectoryAlreadyExistsException(string message, Exception inner) : base(message, inner) { }
    }
}
