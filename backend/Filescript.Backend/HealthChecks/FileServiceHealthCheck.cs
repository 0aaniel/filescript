using Filescript.Backend.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Filescript.HealthChecks
{
    /// <summary>
    /// Health check for the FileService.
    /// </summary>
    public class FileServiceHealthCheck : IHealthCheck
    {
        private readonly IFileService _fileService;
        private readonly ILogger<FileServiceHealthCheck> _logger;

        public FileServiceHealthCheck(IFileService fileService, ILogger<FileServiceHealthCheck> logger)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("FileServiceHealthCheck: Starting health check.");

            try
            {
                bool isHealthy = await _fileService.BasicHealthCheckAsync();
                if (isHealthy)
                {
                    _logger.LogInformation("FileServiceHealthCheck: Healthy.");
                    return HealthCheckResult.Healthy("FileService is operational.");
                }
                else
                {
                    _logger.LogWarning("FileServiceHealthCheck: Unhealthy.");
                    return HealthCheckResult.Unhealthy("FileService is unhealthy.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FileServiceHealthCheck: Exception during health check.");
                return HealthCheckResult.Unhealthy("FileService encountered an exception.", ex);
            }
        }
    }
}
