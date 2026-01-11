using AI_Bible_App.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.Models.Chat;
using System.Diagnostics;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Service that pre-warms the local AI model on startup for faster first responses.
/// Sends a minimal prompt to load the model into GPU/CPU memory.
/// </summary>
public class ModelWarmupService : IModelWarmupService
{
    private readonly ILogger<ModelWarmupService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _ollamaUrl;
    private readonly string _modelName;
    private readonly int _numGpu;
    private readonly int _numThread;
    private volatile bool _isWarmedUp;
    private volatile bool _isWarmingUp;
    
    public bool IsWarmedUp => _isWarmedUp;
    public bool IsWarmingUp => _isWarmingUp;
    
    public event EventHandler<bool>? WarmupCompleted;

    public ModelWarmupService(
        IConfiguration configuration,
        ILogger<ModelWarmupService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        _ollamaUrl = configuration["Ollama:Url"] ?? "http://localhost:11434";
        _modelName = configuration["Ollama:ModelName"] ?? "phi3.5:3.8b-mini-instruct-q4_K_M";
        _numGpu = int.TryParse(configuration["Ollama:NumGpu"], out var gpu) ? gpu : -1; // -1 = auto
        _numThread = int.TryParse(configuration["Ollama:NumThread"], out var thread) ? thread : 0; // 0 = auto
    }

    public async Task WarmupModelAsync(CancellationToken cancellationToken = default)
    {
        if (_isWarmedUp || _isWarmingUp)
        {
            _logger.LogDebug("Model warmup skipped - already warmed up or warming up");
            return;
        }

        _isWarmingUp = true;
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation(
                "🔥 Pre-warming model {Model} at {Url} (GPU layers: {Gpu}, Threads: {Thread})...", 
                _modelName, _ollamaUrl, _numGpu == -1 ? "auto" : _numGpu, _numThread == 0 ? "auto" : _numThread);
            
            // Create client for warmup
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(_ollamaUrl),
                Timeout = TimeSpan.FromMinutes(5)
            };
            
            var client = new OllamaApiClient(httpClient, _ollamaUrl)
            {
                SelectedModel = _modelName
            };
            
            // Send a minimal "ping" prompt to load model into memory
            var warmupRequest = new ChatRequest
            {
                Model = _modelName,
                Messages = new List<Message>
                {
                    new Message 
                    { 
                        Role = ChatRole.User, 
                        Content = "Hi" 
                    }
                },
                Options = new OllamaSharp.Models.RequestOptions
                {
                    NumCtx = 256,        // Small context for warmup
                    NumPredict = 5,      // Minimal response
                    NumGpu = _numGpu,    // GPU acceleration setting
                    NumThread = _numThread > 0 ? _numThread : null // CPU threads
                }
            };
            
            // Execute warmup - just need to start the model loading
            await foreach (var response in client.ChatAsync(warmupRequest, cancellationToken))
            {
                // Just consume the first few tokens to confirm model is loaded
                if (response?.Done == true)
                    break;
            }
            
            stopwatch.Stop();
            _isWarmedUp = true;
            
            _logger.LogInformation(
                "✅ Model {Model} warmed up successfully in {ElapsedMs}ms", 
                _modelName, stopwatch.ElapsedMilliseconds);
            
            WarmupCompleted?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, 
                "⚠️ Model warmup failed after {ElapsedMs}ms - first response may be slower. Error: {Message}", 
                stopwatch.ElapsedMilliseconds, ex.Message);
            
            WarmupCompleted?.Invoke(this, false);
        }
        finally
        {
            _isWarmingUp = false;
        }
    }
}
