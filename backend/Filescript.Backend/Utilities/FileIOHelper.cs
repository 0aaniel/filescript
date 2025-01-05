using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Filescript.Backend.Utilities
{
    public class FileIOHelper : IDisposable
    {
        private readonly ILogger<FileIOHelper> _logger;
        private readonly string _containerFilePath;
        private readonly int _blockSize;
        private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
        private bool _disposed;

        public FileIOHelper(ILogger<FileIOHelper> logger, string containerFilePath, int blockSize = 4096)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _containerFilePath = containerFilePath ?? throw new ArgumentNullException(nameof(containerFilePath));
            _blockSize = blockSize;
        }

        public async Task InitializeContainerAsync(long totalBlocks)
        {
            _logger.LogInformation("FileIOHelper: Initializing container file '{ContainerFilePath}' with {TotalBlocks} blocks.", _containerFilePath, totalBlocks);

            await _fileLock.WaitAsync();
            try
            {
                // Create the directory if it doesn't exist
                var directory = Path.GetDirectoryName(_containerFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var fs = new FileStream(_containerFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.SetLength(totalBlocks * _blockSize);

                    byte[] emptyBlock = new byte[_blockSize];
                    for (long i = 0; i < totalBlocks; i++)
                    {
                        fs.Seek(i * _blockSize, SeekOrigin.Begin);
                        await fs.WriteAsync(emptyBlock, 0, emptyBlock.Length);
                        await fs.FlushAsync();
                    }
                }

                _logger.LogInformation("FileIOHelper: Container file '{ContainerFilePath}' initialized successfully.", _containerFilePath);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<byte[]> ReadBlockAsync(long blockIndex)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(FileIOHelper));

            byte[] buffer = new byte[_blockSize];

            await _fileLock.WaitAsync();
            try
            {
                using (var fs = new FileStream(_containerFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fs.Seek(blockIndex * _blockSize, SeekOrigin.Begin);
                    await fs.ReadAsync(buffer, 0, _blockSize);
                }

                return buffer;
            }
            finally
            {
                _fileLock.Release();
            }
        }

<<<<<<< HEAD
        /// <summary>
        /// Initializes the container by setting up the required number of blocks.
        /// </summary>
        /// <param name="totalBlocks">Total number of blocks to initialize.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InitializeContainerAsync(long totalBlocks)
        {
            try
            {
                if (!File.Exists(_containerFilePath))
                {
                    using (FileStream fs = new FileStream(_containerFilePath, FileMode.CreateNew, FileAccess.Write))
                    {
                        _logger.LogInformation("Initializing container with {TotalBlocks} blocks.", totalBlocks);
                        byte[] emptyBlock = new byte[_blockSize];
                        for (long i = 0; i < totalBlocks; i++)
                        {
                            await fs.WriteAsync(emptyBlock, 0, _blockSize);
                        }
                    }
                    _logger.LogInformation("Container file '{ContainerFilePath}' initialized with {TotalBlocks} blocks.", _containerFilePath, totalBlocks);
                }
                else
                {
                    _logger.LogInformation("Container file '{ContainerFilePath}' already exists. Skipping initialization.", _containerFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing container file '{ContainerFilePath}'.", _containerFilePath);
                throw;
            }
        }

        /// <summary>
        /// Retrieves the total number of blocks in the container file.
        /// </summary>
        /// <returns>Total number of blocks.</returns>
        public long GetTotalBlocks()
=======
        public async Task WriteBlockAsync(long blockIndex, byte[] data)
>>>>>>> 2c7fc1d452f3d8b7ae4ae8da4de75d31f912fdd3
        {
            if (_disposed) throw new ObjectDisposedException(nameof(FileIOHelper));
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length != _blockSize)
                throw new ArgumentException($"Data must be exactly {_blockSize} bytes.", nameof(data));

            await _fileLock.WaitAsync();
            try
            {
                using (var fs = new FileStream(_containerFilePath, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    fs.Seek(blockIndex * _blockSize, SeekOrigin.Begin);
                    await fs.WriteAsync(data, 0, data.Length);
                    await fs.FlushAsync();
                }

                _logger.LogDebug("Written {DataLength} bytes to block {BlockIndex}.", data.Length, blockIndex);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public string GetContainerFilePath()
        {
            return _containerFilePath;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _fileLock?.Dispose();
                _disposed = true;
            }
        }
    }
}