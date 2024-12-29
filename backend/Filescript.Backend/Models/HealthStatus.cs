namespace Filescript.Backend.Models {

    /// <summary>
    /// Represents the overall health status of the service.
    /// </summary>
    public class HealthStatus {
        /// <summary>
        /// Overall status of the service (e.g., Healthy, Unhealthy).
        /// </summary>
        public required string Status { get; set; }

        /// <summary>
        /// Timestamp of the health check.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// List of individual component health checks.
        /// </summary>
        public List<HealthCheck>? Checks { get; set; }
    }
}