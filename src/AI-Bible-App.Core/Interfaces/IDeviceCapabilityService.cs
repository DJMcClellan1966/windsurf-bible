namespace AI_Bible_App.Core.Interfaces;

/// <summary>
/// Service for detecting device capabilities to determine optimal AI tier
/// </summary>
public interface IDeviceCapabilityService
{
    /// <summary>
    /// Get the device capability tier for AI inference
    /// </summary>
    DeviceCapabilityTier GetCapabilityTier();
    
    /// <summary>
    /// Get available RAM in MB
    /// </summary>
    long GetAvailableMemoryMB();
    
    /// <summary>
    /// Check if the device has network connectivity
    /// </summary>
    bool HasNetworkConnectivity();
    
    /// <summary>
    /// Get the recommended AI backend based on device capabilities
    /// </summary>
    AIBackendRecommendation GetRecommendedBackend();
}

/// <summary>
/// Device capability tiers for AI inference
/// </summary>
public enum DeviceCapabilityTier
{
    /// <summary>
    /// Very limited device (< 2GB RAM) - use cached responses only
    /// </summary>
    Minimal = 0,
    
    /// <summary>
    /// Low-end device (2-3GB RAM) - can run tiny models (0.5B)
    /// </summary>
    Low = 1,
    
    /// <summary>
    /// Mid-range device (3-6GB RAM) - can run small models (1-2B)
    /// </summary>
    Medium = 2,
    
    /// <summary>
    /// High-end device (6GB+ RAM) - can run larger models (3B+)
    /// </summary>
    High = 3
}

/// <summary>
/// Recommendation for which AI backend to use
/// </summary>
public class AIBackendRecommendation
{
    /// <summary>
    /// Primary backend to use
    /// </summary>
    public AIBackendType Primary { get; set; }
    
    /// <summary>
    /// Fallback backend if primary fails
    /// </summary>
    public AIBackendType Fallback { get; set; }
    
    /// <summary>
    /// Emergency fallback (cached responses)
    /// </summary>
    public AIBackendType Emergency { get; set; } = AIBackendType.Cached;
    
    /// <summary>
    /// Recommended model name for on-device inference
    /// </summary>
    public string? RecommendedModelName { get; set; }
    
    /// <summary>
    /// Recommended context size based on available memory
    /// </summary>
    public int RecommendedContextSize { get; set; } = 1024;
}

/// <summary>
/// Available AI backend types
/// </summary>
public enum AIBackendType
{
    /// <summary>
    /// Local Ollama server (desktop only)
    /// </summary>
    LocalOllama,
    
    /// <summary>
    /// On-device LLamaSharp inference
    /// </summary>
    OnDevice,
    
    /// <summary>
    /// Cloud API (Groq)
    /// </summary>
    Cloud,
    
    /// <summary>
    /// Pre-cached responses (emergency fallback)
    /// </summary>
    Cached
}
