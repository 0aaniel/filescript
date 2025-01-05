using Filescript.Backend.Exceptions;
using Filescript.Backend.Services;
using Filescript.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Filescript.Backend.Controllers
{
    [ApiController]
    [Route("api/container/{containerName}/file")]
    public class FileController : ControllerBase
    {
        private readonly ContainerManager _containerManager;

        public FileController(ContainerManager containerManager)
        {
            _containerManager = containerManager;
        }

        [HttpPost("create")]
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
            catch (FileAlreadyExistsException ex)
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
                return StatusCode(500, new { message = "An error occurred while creating the file: " + ex.Message });
            }
        }

        [HttpGet("read")]
        public async Task<IActionResult> ReadFile(string containerName, [FromQuery] ReadFileRequest request)
        {
            try
            {
                byte[] content = await _containerManager.GetFileService(containerName).ReadFileAsync(
                    request.FileName,
                    request.Path
                );

                if (content == null || content.Length == 0)
                    return NotFound(new { message = "File content is empty or file does not exist." });

                return File(content, "application/octet-stream", request.FileName);
            }
            catch (Exceptions.FileNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ContainerNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception if logging is set up
                return StatusCode(500, new { message = "An error occurred while reading the file." });
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteFile(string containerName, [FromBody] DeleteFileRequest request)
        {
            try
            {
                bool result = await _containerManager.GetFileService(containerName).DeleteFileAsync(
                    request.FileName,
                    request.Path
                );

                if (result)
                    return Ok(new { message = $"File '{request.FileName}' deleted successfully from container '{containerName}'." });

                return BadRequest(new { message = "Failed to delete file." });
            }
            catch (Exceptions.FileNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ContainerNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception if logging is set up
                return StatusCode(500, new { message = "An error occurred while deleting the file." });
            }
        }

        // Define other file-related endpoints as needed
    }

    public class CreateFileRequest
    {
        public string FileName { get; set; }
        public string Path { get; set; }
        public byte[] Content { get; set; }
    }

    public class ReadFileRequest
    {
        public string FileName { get; set; }
        public string Path { get; set; }
    }

    public class DeleteFileRequest
    {
        public string FileName { get; set; }
        public string Path { get; set; }
    }
}
