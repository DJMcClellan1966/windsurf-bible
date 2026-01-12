namespace AI_Bible_App.Core.Models;

/// <summary>
/// Type of AI backend
/// </summary>
public enum AIBackendType
{
    LocalOllama,
    OnDevice,
    Cloud,
    Cached
}

/// <summary>
/// Recommended AI backend based on device capabilities
/// </summary>
public class AIBackendRecommendation
{
    public AIBackendType PrimaryBackend { get; set; }
    public AIBackendType FallbackBackend { get; set; }
    public string Reason { get; set; } = string.Empty;
}
