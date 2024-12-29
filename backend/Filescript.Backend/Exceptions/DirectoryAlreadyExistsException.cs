using System;
using System.Runtime.Serialization;

namespace Filescript.Backend.Exceptions
{
    /// <summary>
    /// Exception thrown when attempting to create a directory that already exists.
    /// </summary>
    [Serializable]
    public class DirectoryAlreadyExistsException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryAlreadyExistsException"/> class.
        /// </summary>
        public DirectoryAlreadyExistsException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryAlreadyExistsException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DirectoryAlreadyExistsException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryAlreadyExistsException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="inner">The inner exception reference.</param>
        public DirectoryAlreadyExistsException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryAlreadyExistsException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
        /// <param name="context">The StreamingContext that contains contextual information.</param>
        protected DirectoryAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
