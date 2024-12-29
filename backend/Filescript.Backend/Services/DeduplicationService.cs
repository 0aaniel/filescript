
using System.Security.Cryptography;
using System.Text;
using Filescript.Backend.Utilities;
using Filescript.Utilities;

namespace Filescript.Backend.Services {

    /// <summary>
    /// Service handling deduplication of data blocks.
    /// </summary>
    public class DeduplicationService : IDeduplicationService
    {
        private readonly ILogger<DeduplicationService> _logger;
        private readonly FileIOHelper _fileIOHelper;
        private readonly HashTable<string, int> _blockHashToIndex;
        private readonly HashTable<int, int> _blockIndexReferenceCount;

        public DeduplicationService(
            ILogger<DeduplicationService> logger,
            FileIOHelper fileIOHelper)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileIOHelper = fileIOHelper ?? throw new ArgumentNullException(nameof(fileIOHelper));

            _blockHashToIndex = new HashTable<string, int>();
            _blockIndexReferenceCount = new HashTable<int, int>();
        }

        public async Task<int> StoreBlockAsync(byte[] data)
        {
            string hash = ComputeHash(data);

            if (_blockHashToIndex.TryGetValue(hash, out int existingIndex))
            {
                // Increment reference count
                if (_blockIndexReferenceCount.TryGetValue(existingIndex, out int refCount))
                {
                    _blockIndexReferenceCount.Add(existingIndex, refCount + 1);
                }
                else
                {
                    _blockIndexReferenceCount.Add(existingIndex, 1);
                }

                _logger.LogInformation("DeduplicationService: Block already exists at index {Index}. Incremented reference count.", existingIndex);
                return existingIndex;
            }
            else
            {
                // Store new block
                int newIndex = (int)_fileIOHelper.GetTotalBlocks();
                await _fileIOHelper.WriteBlockAsync(newIndex, data);
                _blockHashToIndex.Add(hash, newIndex);
                _blockIndexReferenceCount.Add(newIndex, 1);

                _logger.LogInformation("DeduplicationService: Stored new block at index {Index}.", newIndex);
                return newIndex;
            }        
        }

        public void RemoveBlock(int blockIndex)
        {
            if (_blockIndexReferenceCount.TryGetValue(blockIndex, out int refCount))
            {
                if (refCount > 1)
                {
                    _blockIndexReferenceCount.Add(blockIndex, refCount - 1);
                    _logger.LogInformation("DeduplicationService: Decremented reference count for block {Index} to {RefCount}.", blockIndex, refCount - 1);
                }
                else
                {
                    _blockIndexReferenceCount.Remove(blockIndex);
                    // Retrieve hash to remove from hash table
                    string hash = GetHashByBlockIndex(blockIndex);
                    if (!string.IsNullOrEmpty(hash))
                    {
                        _blockHashToIndex.Remove(hash);
                    }

                    _logger.LogInformation("DeduplicationService: Removed block {Index} as its reference count reached zero.", blockIndex);
                }
            }
            else
            {
                _logger.LogWarning("DeduplicationService: Attempted to remove non-existent block {Index}.", blockIndex);
            }
        }

        public bool BasicHealthCheck()
        {
            _logger.LogInformation("DeduplicationService: Performing basic health check.");

            try
            {
                // Example health check: Verify that internal hash tables are operational
                // Perform a dummy add and remove operation
                string testHash = "TEST_HASH";
                int testIndex = 99999;

                _blockHashToIndex.Add(testHash, testIndex);
                bool removed = _blockHashToIndex.Remove(testHash);

                if (!removed)
                {
                    _logger.LogError("DeduplicationService: Failed to remove test hash.");
                    return false;
                }

                _logger.LogInformation("DeduplicationService: Basic health check passed.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeduplicationService: Exception during basic health check.");
                return false;
            }
        }

        private string ComputeHash(byte[] data) {
            
            // Compute SHA-256 hash of the data
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(data);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("X2"));

                return sb.ToString();
            }
        }

        private string GetHashByBlockIndex(int blockIndex)
        {
            foreach (var kvp in _blockHashToIndex.GetAllValues())
            {
                if (kvp == blockIndex)
                {
                    // TODO
                    // Assuming the hash can be retrieved; implement accordingly
                    // Placeholder implementation
                    return "PLACEHOLDER_HASH";
                }
            }
            return null;
        }
    }
}