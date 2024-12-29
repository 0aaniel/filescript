using Filescript.Backend.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Filescript.HealthChecks
{
    /// <summary>
    /// Health check for the ResiliencyService.
    /// </summary>
    public class ResiliencyServiceHealthCheck : IHealthCheck
    {
        private readonly IResiliencyService _resiliencyService;
        private readonly ILogger<ResiliencyServiceHealthCheck> _logger;

        public ResiliencyServiceHealthCheck(IResiliencyService resiliencyService, ILogger<ResiliencyServiceHealthCheck> logger)
        {
            _resiliencyService = resiliencyService ?? throw new ArgumentNullException(nameof(resiliencyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("ResiliencyServiceHealthCheck: Starting health check.");

            try
            {
                bool isHealthy = _resiliencyService.BasicHealthCheck();
                if (isHealthy)
                {
                    _logger.LogInformation("ResiliencyServiceHealthCheck: Healthy.");
                    return Task.FromResult(HealthCheckResult.Healthy("ResiliencyService is operational."));
                }
                else
                {
                    _logger.LogWarning("ResiliencyServiceHealthCheck: Unhealthy.");
                    return Task.FromResult(HealthCheckResult.Unhealthy("ResiliencyService is unhealthy."));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResiliencyServiceHealthCheck: Exception during health check.");
                return Task.FromResult(HealthCheckResult.Unhealthy("ResiliencyService encountered an exception.", ex));
            }
        }
    }
}
