namespace AI_Bible_App.Core.Interfaces;

/// <summary>
/// Service for pre-warming AI models to eliminate cold-start delays
/// </summary>
public interface IModelWarmupService
{
    /// <summary>
    /// Whether the model has been warmed up and is ready for fast responses
    /// </summary>
    bool IsWarmedUp { get; }
    
    /// <summary>
    /// Whether warmup is currently in progress
    /// </summary>
    bool IsWarmingUp { get; }
    
    /// <summary>
    /// Pre-load the model into memory for faster first response
    /// </summary>
    Task WarmupModelAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Event raised when warmup completes
    /// </summary>
    event EventHandler<bool>? WarmupCompleted;
}
