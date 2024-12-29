using Filescript.Backend.Models;
using Filescript.Backend.DataStructures;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Filescript.Utilities;
using Filescript.Backend.Services;
using Filescript.Models;
using Filescript.Backend.DataStructures.HashTable;

namespace Filescript.Services
{
    /// <summary>
    /// Service handling deduplication of data blocks.
    /// </summary>
    public class DeduplicationService : IDeduplicationService
    {
        private readonly ILogger<DeduplicationService> _logger;
        private readonly FileIOHelper _fileIOHelper;
        private readonly ContainerMetadata _metadata;
        private readonly Superblock _superblock;
        private readonly HashTable<string, int> _blockHashToIndex;
        private readonly HashTable<int, int> _blockIndexReferenceCount;
        private readonly HashTable<int, string> _blockIndexToHash;

        public DeduplicationService(ILogger<DeduplicationService> logger, FileIOHelper fileIOHelper, ContainerMetadata metadata)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileIOHelper = fileIOHelper ?? throw new ArgumentNullException(nameof(fileIOHelper));
            _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

            // Load superblock from block 0
            byte[] superblockData = _fileIOHelper.ReadBlockAsync(0).Result;
            _superblock = Superblock.Deserialize(superblockData);

            // Initialize deduplication mappings
            _blockHashToIndex = new HashTable<string, int>();
            _blockIndexReferenceCount = new HashTable<int, int>();
            _blockIndexToHash = new HashTable<int, string>();

            LoadDeduplicationMappings();
        }

        private void LoadDeduplicationMappings()
        {
            // Iterate through all files and populate deduplication mappings
            foreach (var file in _metadata.Files.Values)
            {
                foreach (var blockIndex in file.BlockIndices)
                {
                    // Read block data
                    byte[] blockData = _fileIOHelper.ReadBlockAsync(blockIndex).Result;
                    string hash = ComputeHash(blockData);

                    if (_blockHashToIndex.TryGetValue(hash, out int existingIndex))
                    {
                        _blockIndexReferenceCount.Add(existingIndex, _blockIndexReferenceCount.TryGetValue(existingIndex, out int count) ? count + 1 : 1);
                    }
                    else
                    {
                        _blockHashToIndex.Add(hash, blockIndex);
                        _blockIndexReferenceCount.Add(blockIndex, 1);
                        _blockIndexToHash.Add(blockIndex, hash);
                    }

                    _metadata.FreeBlocks.Remove(blockIndex); // Block is in use
                }
            }

            _logger.LogInformation("DeduplicationService: Loaded deduplication mappings.");
        }

        /// <summary>
        /// Stores a block of data, deduplicating if possible.
        /// </summary>
        public async Task<int> StoreBlockAsync(byte[] data)
        {
            string hash = ComputeHash(data);

            if (_blockHashToIndex.TryGetValue(hash, out int existingIndex))
            {
                // Increment reference count
                if (_blockIndexReferenceCount.TryGetValue(existingIndex, out int count))
                {
                    _blockIndexReferenceCount.Add(existingIndex, count + 1);
                }
                else
                {
                    _blockIndexReferenceCount.Add(existingIndex, 1);
                }

                _logger.LogInformation($"DeduplicationService: Duplicate block detected. Existing at index {existingIndex}. Incremented reference count to {(_blockIndexReferenceCount.TryGetValue(existingIndex, out int newCount) ? newCount : 1)}.");
                return existingIndex;
            }
            else
            {
                // Allocate a new block
                int newBlockIndex = _metadata.AllocateBlock();
                await _fileIOHelper.WriteBlockAsync(newBlockIndex, data);

                // Update deduplication mappings
                _blockHashToIndex.Add(hash, newBlockIndex);
                _blockIndexReferenceCount.Add(newBlockIndex, 1);
                _blockIndexToHash.Add(newBlockIndex, hash);

                _logger.LogInformation($"DeduplicationService: Stored new block at index {newBlockIndex} with hash {hash}.");
                return newBlockIndex;
            }
        }

        /// <summary>
        /// Removes a block, decrementing its reference count and freeing it if necessary.
        /// </summary>
        public void RemoveBlock(int blockIndex)
        {
            if (_blockIndexReferenceCount.TryGetValue(blockIndex, out int count))
            {
                if (count > 1)
                {
                    _blockIndexReferenceCount.Add(blockIndex, count - 1);
                    _logger.LogInformation($"DeduplicationService: Decremented reference count for block {blockIndex} to {count - 1}.");
                }
                else
                {
                    // Remove block from deduplication mappings
                    if (_blockIndexToHash.TryGetValue(blockIndex, out string hash))
                    {
                        _blockHashToIndex.Remove(hash);
                        _blockIndexToHash.Remove(blockIndex);
                    }

                    // Free the block
                    _metadata.FreeBlock(blockIndex);
                    _logger.LogInformation($"DeduplicationService: Block {blockIndex} is no longer referenced and has been freed.");
                }
            }
            else
            {
                _logger.LogWarning($"DeduplicationService: Attempted to remove non-existent block {blockIndex}.");
            }
        }

        /// <summary>
        /// Computes the SHA-256 hash of the given data.
        /// </summary>
        private string ComputeHash(byte[] data)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hashBytes = sha.ComputeHash(data);
                return BitConverter.ToString(hashBytes).Replace("-", "");
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
    }
}
