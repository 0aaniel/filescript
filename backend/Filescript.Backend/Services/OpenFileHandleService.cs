using Filescript.Backend.Services;
using Microsoft.Extensions.Logging;
using System;

namespace Filescript.Services
{
    /// <summary>
    /// Service managing open file handles using a linked list.
    /// </summary>
    public class OpenFileHandleService
    {
        private readonly LinkedList<OpenFileHandle> _openFileHandles;
        private readonly ILogger<OpenFileHandleService> _logger;

        public OpenFileHandleService(ILogger<OpenFileHandleService> logger)
        {
            _openFileHandles = new LinkedList<OpenFileHandle>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Opens a file and adds its handle to the linked list.
        /// </summary>
        public void OpenFile(string filePath)
        {
            var handle = new OpenFileHandle(filePath, DateTime.UtcNow);
            _openFileHandles.AddLast(handle);
            _logger.LogInformation($"OpenFileHandleService: Opened file '{filePath}'.");
        }

        /// <summary>
        /// Closes a file and removes its handle from the linked list.
        /// </summary>
        public bool CloseFile(string filePath)
        {
            var node = _openFileHandles.Find(new OpenFileHandle(filePath, DateTime.MinValue));
            if (node != null)
            {
                _openFileHandles.Remove(node.Value);
                _logger.LogInformation($"OpenFileHandleService: Closed file '{filePath}'.");
                return true;
            }
            _logger.LogWarning($"OpenFileHandleService: Attempted to close non-open file '{filePath}'.");
            return false;
        }

        /// <summary>
        /// Represents an open file handle.
        /// </summary>
        public class OpenFileHandle : IEquatable<OpenFileHandle>
        {
            public string FilePath { get; set; }
            public DateTime OpenedAt { get; set; }

            public OpenFileHandle(string filePath, DateTime openedAt)
            {
                FilePath = filePath;
                OpenedAt = openedAt;
            }

            public bool Equals(OpenFileHandle other)
            {
                if (other == null)
                    return false;
                return FilePath.Equals(other.FilePath, StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as OpenFileHandle);
            }

            public override int GetHashCode()
            {
                return FilePath.GetHashCode(StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
