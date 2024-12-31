using System;

namespace Filescript.Backend.Exceptions
{
    /// <summary>
    /// Exception thrown when a specified container is not found.
    /// </summary>
    public class ContainerNotFoundException : Exception
    {
        public ContainerNotFoundException() { }

        public ContainerNotFoundException(string message) : base(message) { }

        public ContainerNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
}
