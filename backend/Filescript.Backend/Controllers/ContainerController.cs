using Filescript.Backend.Exceptions;
using Filescript.Backend.Services;
using Filescript.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Filescript.Backend.Controllers
{
    [ApiController]
    [Route("api/container")]
    public class ContainerController : ControllerBase
    {
        private readonly ContainerManager _containerManager;
        private readonly ILogger<ContainerController> _logger;

        public ContainerController(ContainerManager containerManager, ILogger<ContainerController> logger)
        {
            _containerManager = containerManager;
            _logger = logger;
        }
        
        [HttpPost("create")]
        public async Task<IActionResult> CreateContainer([FromBody] CreateContainerRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid CreateContainerRequest: {ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Attempting to create container '{ContainerName}' at '{ContainerFilePath}'.", 
                    request.ContainerName, request.ContainerFilePath);

                bool result = await _containerManager.CreateContainerAsync(
                    request.ContainerName,
                    request.ContainerFilePath,
                    request.TotalBlocks,
                    request.BlockSize
                );

                if (result)
                {
                    _logger.LogInformation("Container '{ContainerName}' created successfully.", request.ContainerName);
                    return Ok(new { message = $"Container '{request.ContainerName}' created successfully." });
                }

                _logger.LogWarning("Failed to create container '{ContainerName}'.", request.ContainerName);
                return BadRequest(new { message = "Failed to create container." });
            }
            catch (ContainerAlreadyExistsException ex)
            {
                _logger.LogError(ex, "Container '{ContainerName}' already exists.", request.ContainerName);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating container '{ContainerName}'.", request.ContainerName);
                return StatusCode(500, new { message = "An error occurred while creating the container. " + ex.Message });
            }
        }

        [HttpDelete("delete/{containerName}")]
        public IActionResult DeleteContainer(string containerName)
        {
            try
            {
                bool result = _containerManager.DeleteContainer(containerName);

                if (result)
                    return Ok(new { message = $"Container '{containerName}' deleted successfully." });

                return BadRequest(new { message = "Failed to delete container." });
            }
            catch (ContainerNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception if logging is set up
                return StatusCode(500, new { message = "An error occurred while deleting the container." });
            }
        }

        [HttpGet("list")]
        public IActionResult ListContainers()
        {
            var containers = _containerManager.ListContainers();
            return Ok(new { containers });
        }

        // Define other container-related endpoints as needed
    }

    public class CreateContainerRequest
    {
        public string ContainerName { get; set; }
        public string ContainerFilePath { get; set; }
        public int TotalBlocks { get; set; }
        public int BlockSize { get; set; }
    }
}
