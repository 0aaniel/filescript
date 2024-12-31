/* using Filescript.Utilities;
using Microsoft.Extensions.Logging;
using System;

namespace Filescript.Services
{
    /// <summary>
    /// Service handling file operation history for undo functionality.
    /// </summary>
    public class FileOperationHistoryService
    {
        private readonly Stack<FileOperation> _operationStack;
        private readonly ILogger<FileOperationHistoryService> _logger;

        public FileOperationHistoryService(ILogger<FileOperationHistoryService> logger)
        {
            _operationStack = new Stack<FileOperation>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Pushes a file operation onto the history stack.
        /// </summary>
        /// <param name="operation">The file operation to record.</param>
        public void PushOperation(FileOperation operation)
        {
            _operationStack.Push(operation);
            _logger.LogInformation($"FileOperationHistoryService: Pushed operation '{operation.OperationType}' for file '{operation.FilePath}'.");
        }

        /// <summary>
        /// Pops the most recent file operation from the history stack.
        /// </summary>
        /// <returns>The file operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the stack is empty.</exception>
        public FileOperation PopOperation()
        {
            if (_operationStack.IsEmpty())
                throw new InvalidOperationException("No file operations in history.");

            var operation = _operationStack.Pop();
            _logger.LogInformation($"FileOperationHistoryService: Popped operation '{operation.OperationType}' for file '{operation.FilePath}'.");
            return operation;
        }

        /// <summary>
        /// Checks if there are any operations in history.
        /// </summary>
        /// <returns>True if there are operations; otherwise, false.</returns>
        public bool HasOperations()
        {
            return !_operationStack.IsEmpty();
        }

        /// <summary>
        /// Clears the operation history.
        /// </summary>
        public void ClearHistory()
        {
            _operationStack.Clear();
            _logger.LogInformation("FileOperationHistoryService: Cleared file operation history.");
        }
    }

    /// <summary>
    /// Represents a file operation.
    /// </summary>
    public class FileOperation
    {
        public string FilePath { get; set; }
        public FileOperationType OperationType { get; set; }

        public FileOperation(string filePath, FileOperationType operationType)
        {
            FilePath = filePath;
            OperationType = operationType;
        }
    }

    /// <summary>
    /// Enum representing types of file operations.
    /// </summary>
    public enum FileOperationType
    {
        Create,
        Delete,
        Modify
    }
}
*/