using Filescript.Backend.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Filescript.HealthChecks
{
    /// <summary>
    /// Health check for the DirectoryService.
    /// </summary>
    public class DirectoryServiceHealthCheck : IHealthCheck
    {
        private readonly IDirectoryService _directoryService;
        private readonly ILogger<DirectoryServiceHealthCheck> _logger;

        public DirectoryServiceHealthCheck(IDirectoryService directoryService, ILogger<DirectoryServiceHealthCheck> logger)
        {
            _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("DirectoryServiceHealthCheck: Starting health check.");

            try
            {
                bool isHealthy = await _directoryService.BasicHealthCheckAsync();
                if (isHealthy)
                {
                    _logger.LogInformation("DirectoryServiceHealthCheck: Healthy.");
                    return HealthCheckResult.Healthy("DirectoryService is operational.");
                }
                else
                {
                    _logger.LogWarning("DirectoryServiceHealthCheck: Unhealthy.");
                    return HealthCheckResult.Unhealthy("DirectoryService is unhealthy.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DirectoryServiceHealthCheck: Exception during health check.");
                return HealthCheckResult.Unhealthy("DirectoryService encountered an exception.", ex);
            }
        }
    }
}
