using LLama;
using LLama.Common;
using LLama.Sampling;
using Microsoft.Extensions.Logging;
using AI_Bible_App.Core.Services;
using System.Runtime.CompilerServices;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Offline AI service using LLamaSharp for local model inference
/// Optimized like GPT4All for efficient local inference
/// </summary>
public class OfflineAIService : IOfflineAIService
{
    private readonly ILogger<OfflineAIService> _logger;
    private readonly string _modelsDirectory;
    private LLamaWeights? _weights;
    private LLamaContext? _context;
    private string _currentModelName = "phi-3.5-mini-instruct-q4";
    private bool _isInitialized = false;

    // Optimized model catalog (GPT4All-style efficient models)
    private readonly Dictionary<string, LocalModelInfo> _availableModels = new()
    {
        ["phi-3.5-mini-instruct-q4"] = new()
        {
            Name = "phi-3.5-mini-instruct-q4",
            DisplayName = "Phi 3.5 Mini (Recommended)",
            Description = "Microsoft's efficient 3.8B parameter model, optimized for instruction following. Best balance of speed and quality for spiritual conversations.",
            SizeInBytes = 2_300_000_000, // ~2.3GB
            Size = ModelSize.Small,
            ContextLength = 4096,
            RecommendedFor = "Most users - fast, accurate, good quality",
            FilePath = "phi-3.5-mini-instruct-q4_K_M.gguf"
        },
        ["llama-3.2-3b-instruct-q4"] = new()
        {
            Name = "llama-3.2-3b-instruct-q4",
            DisplayName = "Llama 3.2 3B",
            Description = "Meta's compact but powerful model. Excellent for biblical conversations with good theological understanding.",
            SizeInBytes = 1_900_000_000, // ~1.9GB
            Size = ModelSize.Small,
            ContextLength = 8192,
            RecommendedFor = "Fast devices - quick responses",
            FilePath = "llama-3.2-3b-instruct-q4_K_M.gguf"
        },
        ["mistral-7b-instruct-q4"] = new()
        {
            Name = "mistral-7b-instruct-q4",
            DisplayName = "Mistral 7B Instruct",
            Description = "High-quality 7B model with excellent reasoning. Best for deep theological discussions and wisdom.",
            SizeInBytes = 4_100_000_000, // ~4.1GB
            Size = ModelSize.Medium,
            ContextLength = 8192,
            RecommendedFor = "Powerful PCs - highest quality responses",
            FilePath = "mistral-7b-instruct-v0.3.Q4_K_M.gguf"
        },
        ["tinyllama-1.1b-q4"] = new()
        {
            Name = "tinyllama-1.1b-q4",
            DisplayName = "TinyLlama (Ultra-Fast)",
            Description = "Smallest model at 1.1B parameters. Very fast but simpler responses. Good for quick answers and slower devices.",
            SizeInBytes = 670_000_000, // ~670MB
            Size = ModelSize.Tiny,
            ContextLength = 2048,
            RecommendedFor = "Older PCs or quick testing",
            FilePath = "tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf"
        }
    };

    public OfflineAIService(ILogger<OfflineAIService> logger, string? dataDirectory = null)
    {
        _logger = logger;
        _modelsDirectory = Path.Combine(dataDirectory ?? "data", "models");
        Directory.CreateDirectory(_modelsDirectory);
    }

