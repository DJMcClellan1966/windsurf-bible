using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace AI_Bible_App.Infrastructure.Logging;

/// <summary>
/// Centralized Serilog logging configuration for the AI Bible App.
/// Provides structured logging to file and console for debugging user-reported issues.
/// </summary>
public static class SerilogConfiguration
{
    private static bool _isConfigured = false;
    private static readonly object _lock = new();

    /// <summary>
    /// Configure Serilog with file and console sinks.
    /// Call this early in application startup.
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="logDirectory">Directory for log files (defaults to AppData/AI-Bible-App/logs)</param>
    public static void ConfigureLogging(IConfiguration? configuration = null, string? logDirectory = null)
    {
        lock (_lock)
        {
            if (_isConfigured) return;

            var logPath = logDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AI-Bible-App",
                "logs");

            Directory.CreateDirectory(logPath);

            var logFile = Path.Combine(logPath, "ai-bible-app-.log");

            // Read minimum level from config or default to Information
            var minLevel = LogEventLevel.Information;
            if (configuration != null)
            {
                var levelStr = configuration["Logging:MinimumLevel"] ?? configuration["Serilog:MinimumLevel"];
                if (!string.IsNullOrEmpty(levelStr) && Enum.TryParse<LogEventLevel>(levelStr, true, out var parsed))
                {
                    minLevel = parsed;
                }
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(minLevel)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "AI-Bible-App")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: LogEventLevel.Information)
                .WriteTo.File(
                    logFile,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: LogEventLevel.Debug)
                .CreateLogger();

            _isConfigured = true;
            Log.Information("Serilog logging initialized. Log directory: {LogPath}", logPath);
        }
    }

    /// <summary>
    /// Add Serilog to the service collection for DI integration.
    /// </summary>
    public static IServiceCollection AddSerilogLogging(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigureLogging(configuration);
        
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: true);
        });

        return services;
    }

    /// <summary>
    /// Get the log directory path.
    /// </summary>
    public static string GetLogDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AI-Bible-App",
            "logs");
    }

    /// <summary>
    /// Flush and close the logger (call on app shutdown).
    /// </summary>
    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }
}

/// <summary>
/// Extension methods for logging common AI Bible App events.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Log an AI request with character and message context.
    /// </summary>
    public static void LogAIRequest(this Microsoft.Extensions.Logging.ILogger logger, string characterId, string message, string backend)
    {
        logger.LogInformation(
            "AI Request - Character: {CharacterId}, Backend: {Backend}, MessageLength: {Length}",
            characterId, backend, message.Length);
    }

    /// <summary>
    /// Log an AI response with timing information.
    /// </summary>
    public static void LogAIResponse(this Microsoft.Extensions.Logging.ILogger logger, string characterId, long elapsedMs, int responseLength)
    {
        logger.LogInformation(
            "AI Response - Character: {CharacterId}, Duration: {ElapsedMs}ms, ResponseLength: {Length}",
            characterId, elapsedMs, responseLength);
    }

    /// <summary>
    /// Log a RAG query with results count.
    /// </summary>
    public static void LogRAGQuery(this Microsoft.Extensions.Logging.ILogger logger, string query, int resultsCount, long elapsedMs)
    {
        logger.LogDebug(
            "RAG Query - Query: {Query}, Results: {Count}, Duration: {ElapsedMs}ms",
            query.Length > 50 ? query[..50] + "..." : query, resultsCount, elapsedMs);
    }

    /// <summary>
    /// Log a file I/O operation with safe error handling.
    /// </summary>
    public static void LogFileOperation(this Microsoft.Extensions.Logging.ILogger logger, string operation, string path, bool success, Exception? error = null)
    {
        if (success)
        {
            logger.LogDebug("File {Operation} succeeded: {Path}", operation, path);
        }
        else
        {
            logger.LogError(error, "File {Operation} failed: {Path}", operation, path);
        }
    }

    /// <summary>
    /// Log performance metrics for monitoring.
    /// </summary>
    public static void LogPerformance(this Microsoft.Extensions.Logging.ILogger logger, string operation, long elapsedMs, Dictionary<string, object>? metadata = null)
    {
        if (metadata != null && metadata.Any())
        {
            logger.LogInformation(
                "Performance - {Operation}: {ElapsedMs}ms, Metadata: {@Metadata}",
                operation, elapsedMs, metadata);
        }
        else
        {
            logger.LogInformation("Performance - {Operation}: {ElapsedMs}ms", operation, elapsedMs);
        }
    }
}
