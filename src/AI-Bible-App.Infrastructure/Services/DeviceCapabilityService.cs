using AI_Bible_App.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.NetworkInformation;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Service for detecting device capabilities to determine optimal AI tier.
/// Provides platform-specific memory and capability detection.
/// </summary>
public class DeviceCapabilityService : IDeviceCapabilityService
{
    private readonly ILogger<DeviceCapabilityService> _logger;
    private readonly Func<bool>? _networkCheck;
    private DeviceCapabilityTier? _cachedTier;

    public DeviceCapabilityService(ILogger<DeviceCapabilityService> logger, Func<bool>? networkCheck = null)
    {
        _logger = logger;
        _networkCheck = networkCheck;
    }

    public DeviceCapabilityTier GetCapabilityTier()
    {
        if (_cachedTier.HasValue)
            return _cachedTier.Value;

        var memoryMB = GetAvailableMemoryMB();
        
        _cachedTier = memoryMB switch
        {
            < 1500 => DeviceCapabilityTier.Minimal,  // < 1.5GB - cached only
            < 2500 => DeviceCapabilityTier.Low,      // 1.5-2.5GB - tiny models (0.5B)
            < 5000 => DeviceCapabilityTier.Medium,   // 2.5-5GB - small models (1-2B)
            _ => DeviceCapabilityTier.High           // 5GB+ - larger models
        };

        _logger.LogInformation("Device capability tier: {Tier} (Available RAM: {RAM}MB)", 
            _cachedTier.Value, memoryMB);
        
        return _cachedTier.Value;
    }

    public long GetAvailableMemoryMB()
    {
        try
        {
            // Cross-platform memory detection
            if (OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst())
            {
                return GetAppleDeviceMemory();
            }
            else if (OperatingSystem.IsAndroid())
            {
                return GetAndroidMemory();
            }
            else if (OperatingSystem.IsWindows())
            {
                return GetWindowsMemory();
            }
            
            // Fallback: assume medium capability
            return 4000;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect device memory, assuming 4GB");
            return 4000;
        }
    }

    public bool HasNetworkConnectivity()
    {
        try
        {
            // Use injected network check if available (MAUI-specific)
            if (_networkCheck != null)
            {
                return _networkCheck();
            }
            
            // Fallback: try to ping or assume true
            return NetworkInterface.GetIsNetworkAvailable();
        }
        catch
        {
            // Assume connectivity if check fails
            return true;
        }
    }

    public AIBackendRecommendation GetRecommendedBackend()
    {
        var tier = GetCapabilityTier();
        var hasNetwork = HasNetworkConnectivity();
        var isMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
        var isDesktop = OperatingSystem.IsWindows() || OperatingSystem.IsMacCatalyst();

        var recommendation = new AIBackendRecommendation();

        // Desktop with Ollama available
        if (isDesktop && !isMobile)
        {
            recommendation.Primary = AIBackendType.LocalOllama;
            recommendation.Fallback = hasNetwork ? AIBackendType.Cloud : AIBackendType.OnDevice;
            recommendation.RecommendedModelName = "phi3:mini";
            recommendation.RecommendedContextSize = 4096;
            return recommendation;
        }

        // Mobile device recommendations based on tier
        switch (tier)
        {
            case DeviceCapabilityTier.High:
                // High-end mobile: prefer on-device, cloud fallback
                recommendation.Primary = hasNetwork ? AIBackendType.Cloud : AIBackendType.OnDevice;
                recommendation.Fallback = AIBackendType.OnDevice;
                recommendation.RecommendedModelName = "tinyllama-1.1b-chat-q4_k_m.gguf";
                recommendation.RecommendedContextSize = 2048;
                break;

            case DeviceCapabilityTier.Medium:
                // Mid-range: on-device tiny model, cloud fallback
                recommendation.Primary = hasNetwork ? AIBackendType.Cloud : AIBackendType.OnDevice;
                recommendation.Fallback = AIBackendType.OnDevice;
                recommendation.RecommendedModelName = "qwen2-0.5b-instruct-q4_k_m.gguf";
                recommendation.RecommendedContextSize = 1024;
                break;

            case DeviceCapabilityTier.Low:
                // Low-end: cloud preferred, tiny on-device backup
                recommendation.Primary = hasNetwork ? AIBackendType.Cloud : AIBackendType.OnDevice;
                recommendation.Fallback = AIBackendType.Cached;
                recommendation.RecommendedModelName = "qwen2-0.5b-instruct-q4_k_m.gguf";
                recommendation.RecommendedContextSize = 512;
                break;

            case DeviceCapabilityTier.Minimal:
            default:
                // Very limited: cloud only, cached fallback
                recommendation.Primary = hasNetwork ? AIBackendType.Cloud : AIBackendType.Cached;
                recommendation.Fallback = AIBackendType.Cached;
                recommendation.RecommendedModelName = null; // No on-device model
                recommendation.RecommendedContextSize = 256;
                break;
        }

        _logger.LogInformation(
            "AI Backend Recommendation: Primary={Primary}, Fallback={Fallback}, Model={Model}, Context={Context}",
            recommendation.Primary, recommendation.Fallback, 
            recommendation.RecommendedModelName ?? "none", recommendation.RecommendedContextSize);

        return recommendation;
    }

    private long GetAppleDeviceMemory()
    {
        // On iOS/Mac, we can use NSProcessInfo or estimate from device model
        // For now, estimate based on GC memory - actual implementation would use native APIs
        var gcMemory = GC.GetGCMemoryInfo();
        var totalMB = gcMemory.TotalAvailableMemoryBytes / (1024 * 1024);
        
        // iOS typically allows apps to use ~50% of device RAM
        // Adjust estimate based on platform hints
        if (totalMB < 100)
        {
            // GC info not available, estimate from environment
            // Most iOS devices 2021+ have 4GB+, older ones 2-3GB
            return 3000; // Conservative estimate
        }
        
        return totalMB;
    }

    private long GetAndroidMemory()
    {
        // On Android, estimate from available GC memory
        var gcMemory = GC.GetGCMemoryInfo();
        var totalMB = gcMemory.TotalAvailableMemoryBytes / (1024 * 1024);
        
        if (totalMB < 100)
        {
            // Fallback estimate for Android
            return 3000;
        }
        
        return totalMB;
    }

    private long GetWindowsMemory()
    {
        // Windows: use GC memory info which is more accurate
        var gcMemory = GC.GetGCMemoryInfo();
        var totalMB = gcMemory.TotalAvailableMemoryBytes / (1024 * 1024);
        
        // Windows typically has more memory available
        return totalMB > 100 ? totalMB : 8000;
    }
}
