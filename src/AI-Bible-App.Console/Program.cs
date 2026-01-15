using AI_Bible_App.Console.Commands;
using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using AI_Bible_App.Infrastructure.Repositories;
using AI_Bible_App.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI_Bible_App.Console;

class Program
{
    static async Task Main(string[] args)
    {
        // Check for download command
        if (args.Length > 0 && args[0] == "download-bible")
        {
            System.Console.WriteLine("Running Bible data downloader...");
            var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<DownloadBibleDataCommand>();
            var command = new DownloadBibleDataCommand(logger);
            await command.ExecuteAsync();
            return;
        }
        
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.local.json", optional: true)
            .Build();

        // Setup dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Run the application
        var app = new BibleApp(
            serviceProvider.GetRequiredService<IAIService>(),
            serviceProvider.GetRequiredService<ICharacterRepository>(),
            serviceProvider.GetRequiredService<IChatRepository>(),
            serviceProvider.GetRequiredService<IPrayerRepository>(),
            serviceProvider.GetRequiredService<ILogger<BibleApp>>(),
            serviceProvider.GetService<IBibleRAGService>()
        );

        await app.RunAsync();
    }

    static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Register services
        // Add encryption and file security services
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IFileSecurityService, FileSecurityService>();
        
        // Use WEB Bible repository by default (can switch to JsonBibleRepository for KJV)
        var defaultTranslation = configuration["Bible:DefaultTranslation"] ?? "WEB";
        if (defaultTranslation.Equals("WEB", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IBibleRepository, WebBibleRepository>();
        }
        else
        {
            services.AddSingleton<IBibleRepository, JsonBibleRepository>();
        }
        
        services.AddSingleton<IBibleRAGService, BibleRAGService>();
        services.AddSingleton<IAIService, LocalAIService>();
        services.AddSingleton<ICharacterRepository, InMemoryCharacterRepository>();
        services.AddSingleton<IChatRepository, JsonChatRepository>();
        services.AddSingleton<IPrayerRepository, JsonPrayerRepository>();
        services.AddSingleton<BibleApp>();
    }
}
