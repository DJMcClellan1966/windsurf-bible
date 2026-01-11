namespace AI_Bible_App.Core.Interfaces;

/// <summary>
/// Service for checking health of dependencies (Ollama, etc.)
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Check if Ollama service is available and responding
    /// </summary>
    Task<bool> IsOllamaAvailableAsync();

    /// <summary>
    /// Get detailed health status with error messages
    /// </summary>
    Task<HealthStatus> GetHealthStatusAsync();
}

/// <summary>
/// Health status result
/// </summary>
public class HealthStatus
{
    public bool IsHealthy { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, bool> ComponentStatus { get; set; } = new();
}
