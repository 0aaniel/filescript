using Filescript.Backend.Services;
using Filescript.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Filescript.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly ContainerManager _containerManager;

        public FileController(ContainerManager containerManager)
        {
            _containerManager = containerManager;
        }

        [HttpPost("{containerName}/create")]
        public async Task<IActionResult> CreateFile(string containerName, [FromBody] CreateFileRequest request)
        {
            try
            {
                bool result = await _containerManager.CreateFileAsync(
                    containerName,
                    request.FileName,
                    request.Path,
                    request.Content
                );

                if (result)
                    return Ok(new { message = $"File '{request.FileName}' created successfully in container '{containerName}'." });

                return BadRequest(new { message = "Failed to create file." });
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, new { message = "An error occurred while creating the file." });
            }
        }

        // Define other file-related endpoints
    }

    public class CreateFileRequest
    {
        public string FileName { get; set; }
        public string Path { get; set; }
        public byte[] Content { get; set; }
    }
}
