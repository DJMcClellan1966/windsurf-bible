using AI_Bible_App.Core.Models;
using AI_Bible_App.Core.Services;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Detects device capabilities and recommends optimal AI configuration
/// </summary>
public class DeviceCapabilityService : IDeviceCapabilityService
{
    private readonly ILogger<DeviceCapabilityService> _logger;
    private DeviceCapabilities? _cachedCapabilities;
    private ModelConfiguration? _userOverride;

    public DeviceCapabilityService(ILogger<DeviceCapabilityService> logger)
    {
        _logger = logger;
    }

    public async Task<DeviceCapabilities> DetectCapabilitiesAsync()
    {
        if (_cachedCapabilities != null)
            return _cachedCapabilities;

        var capabilities = new DeviceCapabilities
        {
            DeviceId = Environment.MachineName,
            DeviceName = Environment.MachineName,
            Platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
                      RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" :
                      RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" : "Unknown"
        };

        // Detect CPU
        capabilities.CpuCoreCount = Environment.ProcessorCount;
        capabilities.CpuArchitecture = RuntimeInformation.ProcessArchitecture.ToString();

        // Detect memory (Windows-specific using native API)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    capabilities.TotalMemoryBytes = (long)memStatus.ullTotalPhys;
                    capabilities.AvailableMemoryBytes = (long)memStatus.ullAvailPhys;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to detect memory using Windows API");
            }
        }

        // Detect GPU (basic check)
        capabilities.HasDedicatedGpu = await DetectGpuAsync();

        // Calculate performance tier
        capabilities.PerformanceTier = CalculatePerformanceTier(capabilities);

        // Set recommendations
        var config = GetConfigurationForTier(capabilities.PerformanceTier);
        capabilities.RecommendedModelSize = config.ModelSize;
        capabilities.RecommendedMaxContextLength = config.ContextLength;

        _cachedCapabilities = capabilities;
        return capabilities;
    }

    public async Task<ModelConfiguration> GetRecommendedConfigurationAsync()
    {
        if (_userOverride != null)
            return _userOverride;

        var capabilities = await DetectCapabilitiesAsync();
        return GetConfigurationForTier(capabilities.PerformanceTier);
    }

    public List<ModelConfiguration> GetAvailableConfigurations()
    {
        return new List<ModelConfiguration>
        {
            new()
            {
                TierId = "low",
                DisplayName = "Low Performance (Budget Devices)",
                MinimumTier = DevicePerformanceTier.Low,
                ModelSize = "2.5GB",
                ContextLength = 2048,
                MaxHistoricalContexts = 1,
                MaxLanguageInsights = 2,
                MaxThematicConnections = 1,
                UseKnowledgeBasePagination = true,
                NumGpuLayers = 0,
                PreferCloudOffloading = true
            },
            new()
            {
                TierId = "medium",
                DisplayName = "Medium Performance (Standard Devices)",
                MinimumTier = DevicePerformanceTier.Medium,
                ModelSize = "4GB",
                ContextLength = 4096,
                MaxHistoricalContexts = 2,
                MaxLanguageInsights = 3,
                MaxThematicConnections = 2,
                UseKnowledgeBasePagination = true,
                NumGpuLayers = 10,
                PreferCloudOffloading = false
            },
            new()
            {
                TierId = "high",
                DisplayName = "High Performance (Gaming/Work)",
                MinimumTier = DevicePerformanceTier.High,
                ModelSize = "6GB",
                ContextLength = 8192,
                MaxHistoricalContexts = 3,
                MaxLanguageInsights = 5,
                MaxThematicConnections = 3,
                UseKnowledgeBasePagination = false,
                NumGpuLayers = 20,
                PreferCloudOffloading = false
            },
            new()
            {
                TierId = "ultra",
                DisplayName = "Ultra Performance (Enthusiast)",
                MinimumTier = DevicePerformanceTier.Ultra,
                ModelSize = "8GB+",
                ContextLength = 16384,
                MaxHistoricalContexts = 5,
                MaxLanguageInsights = 10,
                MaxThematicConnections = 5,
                UseKnowledgeBasePagination = false,
                NumGpuLayers = 35,
                PreferCloudOffloading = false
            }
        };
    }

    public async Task SetConfigurationAsync(string tierId)
    {
        var config = GetAvailableConfigurations().FirstOrDefault(c => c.TierId == tierId);
        if (config != null)
        {
            _userOverride = config;
            _logger.LogInformation("User manually set configuration to {TierId}", tierId);
        }
    }

    public bool CanHandleConfiguration(ModelConfiguration config, DeviceCapabilities capabilities)
    {
        return capabilities.PerformanceTier >= config.MinimumTier;
    }

    private DevicePerformanceTier CalculatePerformanceTier(DeviceCapabilities capabilities)
    {
        var ramGB = capabilities.TotalMemoryBytes / (1024.0 * 1024 * 1024);

        if (ramGB < 4)
            return DevicePerformanceTier.Low;
        if (ramGB < 8)
            return DevicePerformanceTier.Medium;
        if (ramGB < 16)
            return DevicePerformanceTier.High;

        return DevicePerformanceTier.Ultra;
    }

    private ModelConfiguration GetConfigurationForTier(DevicePerformanceTier tier)
    {
        return GetAvailableConfigurations().First(c => c.MinimumTier == tier);
    }

    private async Task<bool> DetectGpuAsync()
    {
        // Simple GPU detection - check for NVIDIA/AMD in system
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = "path win32_VideoController get name",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(psi);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    return output.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                           output.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                           output.Contains("Radeon", StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        catch
        {
            // GPU detection is best-effort
        }

        return false;
    }

    #region Windows API for Memory Detection

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    #endregion
}
