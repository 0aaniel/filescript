using Filescript.Backend.Middleware;
using Filescript.Backend.Services;
using Filescript.Backend.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configure CORS first
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
    {
        policyBuilder
            .WithOrigins("http://localhost:3000", "https://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Content-Type");
    });
});

// Add HTTPS configuration
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 5001;
});

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
app.UseCors();

// Add health check endpoint
app.MapGet("/api/health/frontend/ping", () => Results.Ok("Healthy"));

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseHttpsRedirection();
    // Force HTTP in development
    app.Urls.Clear();
    app.Urls.Add("https://localhost:5001");
}

// Add middleware to log all requests
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation(
        "Request: {Method} {Path} {Headers}",
        context.Request.Method,
        context.Request.Path,
        string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}={h.Value}"))
    );
    await next();
});

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

// Run the app
app.Run();
