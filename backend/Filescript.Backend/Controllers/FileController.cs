using Filescript.Backend.Exceptions;
using Filescript.Backend.Models.RequestModels;
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

        [HttpPost("cpin")]
        public async Task<IActionResult> CopyFileIn(
            string containerName, 
            [FromBody] CopyInRequest request)
        {
            if (!System.IO.File.Exists(request.ExternalFilePath))
            {
                return BadRequest(new { message = $"External file '{request.ExternalFilePath}' does not exist." });
            }

            try
            {
                // 1. Read the external file in chunks
                // 2. Create a file inside the container with the chunked content
                const int BUFFER_SIZE = 64 * 1024; // 64 KB chunks, for example
                byte[] buffer = new byte[BUFFER_SIZE];

                using (FileStream fs = new FileStream(request.ExternalFilePath, FileMode.Open, FileAccess.Read))
                {
                    // Instead of reading the whole file in memory, read in chunks
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int bytesRead;
                        while ((bytesRead = await fs.ReadAsync(buffer, 0, BUFFER_SIZE)) > 0)
                        {
                            // Write those bytes to an intermediate MemoryStream
                            // so we can pass them all at once to CreateFileAsync at the end
                            // *** If you REALLY want to avoid building the entire file in memory at once,
                            // you would need a more advanced approach that writes chunk-by-chunk
                            // to the container. The example below is the simpler approach. ***
                            await ms.WriteAsync(buffer, 0, bytesRead);
                        }

                        // We now have the entire file in `ms` as a byte array
                        byte[] content = ms.ToArray();

                        // 3. Call your existing CreateFileAsync to store the file inside the container
                        bool result = await _containerManager.CreateFileAsync(
                            containerName,
                            request.ContainerFileName,
                            request.ContainerPath,
                            content
                        );

                        if (result)
                        {
                            return Ok(new 
                            { 
                                message = $"File '{request.ExternalFilePath}' copied to container '{containerName}' as '{request.ContainerFileName}'." 
                            });
                        }
                        else
                        {
                            return BadRequest(new { message = "Failed to copy file to container." });
                        }
                    }
                }
            }
            catch (ContainerNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while copying file into container: " + ex.Message });
            }
        }

        [HttpPost("cpout")]
        public async Task<IActionResult> CopyFileOut(
            string containerName,
            [FromBody] CopyOutRequest request)
        {
            try
            {
                // 1. Read the file’s content inside the container
                byte[] content = await _containerManager
                    .GetFileService(containerName)
                    .ReadFileAsync(request.ContainerFileName, request.ContainerPath);

                // 2. Write that content to an external file in chunks
                const int BUFFER_SIZE = 64 * 1024; // 64 KB chunk
                Directory.CreateDirectory(Path.GetDirectoryName(request.ExternalFilePath));

                using (FileStream fs = new FileStream(request.ExternalFilePath, FileMode.Create, FileAccess.Write))
                {
                    // Write in smaller chunks to the external file
                    int totalBytesWritten = 0;
                    while (totalBytesWritten < content.Length)
                    {
                        int bytesToWrite = Math.Min(BUFFER_SIZE, content.Length - totalBytesWritten);
                        await fs.WriteAsync(content, totalBytesWritten, bytesToWrite);
                        totalBytesWritten += bytesToWrite;
                    }
                }

                return Ok(new
                {
                    message = $"File '{request.ContainerFileName}' from container '{containerName}' copied out to '{request.ExternalFilePath}'."
                });
            }
            catch (Filescript.Backend.Exceptions.FileNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ContainerNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while copying file out of container: " + ex.Message });
            }
        }
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
