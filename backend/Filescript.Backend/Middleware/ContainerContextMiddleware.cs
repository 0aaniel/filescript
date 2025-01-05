using Filescript.Backend.Services;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Filescript.Backend.Middleware
{
    public class ContainerContextMiddleware
    {
        private readonly RequestDelegate _next;

        public ContainerContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ContainerContext containerContext)
        {
            // Get container name from header, route, or query parameter
            // This is an example - adjust based on your API design
            string containerName = context.Request.Headers["X-Container-Name"].ToString();
            
            // Or from route data
            // var containerName = context.Request.RouteValues["containerName"]?.ToString();

            if (!string.IsNullOrEmpty(containerName))
            {
                containerContext.SetCurrentContainer(containerName);
            }

            await _next(context);
        }
    }
}