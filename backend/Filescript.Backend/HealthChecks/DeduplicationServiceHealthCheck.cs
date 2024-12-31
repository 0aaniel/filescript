using Filescript.Backend.Services;
using Filescript.Backend.Services.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Filescript.HealthChecks
{
    /// <summary>
    /// Health check for the DeduplicationService.
    /// </summary>
    public class DeduplicationServiceHealthCheck : IHealthCheck
    {
        private readonly IDeduplicationService _deduplicationService;
        private readonly ILogger<DeduplicationServiceHealthCheck> _logger;

        public DeduplicationServiceHealthCheck(IDeduplicationService deduplicationService, ILogger<DeduplicationServiceHealthCheck> logger)
        {
            _deduplicationService = deduplicationService ?? throw new ArgumentNullException(nameof(deduplicationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("DeduplicationServiceHealthCheck: Starting health check.");

            try
            {
                bool isHealthy = _deduplicationService.BasicHealthCheck();
                if (isHealthy)
                {
                    _logger.LogInformation("DeduplicationServiceHealthCheck: Healthy.");
                    return Task.FromResult(HealthCheckResult.Healthy("DeduplicationService is operational."));
                }
                else
                {
                    _logger.LogWarning("DeduplicationServiceHealthCheck: Unhealthy.");
                    return Task.FromResult(HealthCheckResult.Unhealthy("DeduplicationService is unhealthy."));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeduplicationServiceHealthCheck: Exception during health check.");
                return Task.FromResult(HealthCheckResult.Unhealthy("DeduplicationService encountered an exception.", ex));
            }
        }
    }
}
