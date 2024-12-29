using Filescript.Backend.Models;
using Filescript.Backend.Models.RequestModels;
using Filescript.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Filescript.Backend.Controllers {

    /// <summary>
    /// Controller responsible for handling file-related operations within the container.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase {
        private readonly IFileService _fileService;
        private readonly ILogger<FileController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileController"/> class.
        /// </summary>
        /// <param name="fileService">Service handling file operations..</param>
        /// <param name="logger">Logger instance for logging.</param>
        public FileController(IFileService fileService, ILogger<FileController> logger) {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Copies a file from the external file system into the container.
        /// Endpoint: POST /api/file/cpin
        /// </summary>
        /// <param name="request">Request payload containing source path and destination name.</param>
        /// <returns>HTTP response indicating success or failure.</returns>
        [HttpPost("cpin")]
        public async Task<IActionResult> CopyIn([FromBody] CpinRequest request) {
            _logger.LogInformation("Received cpin request: SourcePath={SourcePath}, DestName={DestName}", request.SourcePath, request.DestName);

            // Input validation
            if (request == null) {
                _logger.LogWarning("Received null cpin request.");
                return BadRequest(new { message = "Request cannot be null." });
            }

            if (string.IsNullOrWhiteSpace(request.SourcePath)) {
                _logger.LogWarning("Received cpin request with empty source path.");
                return BadRequest(new { message = "Source path cannot be empty." });
            }

            if (string.IsNullOrWhiteSpace(request.DestName)) {
                _logger.LogWarning("Received cpin request with empty destination name.");
                return BadRequest(new { message = "Destination name cannot be empty." });
            }

            try {
                bool result = await _fileService.CopyInAsync(request.SourcePath, request.DestName);

                if (result) {
                    _logger.LogInformation("Successfully copied file from {SourcePath} to {DestName}.", request.SourcePath, request.DestName);
                    return Ok();
                } else {
                    _logger.LogWarning("Failed to copy file from {SourcePath} to {DestName}.", request.SourcePath, request.DestName);
                    return StatusCode(500, new { message = "Failed to copy file." });
                }

            } catch (FileNotFoundException ex) {
                _logger.LogError(ex, "cpin operation failed: Source file not found.");
                return NotFound(new { message = "Source file not found." });
            } catch (UnauthorizedAccessException ex) {
                _logger.LogError(ex, "cpin operation failed: Unauthorized access.");
                return StatusCode(403, new { message = "Unauthorized access to the source file." });
            } catch (ArgumentException ex) {
                _logger.LogError(ex, "cpin operation failed: Invalid arguments.");
                return BadRequest(new { message = ex.Message });
            } catch (Exception ex) {
                _logger.LogError(ex, "cpin operation encountered an unexpected error.");
                return StatusCode(500, new { message = "UAn unexpected error occurred during the copy-in operation." });
            }
        }

        /// <summary>
        /// Copies a file from the container to the external file system.
        /// Endpoint: POST /api/file/cpout
        /// </summary>
        /// <param name="request">Request payload containing source name and destination path.</param>
        /// <returns>HTTP response indicating success or failure.</returns>
        [HttpPost("cpout")]
        public async Task<IActionResult> CopyOut([FromBody] CpoutRequest request) {
            _logger.LogInformation("Received cpout request: SourceName={SourceName}, DestPath={DestPath}", request.SourceName, request.DestPath);

            // Input validation
            if (request == null) {
                _logger.LogWarning("Received null cpout request.");
                return BadRequest(new { message = "Request payload is null." });
            }

            if (string.IsNullOrWhiteSpace(request.SourceName)) {
                _logger.LogWarning("Received cpout request with empty source name.");
                return BadRequest(new { message = "Source name cannot be empty." });
            }

            if (string.IsNullOrWhiteSpace(request.DestPath)) {
                _logger.LogWarning("Received cpout request with empty destination path.");
                return BadRequest(new { message = "Destination path cannot be empty." });
            }

            try {
                bool result = await _fileService.CopyOutAsync(request.SourceName, request.DestPath);

                if (result) {
                    _logger.LogInformation("cpout operation successful: {SourceName} to {DestPath}.", request.SourceName, request.DestPath);
                    return Ok(new { message = $"File '{request.SourceName}' copied out to '{request.DestPath}' successfully."});
                } else {
                    _logger.LogError("cpout operation failed for SourceName={SourceName}", request.SourceName);
                    return StatusCode(500, new { message = "Failed to copy file out of the container." });
                }
            } catch (FileNotFoundException ex) {
                _logger.LogError(ex, "cpout operation failed: File not found in container.");
                return NotFound(new { message = "File not found in the container." });
            } catch (UnauthorizedAccessException ex) {
                _logger.LogError(ex, "cpout operation failed: Unauthorized access.");
                return StatusCode(403, new { message = "Unauthorized access to write to the destination path." });
            } catch (ArgumentException ex) {
                _logger.LogError(ex, "cpout operation failed: Invalid arguments.");
                return BadRequest(new { message = ex.Message });
            } catch (Exception ex) {
                _logger.LogError(ex, "cpout operation encountered an unexpected error.");
                return StatusCode(500, new { message = "An unexpected error occurred during the copy-out operation." });
            }
        }

        /// <summary>
        /// Removes a file from the container.
        /// Endpoint: POST /api/file/rm
        /// </summary>
        /// <param name="request">Request payload containing the name of the file to remove.</param>
        /// <returns>HTTP response indicating success or failure.</returns>
        [HttpPost("rm")]
        public async Task<IActionResult> RemoveFile([FromBody] RemoveFileRequest request) {
            _logger.LogInformation("Received rm request: FileName={FileName}", request.FileName);

            // Input validation
            if (request == null) {
                _logger.LogWarning("Received null rm request.");
                return BadRequest(new { message = "Request payload is null." });
            }

            if (string.IsNullOrWhiteSpace(request.FileName)) {
                _logger.LogWarning("Received rm request with empty file name.");
                return BadRequest(new { message = "File name cannot be empty." });
            }

            try {
                bool result = await _fileService.RemoveFileAsync(request.FileName);

                if (result) {
                    _logger.LogInformation("rm operation successful: {FileName}.", request.FileName);
                    return Ok(new { message = $"File '{request.FileName}' removed from the container successfully." });
                } else {
                    _logger.LogError("rm operation failed for FileName={FileName}", request.FileName);
                    return NotFound(new { message = $"File '{request.FileName}' not found in the container." });
                }

            } catch (Exception ex) {
                _logger.LogError(ex, "rm operation encountered an unexpected error.");
                return StatusCode(500, new { message = "An unexpected error occurred during the remove file operation." });
            }
        }

        /// <summary>
        /// Lists all files in the current directory of the container.
        /// Endpoint: GET /api/file/ls
        /// </summary>
        /// <returns>HTTP response containing a list of files with their names and sizes.</returns>
        [HttpGet("ls")]
        public IActionResult ListFiles() {
            _logger.LogInformation("Received ls request.");

            try {
                var files = _fileService.ListFiles();

                if (files == null || files.Count == 0) {
                    _logger.LogInformation("ls operation: No files found in the current directory.");
                    return Ok(new { message = "No files found in the current directory.", files = new object[] { } });
                }

                // Prepare response
                var response = new object[files.Count];
                for (int i = 0; i < files.Count; i++) {
                    response[i] = new {
                        Name = files[i].Name,
                        Size = $"{files[i].Size} bytes"
                    };
                }

                _logger.LogInformation("ls operation successful: {FileCount} files listed.", files.Count);
                return Ok(new { files = response });

            } catch (Exception ex) {
                _logger.LogError(ex, "ls operation encountered an unexpected error.");
                return StatusCode(500, new { message = "An unexpected error occurred during the list files operation." });
            }
        }
    }
}