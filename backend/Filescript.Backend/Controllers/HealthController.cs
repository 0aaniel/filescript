using Filescript.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Filescript.Backend.Models;

namespace Filescript.Backend.Controllers {

    /// <summary>
    /// Controller responsible for providing health check endpoints for the backend service.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase {

        private readonly ILogger<HealthController> _logger;
        private readonly IFileService _fileService;
        private readonly IDirectoryService _directoryService;
        private readonly IDeduplicationService _deduplicationService;
        private readonly IResiliencyService _resiliencyService;

         /// <summary>
        /// Initializes a new instance of the <see cref="HealthController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for logging.</param>
        /// <param name="fileService">Service handling file operations.</param>
        /// <param name="directoryService">Service handling directory operations.</param>
        /// <param name="deduplicationService">Service handling deduplication operations.</param>
        /// <param name="resiliencyService">Service handling resiliency checks.</param>
        public HealthController(
            ILogger<HealthController> logger,
            IFileService fileService,
            IDirectoryService directoryService,
            IDeduplicationService deduplicationService,
            IResiliencyService resiliencyService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
            _deduplicationService = deduplicationService ?? throw new ArgumentNullException(nameof(deduplicationService));
            _resiliencyService = resiliencyService ?? throw new ArgumentNullException(nameof(resiliencyService));
        }

        /// <summary>
        /// Performs a basic health check to verify if the backend service is running.
        /// Endpoint: GET /api/health/ping
        /// </summary>
        /// <returns>HTTP response indicating the service is healthy.</returns>
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            _logger.LogInformation("HealthController: Received ping request.");
            return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Performs a comprehensive health check, verifying the integrity and responsiveness of various services.
        /// Endpoint: GET /api/health/check
        /// </summary>
        /// <returns>HTTP response indicating the overall health of the service.</returns>
        [HttpGet("check")]
        public async Task<IActionResult> CheckHealth() {
            _logger.LogInformation("HealthController: Received health check request.");

            var healthStatus = new Models.HealthStatus {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Checks = new List<HealthCheck>()
            };

            // Check FileService health
            try {
                bool fileServiceStatus = await _fileService.BasicHealthCheckAsync();
                healthStatus.Checks.Add(new HealthCheck {
                    Component = "FileService",
                    Status = fileServiceStatus ? "Healthy" : "Unhealthy"
                });

                if (!fileServiceStatus) {
                    healthStatus.Status = "Unhealthy";
                }
            } catch (Exception ex) {
                _logger.LogError(ex, "HealthController: FileService health check failed.");
                healthStatus.Checks.Add(new HealthCheck {
                    Component = "FileService",
                    Status = "Unhealthy",
                    Error = ex.Message
                });
                healthStatus.Status = "Unhealthy";
            }

            // Check DirectoryService health
            try {
                bool directoryServiceStatus = await _directoryService.BasicHealthCheckAsync();
                healthStatus.Checks.Add(new HealthCheck {
                    Component = "DirectoryService",
                    Status = directoryServiceStatus ? "Healthy" : "Unhealthy"
                });

                if (!directoryServiceStatus) {
                    healthStatus.Status = "Unhealthy";
                }

            } catch (Exception ex) {

                _logger.LogError(ex, "HealthController: DirectoryService health check failed.");
                healthStatus.Checks.Add(new HealthCheck {
                    Component = "DirectoryService",
                    Status = "Unhealthy",
                    Error = ex.Message
                });
                healthStatus.Status = "Unhealthy";
            }

            // Check DeduplicationService
            try
            {
                bool deduplicationServiceStatus = _deduplicationService.BasicHealthCheck();
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
                bool resiliencyServiceStatus = _resiliencyService.BasicHealthCheck();
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
            if (healthStatus.Status == "Healthy") {
                _logger.LogInformation("HealthController: Health check passed.");
                return Ok(healthStatus);
            } else {
                _logger.LogError("HealthController: Health check failed.");
                return StatusCode(500, healthStatus);
            }
        }
    }
}