using Filescript.Backend.Middleware;
using Filescript.Backend.Services.Interfaces;
using Filescript.Backend.Services;
using Filescript.Backend.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddSingleton<ContainerManager>();
builder.Services.AddControllers();

// Register HttpContextAccessor to access headers
builder.Services.AddHttpContextAccessor();


builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IResiliencyService, ResiliencyService>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// Configure middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseRouting();
app.MapControllers();
app.Run();