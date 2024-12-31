using Filescript.Backend.Exceptions;
using Filescript.Backend.Services;
using Filescript.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Filescript.Backend.Controllers
{
    [ApiController]
    [Route("api/container/{containerName}/directory")]
    public class DirectoryController : ControllerBase
    {
        private readonly ContainerManager _containerManager;

        public DirectoryController(ContainerManager containerManager)
        {
            _containerManager = containerManager;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateDirectory(string containerName, [FromBody] CreateDirectoryRequest request)
        {
            try
            {
                bool result = await _containerManager.CreateDirectoryAsync(
                    containerName,
                    request.DirectoryName,
                    request.Path
                );

                if (result)
                    return Ok(new { message = $"Directory '{request.DirectoryName}' created successfully at path '{request.Path}' in container '{containerName}'." });

                return BadRequest(new { message = "Failed to create directory." });
            }
            catch (Exceptions.DirectoryAlreadyExistsException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ContainerNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception if logging is set up
                return StatusCode(500, new { message = "An error occurred while creating the directory." });
            }
        }

        [HttpGet("list")]
        public IActionResult ListDirectories(string containerName)
        {
            try
            {
                var directories = _containerManager.ListDirectories(containerName);
                return Ok(new { directories });
            }
            catch (Exceptions.ContainerNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception if logging is set up
                return StatusCode(500, new { message = "An error occurred while listing directories." });
            }
        }

        // Define other directory-related endpoints as needed
    }

    public class CreateDirectoryRequest
    {
        public string DirectoryName { get; set; }
        public string Path { get; set; }
    }
}
