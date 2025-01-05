using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Filescript.Backend.Utilities
{
    /// <summary>
    /// Helper class for handling low-level file I/O operations with the container file.
    /// </summary>
    public class FileIOHelper : IDisposable
    {
        private readonly ILogger<FileIOHelper> _logger;
        private readonly string _containerFilePath;
        private readonly int _blockSize;
        private readonly FileStream _fileStream;
        private bool _disposed = false;

        /// <summary>
        /// Gets the path to the container file.
        /// </summary>
        public string ContainerFilePath => _containerFilePath;

        /// <summary>
        /// Gets the size of each block in bytes.
        /// </summary>
        public int BlockSize => _blockSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileIOHelper"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for logging.</param>
        /// <param name="containerFilePath">Path to the container file.</param>
        /// <param name="blockSize">Size of each block in bytes.</param>
        public FileIOHelper(ILogger<FileIOHelper> logger, string containerFilePath, int blockSize = 4096)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _containerFilePath = containerFilePath ?? throw new ArgumentNullException(nameof(containerFilePath));
            _blockSize = blockSize;

            // Ensure the container file exists
            if (!File.Exists(_containerFilePath))
            {
                _logger.LogInformation("Container file does not exist. Creating new container file at {Path}.", _containerFilePath);
                using (var fs = new FileStream(_containerFilePath, FileMode.CreateNew, FileAccess.Write))
                {
                    // Optionally, initialize the container with a specific size or structure
                }
            }

            // Open the container file for reading and writing
            _fileStream = new FileStream(_containerFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            _logger.LogInformation("Opened container file at {Path} with block size {BlockSize} bytes.", _containerFilePath, _blockSize);
        }

        /// <summary>
        /// Writes data to a specific block in the container file.
        /// </summary>
        /// <param name="blockIndex">Index of the block to write to.</param>
        /// <param name="data">Data to write. Must not exceed the block size.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when blockIndex is negative.</exception>
        /// <exception cref="ArgumentException">Thrown when data length exceeds block size.</exception>
        public async Task WriteBlockAsync(int blockIndex, byte[] data)
        {
            if (blockIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(blockIndex), "Block index cannot be negative.");

            if (data.Length > _blockSize)
                throw new ArgumentException($"Data length {data.Length} exceeds block size {_blockSize} bytes.", nameof(data));

            long position = (long)blockIndex * _blockSize;
            _fileStream.Seek(position, SeekOrigin.Begin);

            byte[] buffer = new byte[_blockSize];
            Array.Copy(data, buffer, data.Length);
            if (data.Length < _blockSize)
            {
                // Padding remaining bytes with zeros
                Array.Clear(buffer, data.Length, _blockSize - data.Length);
            }

            await _fileStream.WriteAsync(buffer, 0, _blockSize);
            await _fileStream.FlushAsync();

            _logger.LogInformation("Written data to block {BlockIndex} at position {Position}.", blockIndex, position);
        }

        /// <summary>
        /// Reads data from a specific block in the container file.
        /// </summary>
        /// <param name="blockIndex">Index of the block to read from.</param>
        /// <returns>A task representing the asynchronous operation, containing the data read.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when blockIndex is negative.</exception>
        public async Task<byte[]> ReadBlockAsync(int blockIndex)
        {
            if (blockIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(blockIndex), "Block index cannot be negative.");

            long position = (long)blockIndex * _blockSize;
            _fileStream.Seek(position, SeekOrigin.Begin);

            byte[] buffer = new byte[_blockSize];
            int bytesRead = await _fileStream.ReadAsync(buffer, 0, _blockSize);

            if (bytesRead == 0)
            {
                _logger.LogWarning("ReadBlockAsync: No data read from block {BlockIndex}. Returning empty data.", blockIndex);
                return new byte[0];
            }

            _logger.LogInformation("Read data from block {BlockIndex} at position {Position}. Bytes read: {BytesRead}.", blockIndex, position, bytesRead);

            return buffer;
        }

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
        {
            long totalBlocks = _fileStream.Length / _blockSize;
            _logger.LogInformation("Container file has {TotalBlocks} blocks.", totalBlocks);
            return totalBlocks;
        }

        /// <summary>
        /// Closes the container file stream.
        /// </summary>
        public void Close()
        {
            _fileStream.Close();
            _logger.LogInformation("Closed container file at {Path}.", _containerFilePath);
        }

        /// <summary>
        /// Ensures that all pending data is written to the container file.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task FlushAsync()
        {
            await _fileStream.FlushAsync();
            _logger.LogInformation("Flushed container file at {Path}.", _containerFilePath);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="FileIOHelper"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing">Indicates whether to release managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _fileStream?.Dispose();
                    _logger.LogInformation("Disposed FileIOHelper and closed container file at {Path}.", _containerFilePath);
                }

                _disposed = true;
            }
        }
    }
}
