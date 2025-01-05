using Filescript.Backend.Exceptions;
using Filescript.Backend.Services;
using Filescript.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Filescript.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContainerController : ControllerBase
    {
        private readonly ContainerManager _containerManager;

        public ContainerController(ContainerManager containerManager)
        {
            _containerManager = containerManager;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateContainer([FromBody] CreateContainerRequest request)
        {
            try
            {
                bool result = await _containerManager.CreateContainerAsync(
                    request.ContainerName,
                    request.ContainerFilePath,
                    (int)request.TotalBlocks,
                    request.BlockSize
                );

                if (result)
                    return Ok(new { message = $"Container '{request.ContainerName}' created successfully." });

                return BadRequest(new { message = "Failed to create container." });
            }
            catch (ContainerAlreadyExistsException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, new { message = "An error occurred while creating the container." });
            }
        }

        // Define other endpoints (e.g., delete, list, etc.)
    }

    public class CreateContainerRequest
    {
        public string ContainerName { get; set; }
        public string ContainerFilePath { get; set; }
        public long TotalBlocks { get; set; }
        public int BlockSize { get; set; }
    }
}
