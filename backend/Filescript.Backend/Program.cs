<<<<<<< HEAD
using Filescript.Backend.Services;
using Filescript.Backend.Utilities;
using Filescript.Backend.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
=======
using Filescript.Backend.Middleware;
using Filescript.Backend.Services;
using Filescript.Backend.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
>>>>>>> 2c7fc1d452f3d8b7ae4ae8da4de75d31f912fdd3

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddSingleton<ContainerManager>();
<<<<<<< HEAD
// builder.Services.AddTransient<FileIOHelper>();
// builder.Services.AddTransient<IDirectoryService, DirectoryService>();
// builder.Services.AddTransient<IFileService, FileService>();
=======
>>>>>>> 2c7fc1d452f3d8b7ae4ae8da4de75d31f912fdd3
builder.Services.AddControllers();

// Configure logging (optional, already set up by default)
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Build the app
var app = builder.Build();

// Configure middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseRouting();

<<<<<<< HEAD
=======
// Map controllers
>>>>>>> 2c7fc1d452f3d8b7ae4ae8da4de75d31f912fdd3
app.MapControllers();

// Run the app
app.Run();
