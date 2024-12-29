/* using Filescript.Utilities;
using System.Threading.Tasks;

namespace Filescript.Backend.Services
{
    /// <summary>
    /// Service handling directory navigation history.
    /// </summary>
    public class DirectoryHistoryService
    {
        private readonly Stack<string> _historyStack;
        private readonly ILogger<DirectoryHistoryService> _logger;

        public DirectoryHistoryService(ILogger<DirectoryHistoryService> logger)
        {
            _historyStack = new Stack<string>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Pushes a directory path onto the history stack.
        /// </summary>
        /// <param name="directoryPath">The directory path to push.</param>
        public void PushDirectory(string directoryPath)
        {
            _historyStack.Push(directoryPath);
            _logger.LogInformation($"DirectoryHistoryService: Pushed directory '{directoryPath}' to history.");
        }

        /// <summary>
        /// Pops the most recent directory path from the history stack.
        /// </summary>
        /// <returns>The directory path.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the stack is empty.</exception>
        public string PopDirectory()
        {
            if (_historyStack.IsEmpty())
                throw new InvalidOperationException("No directory in history.");

            string directoryPath = _historyStack.Pop();
            _logger.LogInformation($"DirectoryHistoryService: Popped directory '{directoryPath}' from history.");
            return directoryPath;
        }

        /// <summary>
        /// Checks if there is any directory in the history.
        /// </summary>
        /// <returns>True if history is not empty; otherwise, false.</returns>
        public bool HasHistory()
        {
            return !_historyStack.IsEmpty();
        }

        /// <summary>
        /// Clears the directory history.
        /// </summary>
        public void ClearHistory()
        {
            _historyStack.Clear();
            _logger.LogInformation("DirectoryHistoryService: Cleared directory history.");
        }
    }
}

*/