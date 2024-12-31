using System;

namespace Filescript.Backend.Exceptions
{
    /// <summary>
    /// Exception thrown when a specified file is not found.
    /// </summary>
    public class FileNotFoundException : Exception
    {
        public FileNotFoundException() { }

        public FileNotFoundException(string message) : base(message) { }

        public FileNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
}