    public async Task<bool> IsOfflineModeAvailableAsync()
    {
        try
        {
            if (_isInitialized && _weights != null)
                return true;

            var currentModel = _availableModels[_currentModelName];
            var modelPath = Path.Combine(_modelsDirectory, currentModel.FilePath);

            return File.Exists(modelPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking offline mode availability");
            return false;
        }
    }

    public async Task<string> GetCompletionAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureModelLoadedAsync(cancellationToken);

            if (_context == null || _weights == null)
            {
                _logger.LogError("Model not loaded");
                return "I'm sorry, the offline model is not available. Please check your internet connection or download a local model.";
            }

            // Create executor for inference
            var executor = new InteractiveExecutor(_context);

            // Optimized prompt format for instruction models
            var prompt = $@"<|system|>
{systemPrompt}
<|user|>
{userMessage}
<|assistant|>";

            // Inference parameters optimized for quality and speed (GPT4All-style)
            var inferenceParams = new InferenceParams
            {
                MaxTokens = 512,
                SamplingPipeline = new DefaultSamplingPipeline
                {
                    Temperature = 0.7f,
                    TopP = 0.9f,
                    TopK = 40,
                    RepeatPenalty = 1.1f
                },
                AntiPrompts = new List<string> { "<|user|>", "<|system|>" }
            };

            var response = new System.Text.StringBuilder();

            await foreach (var text in executor.InferAsync(prompt, inferenceParams, cancellationToken))
            {
                response.Append(text);
            }

            var result = response.ToString().Trim();
            _logger.LogInformation("Offline completion generated: {Length} chars", result.Length);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Offline completion cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating offline completion");
            return "I apologize, but I'm having trouble generating a response offline. Please try again or check your connection.";
        }
    }

    public async IAsyncEnumerable<string> GetStreamingCompletionAsync(
        string systemPrompt, 
        string userMessage, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureModelLoadedAsync(cancellationToken);

        if (_context == null || _weights == null)
        {
            _logger.LogError("Model not loaded for streaming");
            yield return "Offline model not available.";
            yield break;
        }

        var executor = new InteractiveExecutor(_context);

        var prompt = $@"<|system|>
{systemPrompt}
<|user|>
{userMessage}
<|assistant|>";

        var inferenceParams = new InferenceParams
        {
            MaxTokens = 512,
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = 0.7f,
                TopP = 0.9f,
                TopK = 40,
                RepeatPenalty = 1.1f
            },
            AntiPrompts = new List<string> { "<|user|>", "<|system|>" }
        };

        await foreach (var text in executor.InferAsync(prompt, inferenceParams, cancellationToken))
        {
            yield return text;
        }
    }

    public Task<bool> IsModelDownloadedAsync(string modelName)
    {
        if (!_availableModels.ContainsKey(modelName))
            return Task.FromResult(false);

        var modelInfo = _availableModels[modelName];
        var modelPath = Path.Combine(_modelsDirectory, modelInfo.FilePath);
        return Task.FromResult(File.Exists(modelPath));
    }

    public Task<List<LocalModelInfo>> GetAvailableModelsAsync()
    {
        var models = _availableModels.Values.ToList();

        // Update download status
        foreach (var model in models)
        {
            var modelPath = Path.Combine(_modelsDirectory, model.FilePath);
            model.IsDownloaded = File.Exists(modelPath);
        }

        return Task.FromResult(models);
    }

    public async Task<bool> DownloadModelAsync(string modelName, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_availableModels.ContainsKey(modelName))
            {
                _logger.LogWarning("Model {ModelName} not found in catalog", modelName);
                return false;
            }

            var modelInfo = _availableModels[modelName];
            var modelPath = Path.Combine(_modelsDirectory, modelInfo.FilePath);

            if (File.Exists(modelPath))
            {
                _logger.LogInformation("Model {ModelName} already downloaded", modelName);
                return true;
            }

            // Download URLs for quantized GGUF models (Hugging Face)
            var downloadUrls = new Dictionary<string, string>
            {
                ["phi-3.5-mini-instruct-q4"] = "https://huggingface.co/bartowski/Phi-3.5-mini-instruct-GGUF/resolve/main/Phi-3.5-mini-instruct-Q4_K_M.gguf",
                ["llama-3.2-3b-instruct-q4"] = "https://huggingface.co/bartowski/Llama-3.2-3B-Instruct-GGUF/resolve/main/Llama-3.2-3B-Instruct-Q4_K_M.gguf",
                ["mistral-7b-instruct-q4"] = "https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.3-GGUF/resolve/main/mistral-7b-instruct-v0.3.Q4_K_M.gguf",
                ["tinyllama-1.1b-q4"] = "https://huggingface.co/TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF/resolve/main/tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf"
            };

            if (!downloadUrls.ContainsKey(modelName))
            {
                _logger.LogError("Download URL not configured for {ModelName}", modelName);
                return false;
            }

            _logger.LogInformation("Downloading model {ModelName} from {Url}", modelName, downloadUrls[modelName]);

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromHours(2) };
            using var response = await httpClient.GetAsync(downloadUrls[modelName], HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? modelInfo.SizeInBytes;
            var downloadedBytes = 0L;

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(modelPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                downloadedBytes += bytesRead;

                progress?.Report((double)downloadedBytes / totalBytes);
            }

            _logger.LogInformation("Model {ModelName} downloaded successfully to {Path}", modelName, modelPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading model {ModelName}", modelName);
            return false;
        }
    }

    public Task<bool> DeleteModelAsync(string modelName)
    {
        try
        {
            if (!_availableModels.ContainsKey(modelName))
                return Task.FromResult(false);

            var modelInfo = _availableModels[modelName];
            var modelPath = Path.Combine(_modelsDirectory, modelInfo.FilePath);

            if (File.Exists(modelPath))
            {
                // Unload if this is the current model
                if (modelName == _currentModelName)
                {
                    _weights?.Dispose();
                    _context?.Dispose();
                    _weights = null;
                    _context = null;
                    _isInitialized = false;
                }

                File.Delete(modelPath);
                _logger.LogInformation("Deleted model {ModelName}", modelName);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting model {ModelName}", modelName);
            return Task.FromResult(false);
        }
    }

    public string GetCurrentModelName() => _currentModelName;

    public async Task<bool> SwitchModelAsync(string modelName)
    {
        try
        {
            if (!_availableModels.ContainsKey(modelName))
            {
                _logger.LogWarning("Cannot switch to unknown model {ModelName}", modelName);
                return false;
            }

            if (!await IsModelDownloadedAsync(modelName))
            {
                _logger.LogWarning("Cannot switch to {ModelName} - not downloaded", modelName);
                return false;
            }

            // Unload current model
            _weights?.Dispose();
            _context?.Dispose();
            _weights = null;
            _context = null;
            _isInitialized = false;

            _currentModelName = modelName;
            _logger.LogInformation("Switched to model {ModelName}", modelName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error switching to model {ModelName}", modelName);
            return false;
        }
    }

    public Task<ModelRequirements> GetModelRequirementsAsync(string modelName)
    {
        if (!_availableModels.ContainsKey(modelName))
        {
            return Task.FromResult(new ModelRequirements
            {
                DiskSpaceRequired = 0,
                RamRequired = 0,
                MinimumCpu = "Unknown"
            });
        }

        var modelInfo = _availableModels[modelName];

        var requirements = new ModelRequirements
        {
            DiskSpaceRequired = modelInfo.SizeInBytes,
            RamRequired = modelInfo.SizeInBytes + 500_000_000, // Model size + 500MB overhead
            MinimumCpu = "x64 CPU with AVX2 support",
            GpuRecommended = modelInfo.Size >= ModelSize.Medium,
            EstimatedLoadTime = modelInfo.Size switch
            {
                ModelSize.Tiny => TimeSpan.FromSeconds(2),
                ModelSize.Small => TimeSpan.FromSeconds(5),
                ModelSize.Medium => TimeSpan.FromSeconds(10),
                ModelSize.Large => TimeSpan.FromSeconds(20),
                _ => TimeSpan.FromSeconds(5)
            }
        };

        return Task.FromResult(requirements);
    }

    private async Task EnsureModelLoadedAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized && _weights != null && _context != null)
            return;

        var currentModel = _availableModels[_currentModelName];
        var modelPath = Path.Combine(_modelsDirectory, currentModel.FilePath);

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Model file not found: {modelPath}. Please download the model first.");
        }

        _logger.LogInformation("Loading model {ModelName} from {Path}", _currentModelName, modelPath);

        // Optimized parameters for efficient inference (GPT4All-style)
        var parameters = new ModelParams(modelPath)
        {
            ContextSize = (uint)currentModel.ContextLength,
            GpuLayerCount = 0, // CPU-only for better compatibility; set to higher for GPU
            UseMemoryLock = false,
            UseMemorymap = true, // Memory-mapped files for efficiency
            Threads = (int?)Math.Max(1, Environment.ProcessorCount / 2) // Use half available cores
        };

        _weights = LLamaWeights.LoadFromFile(parameters);
        _context = _weights.CreateContext(parameters);
        _isInitialized = true;

        _logger.LogInformation("Model {ModelName} loaded successfully", _currentModelName);
    }

    public void Dispose()
    {
        _weights?.Dispose();
        _context?.Dispose();
    }
}
