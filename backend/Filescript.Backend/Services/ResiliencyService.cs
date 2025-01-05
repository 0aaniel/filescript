using System.Security.Cryptography;
using System.Text;
using Filescript.Backend.Utilities;
using Filescript.Backend.Services.Interfaces;

namespace Filescript.Backend.Services {

    /// <summary>
    /// Service handling resiliency and data integrity checks.
    /// </summary>
    public class ResiliencyService : IResiliencyService
    {
        private readonly ILogger<ResiliencyService> _logger;
        private readonly FileIOHelper _fileIOHelper;

        public ResiliencyService(
            ILogger<ResiliencyService> logger,
            FileIOHelper fileIOHelper)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileIOHelper = fileIOHelper ?? throw new ArgumentNullException(nameof(fileIOHelper));
        }

        public async Task<bool> CheckBlockIntegrityAsync(int blockIndex)
        {
            _logger.LogInformation("ResiliencyService: Checking integrity of block {Index}.", blockIndex);

            try
            {
                byte[] data = await _fileIOHelper.ReadBlockAsync(blockIndex);
                string currentHash = ComputeHash(data);

                // Retrieve stored hash from metadata or a separate storage
                string storedHash = GetStoredHash(blockIndex);

                if (currentHash.Equals(storedHash, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("ResiliencyService: Block {Index} integrity verified.", blockIndex);
                    return true;
                }
                else
                {
                    _logger.LogError("ResiliencyService: Block {Index} integrity check failed.", blockIndex);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResiliencyService: Exception during integrity check of block {Index}.", blockIndex);
                return false;
            }
        }

        public bool BasicHealthCheck()
        {
            _logger.LogInformation("ResiliencyService: Performing basic health check.");

            try
            {
                // Example health check: Verify that hashing functions are operational
                byte[] testData = Encoding.UTF8.GetBytes("TestData");
                string hash = ComputeHash(testData);

                if (string.IsNullOrEmpty(hash))
                {
                    _logger.LogError("ResiliencyService: Hash computation returned empty string.");
                    return false;
                }

                _logger.LogInformation("ResiliencyService: Basic health check passed.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResiliencyService: Exception during basic health check.");
                return false;
            }
        }

        private string ComputeHash(byte[] data)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hashBytes = sha.ComputeHash(data);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("X2"));
                return sb.ToString();
            }
        }

        private string GetStoredHash(int blockIndex)
        {
            // Implement retrieval of stored hash from metadata or a separate storage system
            // Placeholder implementation
            return "PLACEHOLDER_HASH";
        }
    }
}