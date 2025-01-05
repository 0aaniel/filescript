
using System.Text.Json;

namespace Filescript.Backend.Middleware {
    public class GlobalExceptionHandlerMiddleware {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger) {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context) {
            try {
                await _next(context);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "An unhandled exception occurred.");
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                var response = new { message = "An unexpected error occurred." };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        }
    }
}