using Filescript.Backend.Services;
using Filescript.Backend.Services.Interfaces;
using Filescript.Backend.Utilities;
using Filescript.Models;

namespace Filescript.Backend;
public class Program {

    private FileIOHelper fileIOHelper;
    private ContainerMetadata metadata;

    public void ConfigureServices(IServiceCollection services) {

        // Register FileIOHelper with parameters
        services.AddSingleton<FileIOHelper>(provider => {
            var logger = provider.GetRequiredService<ILogger<FileIOHelper>>();
            string containerFilePath = "container.dat";
            return new FileIOHelper(logger, containerFilePath);
        });

        // Register ContainerMetadata
        services.AddSingleton<ContainerMetadata>(provider => {
            int totalBlocks = 10000;
            int blockSize = 4096;
            return new ContainerMetadata(totalBlocks, blockSize);
        });

        services.AddSingleton<ContainerManager>();
        services.AddTransient<FileIOHelper>();
        services.AddTransient<IDirectoryService, DirectoryService>();

        // Register DirectoryService as IDirectoryService
        services.AddSingleton<IDirectoryService, DirectoryService>();

        // Register FileService as IFileService
        services.AddSingleton<IFileService, FileService>();

        // Register Superblock
        services.AddSingleton<Superblock>(provider => {
            int totalBlocks = 10000;
            int blockSize = 4096;
            return new Superblock(totalBlocks, blockSize);
        });

        // Add controllers
        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
        
        fileIOHelper.InitializeContainerAsync(metadata.FreeBlocks.Count).GetAwaiter().GetResult();
        // Middleware
        app.UseRouting();
        app.UseEndpoints(endpoints => {
            endpoints.MapControllers();
        });
    }
}