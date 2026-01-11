namespace AI_Bible_App.Core.Services;

/// <summary>
/// Service for offline AI model management and inference
/// </summary>
public interface IOfflineAIService
{
    /// <summary>
    /// Check if offline mode is available and initialized
    /// </summary>
    Task<bool> IsOfflineModeAvailableAsync();

    /// <summary>
    /// Get chat completion using local model
    /// </summary>
    Task<string> GetCompletionAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get streaming chat completion using local model
    /// </summary>
    IAsyncEnumerable<string> GetStreamingCompletionAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a model is downloaded and ready
    /// </summary>
    Task<bool> IsModelDownloadedAsync(string modelName);

    /// <summary>
    /// Get list of available models
    /// </summary>
    Task<List<LocalModelInfo>> GetAvailableModelsAsync();

    /// <summary>
    /// Download a model
    /// </summary>
    Task<bool> DownloadModelAsync(string modelName, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a downloaded model
    /// </summary>
    Task<bool> DeleteModelAsync(string modelName);

    /// <summary>
    /// Get current active model name
    /// </summary>
    string GetCurrentModelName();

    /// <summary>
    /// Switch to a different downloaded model
    /// </summary>
    Task<bool> SwitchModelAsync(string modelName);

    /// <summary>
    /// Get estimated model size and requirements
    /// </summary>
    Task<ModelRequirements> GetModelRequirementsAsync(string modelName);
}

public class LocalModelInfo
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public long SizeInBytes { get; set; }
    public bool IsDownloaded { get; set; }
    public string FilePath { get; set; } = "";
    public ModelSize Size { get; set; }
    public int ContextLength { get; set; }
    public string RecommendedFor { get; set; } = "";
}

public class ModelRequirements
{
    public long DiskSpaceRequired { get; set; }
    public long RamRequired { get; set; }
    public string MinimumCpu { get; set; } = "Any";
    public bool GpuRecommended { get; set; }
    public TimeSpan EstimatedLoadTime { get; set; }
}

public enum ModelSize
{
    Tiny,      // < 1GB (fast, lower quality)
    Small,     // 1-3GB (balanced)
    Medium,    // 3-7GB (good quality)
    Large      // > 7GB (best quality, slower)
}
