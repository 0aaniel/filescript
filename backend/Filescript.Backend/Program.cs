using Filescript.Backend.Services;
using Filescript.Backend.Utilities;
using Filescript.Backend.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddSingleton<ContainerManager>();
// builder.Services.AddTransient<FileIOHelper>();
// builder.Services.AddTransient<IDirectoryService, DirectoryService>();
// builder.Services.AddTransient<IFileService, FileService>();
builder.Services.AddControllers();

// Configure logging (optional, already set up by default)
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Build the app
var app = builder.Build();

// Configure middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseRouting();

app.MapControllers();

app.Run();
