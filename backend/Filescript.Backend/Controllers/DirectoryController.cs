using Filescript.Backend.Exceptions;
using Filescript.Backend.Services;
using Filescript.Models;
using Filescript.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Filescript.Backend.Controllers
{
    [ApiController]
    [Route("api/container/{containerName}/directory")]
    public class DirectoryController : ControllerBase
    {
        private readonly ContainerManager _containerManager;
        private readonly ILogger<DirectoryController> _logger;

        public DirectoryController(
            ContainerManager containerManager,
            ILogger<DirectoryController> logger)  // Add logger to constructor
        {
            _containerManager = containerManager ?? throw new ArgumentNullException(nameof(containerManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("md")]
        public async Task<IActionResult> MakeDirectory(string containerName, [FromBody] CreateDirectoryRequest request)
        {
            try
            {
                _logger.LogInformation("Creating directory '{DirectoryName}' at path '{Path}' in container '{ContainerName}'", 
                    request.DirectoryName, request.Path, containerName);

                bool result = await _containerManager.CreateDirectoryAsync(
                    containerName,
                    request.DirectoryName,
                    request.Path
                );

                if (result)
                {
                    _logger.LogInformation("Directory '{DirectoryName}' created successfully", request.DirectoryName);
                    return Ok(new { message = $"Directory '{request.DirectoryName}' created successfully at path '{request.Path}' in container '{containerName}'." });
                }

                _logger.LogWarning("Failed to create directory '{DirectoryName}'", request.DirectoryName);
                return BadRequest(new { message = "Failed to create directory." });
            }
            catch (Exceptions.DirectoryAlreadyExistsException ex)
            {
                _logger.LogWarning(ex, "Directory already exists: {DirectoryName}", request.DirectoryName);
                return Conflict(new { message = ex.Message });
            }
            catch (ContainerNotFoundException ex)
            {
                _logger.LogWarning(ex, "Container not found: {ContainerName}", containerName);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating directory '{DirectoryName}'", request.DirectoryName);
                return StatusCode(500, new { message = "An error occurred while creating the directory." });
            }
        }

        [HttpGet("ls")]
        public IActionResult ListDirectories(string containerName)
        {
            try
            {
                _logger.LogInformation("Listing directories and files for container '{ContainerName}'", containerName);
                var directories = _containerManager.ListDirectories(containerName);
                return Ok(new { directories });
            }
            catch (Exceptions.ContainerNotFoundException ex)
            {
                _logger.LogWarning(ex, "Container not found: {ContainerName}", containerName);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing directories and files for container '{ContainerName}'", containerName);
                return StatusCode(500, new { message = "An error occurred while listing directories and files." });
            }
        }

        [HttpPost("cd")]
        public async Task<IActionResult> ChangeDirectory(string containerName, [FromBody] ChangeDirectoryRequest request)
        {
            _logger.LogInformation("Changing directory to '{TargetDirectory}' in container '{ContainerName}'", 
                request.TargetDirectory, containerName);

            if (request == null)
            {
                _logger.LogWarning("Change directory request is null");
                return BadRequest(new { message = "Request payload is null." });
            }

            if (string.IsNullOrWhiteSpace(request.TargetDirectory))
            {
                _logger.LogWarning("Target directory is empty");
                return BadRequest(new { message = "Target directory cannot be empty." });
            }

            try
            {
                bool result = await _containerManager.ChangeDirectoryAsync(containerName, request.TargetDirectory);

                if (result)
                {
                    string currentDir = _containerManager.GetCurrentDirectory(containerName);
                    _logger.LogInformation("Changed directory to '{CurrentDirectory}'", currentDir);
                    return Ok(new { message = $"Changed directory to '{currentDir}'." });
                }

                _logger.LogWarning("Directory not found: {TargetDirectory}", request.TargetDirectory);
                return NotFound(new { message = $"Directory '{request.TargetDirectory}' not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing directory to '{TargetDirectory}'", request.TargetDirectory);
                return StatusCode(500, new { message = "An unexpected error occurred during the change directory operation." });
            }
        }

        [HttpPost("rd")]
        public async Task<IActionResult> RemoveDirectory(string containerName, [FromBody] RemoveDirectoryRequest request)
        {
            _logger.LogInformation("Removing directory '{DirectoryName}' from path '{Path}' in container '{ContainerName}'", 
                request.DirectoryName, request.Path, containerName);

            if (request == null)
            {
                _logger.LogWarning("Remove directory request is null");
                return BadRequest(new { message = "Request payload is null." });
            }

            if (string.IsNullOrWhiteSpace(request.DirectoryName))
            {
                _logger.LogWarning("Directory name is empty");
                return BadRequest(new { message = "DirectoryName cannot be empty." });
            }

            try
            {
                // Get current directory if path is not specified
                string targetPath = string.IsNullOrWhiteSpace(request.Path) 
                    ? _containerManager.GetCurrentDirectory(containerName) 
                    : request.Path;

                bool result = await _containerManager.RemoveDirectoryAsync(containerName, request.DirectoryName, targetPath);

                if (result)
                {
                    _logger.LogInformation("Directory '{DirectoryName}' removed successfully", request.DirectoryName);
                    return Ok(new { message = $"Directory '{request.DirectoryName}' removed successfully from path '{targetPath}'." });
                }

                _logger.LogWarning("Directory not found: {DirectoryName}", request.DirectoryName);
                return NotFound(new { message = $"Directory '{request.DirectoryName}' not found at path '{targetPath}'." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing directory '{DirectoryName}'", request.DirectoryName);
                return StatusCode(500, new { message = "An unexpected error occurred during the remove directory operation." });
            }
        }
    }

    public class CreateDirectoryRequest
    {
        public string DirectoryName { get; set; }
        public string Path { get; set; }
    }
}