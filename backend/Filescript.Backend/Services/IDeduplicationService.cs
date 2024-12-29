
namespace Filescript.Backend.Services {
    /// <summary>
    /// Interface defining deduplication operations.
    /// </summary>
    public interface IDeduplicationService
    {
        /// <summary>
        /// Stores a block of data, ensuring deduplication.
        /// </summary>
        /// <param name="data">Data block to store.</param>
        /// <returns>Index of the stored block.</returns>
        Task<int> StoreBlockAsync(byte[] data);

        /// <summary>
        /// Removes a block of data, updating reference counts.
        /// </summary>
        /// <param name="blockIndex">Index of the block to remove.</param>
        void RemoveBlock(int blockIndex);

        /// <summary>
        /// Performs a basic health check of the DeduplicationService.
        /// </summary>
        /// <returns>True if healthy; otherwise, false.</returns>
        bool BasicHealthCheck();
    }
}