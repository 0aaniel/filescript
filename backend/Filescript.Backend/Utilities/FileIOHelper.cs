using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Filescript.Backend.Utilities
{
    /// <summary>
    /// Helper class for performing file I/O operations on the container file.
    /// </summary>
    public class FileIOHelper : IDisposable
    {
        private readonly ILogger<FileIOHelper> _logger;
        private readonly string _containerFilePath;
        private readonly int _blockSize;
        private FileStream _fileStream;

        public FileIOHelper(ILogger<FileIOHelper> logger, string containerFilePath, int blockSize = 4096)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _containerFilePath = containerFilePath ?? throw new ArgumentNullException(nameof(containerFilePath));
            _blockSize = blockSize;

            // Open the file stream
            _fileStream = new FileStream(_containerFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        /// <summary>
        /// Initializes the container file with the specified number of blocks.
        /// </summary>
        public async Task InitializeContainerAsync(long totalBlocks)
        {
            _logger.LogInformation("FileIOHelper: Initializing container file '{ContainerFilePath}' with {TotalBlocks} blocks.", _containerFilePath, totalBlocks);

            // Set the file size to totalBlocks * blockSize
            _fileStream.SetLength(totalBlocks * _blockSize);

            // Optionally, write zeros or some initial data
            byte[] emptyBlock = new byte[_blockSize];
            for (long i = 0; i < totalBlocks; i++)
            {
                await WriteBlockAsync(i, emptyBlock);
            }

            _logger.LogInformation("FileIOHelper: Container file '{ContainerFilePath}' initialized successfully.", _containerFilePath);
        }

        /// <summary>
        /// Reads a block at the specified index.
        /// </summary>
        /// <param name="blockIndex">The index of the block to read.</param>
        /// <returns>A byte array containing the block's data.</returns>
        public byte[] ReadBlock(long blockIndex)
        {
            _logger.LogInformation("FileIOHelper: Reading block {BlockIndex} from '{ContainerFilePath}'.", blockIndex, _containerFilePath);

            byte[] buffer = new byte[_blockSize];
            _fileStream.Seek(blockIndex * _blockSize, SeekOrigin.Begin);
            _fileStream.Read(buffer, 0, _blockSize);
            return buffer;
        }

        /// <summary>
        /// Asynchronously reads a block at the specified index.
        /// </summary>
        /// <param name="blockIndex">The index of the block to read.</param>
        /// <returns>A byte array containing the block's data.</returns>
        public async Task<byte[]> ReadBlockAsync(long blockIndex)
        {
            _logger.LogInformation("FileIOHelper: Asynchronously reading block {BlockIndex} from '{ContainerFilePath}'.", blockIndex, _containerFilePath);

            byte[] buffer = new byte[_blockSize];
            _fileStream.Seek(blockIndex * _blockSize, SeekOrigin.Begin);
            await _fileStream.ReadAsync(buffer, 0, _blockSize);
            return buffer;
        }

        /// <summary>
        /// Writes a byte array to a block at the specified index.
        /// </summary>
        /// <param name="blockIndex">The index of the block to write to.</param>
        /// <param name="data">The data to write.</param>
        public void WriteBlock(long blockIndex, byte[] data)
        {
            if (data.Length != _blockSize)
                throw new ArgumentException($"Data must be exactly {_blockSize} bytes.", nameof(data));

            _logger.LogInformation("FileIOHelper: Writing to block {BlockIndex} in '{ContainerFilePath}'.", blockIndex, _containerFilePath);

            _fileStream.Seek(blockIndex * _blockSize, SeekOrigin.Begin);
            _fileStream.Write(data, 0, _blockSize);
            _fileStream.Flush();
        }

        /// <summary>
        /// Asynchronously writes a byte array to a block at the specified index.
        /// </summary>
        /// <param name="blockIndex">The index of the block to write to.</param>
        /// <param name="data">The data to write.</param>
        public async Task WriteBlockAsync(long blockIndex, byte[] data)
        {
            if (data.Length != _blockSize)
                throw new ArgumentException($"Data must be exactly {_blockSize} bytes.", nameof(data));

            _logger.LogInformation("FileIOHelper: Asynchronously writing to block {BlockIndex} in '{ContainerFilePath}'.", blockIndex, _containerFilePath);

            _fileStream.Seek(blockIndex * _blockSize, SeekOrigin.Begin);
            await _fileStream.WriteAsync(data, 0, _blockSize);
            await _fileStream.FlushAsync();
        }

        public void Dispose()
        {
            _fileStream?.Dispose();
        }

        public string GetContainerFilePath()
        {
            return _containerFilePath;
        }
    }
}
