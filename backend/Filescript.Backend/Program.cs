using Filescript.Backend.Middleware;
using Filescript.Backend.Services;
using Filescript.Backend.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddSingleton<ContainerManager>();
builder.Services.AddControllers();

// Configure logging (optional, already set up by default)
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Build the app
var app = builder.Build();

// Configure middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseRouting();

// Map controllers
app.MapControllers();

// Run the app
app.Run();
