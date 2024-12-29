using Filescript.Backend.Models;
using Filescript.Backend.Models.RequestModels;
using Filescript.Backend.Services;
using Filescript.Exceptions;
using Filescript.Models.RequestModels;
using Filescript.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Filescript.Contrllers {
    
    /// <summary>
    /// Controller responsible for handling directory-related operations within the container.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DirectoryController : ControllerBase {

        private readonly IDirectoryService _directoryService;
        private readonly ILogger<DirectoryController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryController"/> class.
        /// </summary>
        /// <param name="directoryService">Service handling directory operations.</param>
        /// <param name="logger">Logger instance for logging.</param>
        public DirectoryController(IDirectoryService directoryService, ILogger<DirectoryController> logger) {
            _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new directory within the container.
        /// Endpoint: POST /api/directory/md
        /// </summary>
        /// <param name="request">Request payload containing the name of the directory to create.</param>
        /// <returns>HTTP response indicating success or failure.</returns>
        [HttpPost("md")]
        public async Task<IActionResult> MakeDirectory([FromBody] MakeDirectoryRequest request) {
            _logger.LogInformation("Received md request: DirectoryName={DirectoryName}", request.DirectoryName);

            // Input validation
            if (request == null) {
                _logger.LogWarning("md request is null.");
                return BadRequest(new { message = "Request payload is null." });
            }

            if (string.IsNullOrWhiteSpace(request.DirectoryName)) {
                _logger.LogWarning("md request has empty DirectoryName.");
                return BadRequest(new { message = "Directory name cannot be empty." });
            }

            try {
                bool result = await _directoryService.MakeDirectoryAsync(request.DirectoryName);

                if (result) {
                    _logger.LogInformation("md operation successful: {DirectoryName}", request.DirectoryName);
                    return Ok(new { message = $"Directory '{request.DirectoryName}' created successfully." });
                } else {
                    _logger.LogError("md operation failed for DirectoryName={DirectoryName}", request.DirectoryName);
                    return StatusCode(500, new { message = "Failed to create the directory." });
                }
          
            } catch (DirectoryAlreadyExistsException ex) {
                _logger.LogError(ex, "md operation failed: Directory already exists.");
                return Conflict(new { message = "Directory already exists." });
          
            } catch (UnauthorizedAccessException ex) {
                _logger.LogError(ex, "md operation failed: Unauthorized access.");
                return StatusCode(403, new { message = "Unauthorized access to create the directory." });
          
            } catch (ArgumentException ex) {
                _logger.LogError(ex, "md operation failed: Invalid directory name.");
                return BadRequest(new { message = ex.Message });

            } catch (Exception ex) {
                _logger.LogError(ex, "md operation encountered an unexpected error.");
                return StatusCode(500, new { message = "An unexpected error occurred during the make directory operation." });
            }
        }

        /// <summary>
        /// Changes the current directory within the container.
        /// Endpoint: POST /api/directory/cd
        /// </summary>
        /// <param name="request">Request payload containing the target directory path.</param>
        /// <returns>HTTP response indicating success or failure.</returns>
        [HttpPost("cd")]
        public async Task<IActionResult> ChangeDirectory([FromBody] ChangeDirectoryRequest request) {
            _logger.LogInformation("Received cd request: TargetDirectory={TargetDirectory}", request.TargetDirectory);

            // Input validation
            if (request == null) {
                _logger.LogWarning("cd request is null.");
                return BadRequest(new { message = "Request payload is null." });
            }

            if (string.IsNullOrWhiteSpace(request.TargetDirectory)) {
                _logger.LogWarning("cd request has empty TargetDirectory.");
                return BadRequest(new { message = "Target directory cannot be empty." });
            }

            try {
                bool result = await _directoryService.ChangeDirectoryAsync(request.TargetDirectory);

                if (result) {
                    _logger.LogInformation("cd operation successful: {TargetDirectory}", request.TargetDirectory);
                    return Ok(new { message = $"Changed directory to '{request.TargetDirectory}'." });
                } else {
                    _logger.LogError("cd operation failed for TargetDirectory={TargetDirectory}", request.TargetDirectory);
                    return NotFound(new { message = $"Directory '{request.TargetDirectory}' not found." });
                }

            } catch (DirectoryNotFoundException ex) {
                _logger.LogError(ex, "cd operation failed: Directory not found.");
                return NotFound(new { message = "Target directory not found." });

            } catch (UnauthorizedAccessException ex) {
                _logger.LogError(ex, "cd operation failed: Unauthorized access.");
                return StatusCode(403, new { message = "Unauthorized access to the target directory." });

            } catch (ArgumentException ex) {
                _logger.LogError(ex, "cd operation failed: Invalid arguments.");
                return BadRequest(new { message = ex.Message });

            } catch (Exception ex) {
                _logger.LogError(ex, "cd operation encountered an unexpected error.");
                return StatusCode(500, new { message = "An unexpected error occurred during the change directory operation." });
            }
        }

        /// <summary>
        /// Removes a directory from the container.
        /// Endpoint: POST /api/directory/rd
        /// </summary>
        /// <param name="request">Request payload containing the name of the directory to remove.</param>
        /// <returns>HTTP response indicating success or failure.</returns>
        [HttpPost("rd")]
        public async Task<IActionResult> RemoveDirectory([FromBody] RemoveDirectoryRequest request) {
            _logger.LogInformation("Received rd request: DirectoryName={DirectoryName}", request.DirectoryName);

            // Input Validation
            if (request == null) {
                _logger.LogWarning("rd request is null.");
                return BadRequest(new { message = "Request payload is null." });
            }

            if (string.IsNullOrWhiteSpace(request.DirectoryName)) {
                _logger.LogWarning("rd request has empty DirectoryName.");
                return BadRequest(new { message = "DirectoryName cannot be empty." });
            }

            try {
                bool result = await _directoryService.RemoveDirectoryAsync(request.DirectoryName);

                if (result) {
                    _logger.LogInformation("rd operation successful: {DirectoryName}", request.DirectoryName);
                    return Ok(new { message = $"Directory '{request.DirectoryName}' removed successfully." });
                } else {
                    _logger.LogError("rd operation failed for DirectoryName={DirectoryName}", request.DirectoryName);
                    return NotFound(new { message = $"Directory '{request.DirectoryName}' not found." });
                }
            }
            catch (DirectoryNotEmptyException ex) {
                _logger.LogError(ex, "rd operation failed: Directory not empty.");
                return BadRequest(new { message = "Cannot remove a non-empty directory." });
            
            } catch (UnauthorizedAccessException ex) {
                _logger.LogError(ex, "rd operation failed: Unauthorized access.");
                return StatusCode(403, new { message = "Unauthorized access to remove the directory." });
            
            } catch (ArgumentException ex) {
                _logger.LogError(ex, "rd operation failed: Invalid arguments.");
                return BadRequest(new { message = ex.Message });
           
            } catch (Exception ex) {
                _logger.LogError(ex, "rd operation encountered an unexpected error.");
                return StatusCode(500, new { message = "An unexpected error occurred during the remove directory operation." });
            }
        }

        /// <summary>
        /// Lists all directories in the current directory of the container.
        /// Endpoint: GET /api/directory/ls
        /// </summary>
        /// <returns>HTTP response containing a list of directories with their names.</returns>
        [HttpGet("ls")]
        public IActionResult ListDirectories()
        {
            _logger.LogInformation("Received ls request for directories.");

            try
            {
                var directories = _directoryService.ListDirectories();

                if (directories == null || directories.Count == 0)
                {
                    _logger.LogInformation("ls operation: No directories found in the current directory.");
                    return Ok(new { message = "No directories found in the current directory.", directories = new object[] { } });
                }

                // Prepare response
                var response = new object[directories.Count];
                for (int i = 0; i < directories.Count; i++)
                {
                    response[i] = new
                    {
                        Name = directories[i].Name
                    };
                }

                _logger.LogInformation("ls operation successful: {DirectoryCount} directories listed.", directories.Count);
                return Ok(new { directories = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ls operation encountered an unexpected error.");
                return StatusCode(500, new { message = "An unexpected error occurred during the list directories operation." });
            }
        }
    }
}