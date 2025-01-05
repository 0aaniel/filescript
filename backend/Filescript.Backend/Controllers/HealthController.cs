using Filescript.Backend.Attributes;
using Filescript.Backend.Models;
using Filescript.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Filescript.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly ContainerManager _containerManager;
        private readonly ILoggerFactory _loggerFactory;

        public HealthController(
            ILogger<HealthController> logger,
            ContainerManager containerManager,
            ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _containerManager = containerManager;
            _loggerFactory = loggerFactory;
        }

        [NoContainerRequired]
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { status = "healthy", message = "Service is running" });
        }

        [HttpGet("check")]
        public async Task<IActionResult> CheckHealth()
        {
            try
            {
                // Validate container name
                string containerName = Request.Headers["X-Container-Name"].ToString();
                if (string.IsNullOrEmpty(containerName))
                {
                    return BadRequest(new { error = "Container name not specified in request headers" });
                }

                // Verify container exists
                if (!_containerManager.ListContainers().Contains(containerName))
                {
                    return NotFound(new { error = $"Container '{containerName}' not found" });
                }

                _logger.LogInformation("HealthController: Received health check request for container '{ContainerName}'.", containerName);

                var healthStatus = new Models.HealthStatus
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Checks = new List<HealthCheck>()
                };

                // Create service instances with the validated container name
                var fileService = _containerManager.GetFileService(containerName);
                var directoryService = _containerManager.GetDirectoryService(containerName);
                var deduplicationService = new DeduplicationService(
                    _loggerFactory.CreateLogger<DeduplicationService>(),
                    _containerManager,
                    containerName
                );
                var resiliencyService = new ResiliencyService(
                    _loggerFactory.CreateLogger<ResiliencyService>()
                );

                // Check FileService health
                try
                {
                    bool fileServiceStatus = await fileService.BasicHealthCheckAsync();
                    healthStatus.Checks.Add(new HealthCheck
                    {
                        Component = "FileService",
                        Status = fileServiceStatus ? "Healthy" : "Unhealthy"
                    });

                    if (!fileServiceStatus)
                    {
                        healthStatus.Status = "Unhealthy";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "HealthController: FileService health check failed.");
                    healthStatus.Checks.Add(new HealthCheck
                    {
                        Component = "FileService",
                        Status = "Unhealthy",
                        Error = ex.Message
                    });
                    healthStatus.Status = "Unhealthy";
                }

                // Check DirectoryService health
                try
                {
                    bool directoryServiceStatus = await directoryService.BasicHealthCheckAsync();
                    healthStatus.Checks.Add(new HealthCheck
                    {
                        Component = "DirectoryService",
                        Status = directoryServiceStatus ? "Healthy" : "Unhealthy"
                    });

                    if (!directoryServiceStatus)
                    {
                        healthStatus.Status = "Unhealthy";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "HealthController: DirectoryService health check failed.");
                    healthStatus.Checks.Add(new HealthCheck
                    {
                        Component = "DirectoryService",
                        Status = "Unhealthy",
                        Error = ex.Message
                    });
                    healthStatus.Status = "Unhealthy";
                }

                // Check DeduplicationService
                try
                {
                    bool deduplicationServiceStatus = deduplicationService.BasicHealthCheck();
                    healthStatus.Checks.Add(new HealthCheck
                    {
                        Component = "DeduplicationService",
                        Status = deduplicationServiceStatus ? "Healthy" : "Unhealthy"
                    });

                    if (!deduplicationServiceStatus)
                    {
                        healthStatus.Status = "Unhealthy";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "HealthController: DeduplicationService health check failed.");
                    healthStatus.Checks.Add(new HealthCheck
                    {
                        Component = "DeduplicationService",
                        Status = "Unhealthy",
                        Error = ex.Message
                    });
                    healthStatus.Status = "Unhealthy";
                }

                // Check ResiliencyService
                try
                {
                    bool resiliencyServiceStatus = resiliencyService.BasicHealthCheck();
                    healthStatus.Checks.Add(new HealthCheck
                    {
                        Component = "ResiliencyService",
                        Status = resiliencyServiceStatus ? "Healthy" : "Unhealthy"
                    });

                    if (!resiliencyServiceStatus)
                    {
                        healthStatus.Status = "Unhealthy";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "HealthController: ResiliencyService health check failed.");
                    healthStatus.Checks.Add(new HealthCheck
                    {
                        Component = "ResiliencyService",
                        Status = "Unhealthy",
                        Error = ex.Message
                    });
                    healthStatus.Status = "Unhealthy";
                }

                // Determine overall health status
                if (healthStatus.Status == "Healthy")
                {
                    _logger.LogInformation("HealthController: Health check passed for container '{ContainerName}'.", containerName);
                    return Ok(healthStatus);
                }
                else
                {
                    _logger.LogError("HealthController: Health check failed for container '{ContainerName}'.", containerName);
                    return StatusCode(500, healthStatus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HealthController: Unexpected error during health check.");
                return StatusCode(500, new { error = "An unexpected error occurred during health check", details = ex.Message });
            }
        }
    }
}