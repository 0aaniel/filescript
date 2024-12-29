using System.Threading.Tasks;

namespace Filescript.Backend.Services
{
    /// <summary>
    /// Interface defining resiliency operations.
    /// </summary>
    public interface IResiliencyService
    {
        /// <summary>
        /// Checks the integrity of a specific block.
        /// </summary>
        /// <param name="blockIndex">Index of the block to check.</param>
        /// <returns>True if the block is intact; otherwise, false.</returns>
        Task<bool> CheckBlockIntegrityAsync(int blockIndex);

        /// <summary>
        /// Performs a basic health check of the ResiliencyService.
        /// </summary>
        /// <returns>True if healthy; otherwise, false.</returns>
        bool BasicHealthCheck();
    }
}