using System;

namespace Filescript.Backend.Exceptions
{
    /// <summary>
    /// Exception thrown when attempting to create a container that already exists.
    /// </summary>
    public class ContainerAlreadyExistsException : Exception
    {
        public ContainerAlreadyExistsException() { }

        public ContainerAlreadyExistsException(string message) : base(message) { }

        public ContainerAlreadyExistsException(string message, Exception inner) : base(message, inner) { }
    }
}
