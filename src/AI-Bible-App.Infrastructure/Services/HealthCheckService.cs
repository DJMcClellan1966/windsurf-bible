using AI_Bible_App.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OllamaSharp;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Health check service for verifying Ollama and other dependencies
/// </summary>
public class HealthCheckService : IHealthCheckService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<HealthCheckService> _logger;
    private readonly string _ollamaUrl;

    public HealthCheckService(IConfiguration configuration, ILogger<HealthCheckService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _ollamaUrl = configuration["Ollama:Url"] ?? "http://localhost:11434";
    }

    public async Task<bool> IsOllamaAvailableAsync()
    {
        try
        {
            var client = new OllamaApiClient(_ollamaUrl);
            var models = await client.ListLocalModelsAsync();
            return models != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama health check failed");
            return false;
        }
    }

    public async Task<HealthStatus> GetHealthStatusAsync()
    {
        var status = new HealthStatus
        {
            IsHealthy = true,
            ComponentStatus = new Dictionary<string, bool>()
        };

        // Check Ollama
        var ollamaAvailable = await IsOllamaAvailableAsync();
        status.ComponentStatus["Ollama"] = ollamaAvailable;

        if (!ollamaAvailable)
        {
            status.IsHealthy = false;
            status.ErrorMessage = $"Ollama service is not available at {_ollamaUrl}. " +
                                 "Please ensure Ollama is installed and running. " +
                                 "Visit https://ollama.com for installation instructions.";
        }

        // Check if required models are available
        if (ollamaAvailable)
        {
            try
            {
                var client = new OllamaApiClient(_ollamaUrl);
                var models = await client.ListLocalModelsAsync();
                var modelName = _configuration["Ollama:ModelName"] ?? "phi4";
                var embeddingModel = _configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";

                var hasMainModel = models.Any(m => m.Name.Contains(modelName, StringComparison.OrdinalIgnoreCase));
                var hasEmbeddingModel = models.Any(m => m.Name.Contains(embeddingModel, StringComparison.OrdinalIgnoreCase));

                status.ComponentStatus[$"Model:{modelName}"] = hasMainModel;
                status.ComponentStatus[$"Model:{embeddingModel}"] = hasEmbeddingModel;

                if (!hasMainModel || !hasEmbeddingModel)
                {
                    status.IsHealthy = false;
                    var missingModels = new List<string>();
                    if (!hasMainModel) missingModels.Add(modelName);
                    if (!hasEmbeddingModel) missingModels.Add(embeddingModel);
                    
                    status.ErrorMessage = $"Required models not found: {string.Join(", ", missingModels)}. " +
                                         $"Run: ollama pull {string.Join(" && ollama pull ", missingModels)}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking Ollama models");
                status.ComponentStatus["ModelCheck"] = false;
            }
        }

        return status;
    }
}
