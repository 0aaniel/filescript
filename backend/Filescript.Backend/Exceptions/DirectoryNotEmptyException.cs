using System;
using System.Runtime.Serialization;

namespace Filescript.Exceptions
{
    /// <summary>
    /// Exception thrown when attempting to remove a directory that is not empty.
    /// </summary>
    [Serializable]
    public class DirectoryNotEmptyException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryNotEmptyException"/> class.
        /// </summary>
        public DirectoryNotEmptyException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryNotEmptyException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DirectoryNotEmptyException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryNotEmptyException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="inner">The inner exception reference.</param>
        public DirectoryNotEmptyException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryNotEmptyException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
        /// <param name="context">The StreamingContext that contains contextual information.</param>
        protected DirectoryNotEmptyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
