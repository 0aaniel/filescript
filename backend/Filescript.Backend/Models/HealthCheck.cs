namespace Filescript.Backend.Models
{
    /// <summary>
    /// Represents the health status of an individual component.
    /// </summary>
    public class HealthCheck
    {
        /// <summary>
        /// Name of the component being checked.
        /// </summary>
        public required string Component { get; set; }

        /// <summary>
        /// Status of the component (e.g., Healthy, Unhealthy).
        /// </summary>
        public required string Status { get; set; }

        /// <summary>
        /// Error message if the component is unhealthy.
        /// </summary>
        public string? Error { get; set; }
    }
}