using Filescript.Backend.Models;
using Filescript.Backend.DataStructures;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Filescript.Backend.Utilities;
using Filescript.Backend.Services.Interfaces;
using Filescript.Models;
using Filescript.Backend.DataStructures.HashTable;

namespace Filescript.Backend.Services
{
    public class DeduplicationService : IDeduplicationService
    {
        private readonly ILogger<DeduplicationService> _logger;
        private readonly ContainerManager _containerManager;
        private readonly string _containerName;
        private readonly HashTable<string, int> _blockHashToIndex;
        private readonly HashTable<int, int> _blockIndexReferenceCount;
        private readonly HashTable<int, string> _blockIndexToHash;
        private readonly Superblock _superblock;
        private ContainerMetadata _metadata;
        private FileIOHelper _fileIOHelper;

        public DeduplicationService(
            ILogger<DeduplicationService> logger,
            ContainerManager containerManager,
            string containerName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _containerManager = containerManager ?? throw new ArgumentNullException(nameof(containerManager));
            _containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));

            // Initialize deduplication mappings
            _blockHashToIndex = new HashTable<string, int>();
            _blockIndexReferenceCount = new HashTable<int, int>();
            _blockIndexToHash = new HashTable<int, string>();

            // Load container resources
            _metadata = _containerManager.GetContainer(_containerName);
            _fileIOHelper = _containerManager.GetFileIOHelper(_containerName);
            _superblock = _containerManager.GetSuperblock(_containerName);

            LoadDeduplicationMappings();
        }

        private void LoadDeduplicationMappings()
        {
            _logger.LogInformation("DeduplicationService: Loading deduplication mappings for container '{ContainerName}'.", _containerName);

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

            _logger.LogInformation("DeduplicationService: Deduplication mappings loaded for container '{ContainerName}'.", _containerName);
        }

        public async Task<int> StoreBlockAsync(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

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

                _logger.LogInformation("DeduplicationService: Duplicate block detected in container '{ContainerName}'. Using existing block at index {BlockIndex}. Reference count: {Count}.",
                    _containerName, existingIndex, _blockIndexReferenceCount.TryGetValue(existingIndex, out int newCount) ? newCount : 1);
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

                _logger.LogInformation("DeduplicationService: Stored new block in container '{ContainerName}' at index {BlockIndex}.", _containerName, newBlockIndex);
                return newBlockIndex;
            }
        }

        public void RemoveBlock(int blockIndex)
        {
            if (_blockIndexReferenceCount.TryGetValue(blockIndex, out int count))
            {
                if (count > 1)
                {
                    _blockIndexReferenceCount.Add(blockIndex, count - 1);
                    _logger.LogInformation("DeduplicationService: Decremented reference count for block {BlockIndex} to {Count} in container '{ContainerName}'.", 
                        blockIndex, count - 1, _containerName);
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
                    _logger.LogInformation("DeduplicationService: Block {BlockIndex} in container '{ContainerName}' is no longer referenced and has been freed.", 
                        blockIndex, _containerName);
                }
            }
            else
            {
                _logger.LogWarning("DeduplicationService: Attempted to remove non-existent block {BlockIndex} from container '{ContainerName}'.", 
                    blockIndex, _containerName);
            }
        }

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
            _logger.LogInformation("DeduplicationService: Performing basic health check for container '{ContainerName}'.", _containerName);

            try
            {
                // Verify that internal hash tables are operational
                string testHash = "TEST_HASH";
                int testIndex = 99999;

                _blockHashToIndex.Add(testHash, testIndex);
                bool removed = _blockHashToIndex.Remove(testHash);

                if (!removed)
                {
                    _logger.LogError("DeduplicationService: Health check failed for container '{ContainerName}': Could not remove test hash.", _containerName);
                    return false;
                }

                _logger.LogInformation("DeduplicationService: Basic health check passed for container '{ContainerName}'.", _containerName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeduplicationService: Exception during basic health check for container '{ContainerName}'.", _containerName);
                return false;
            }
        }
    }
}