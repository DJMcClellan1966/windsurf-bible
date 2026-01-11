using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using AI_Bible_App.Core.Services;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Service to detect and monitor internet connectivity
/// </summary>
public class ConnectivityService : IConnectivityService
{
    private readonly ILogger<ConnectivityService> _logger;
    private bool _isConnected;

    public event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged;

    public ConnectivityService(ILogger<ConnectivityService> logger)
    {
        _logger = logger;
        _isConnected = CheckConnectivity();

        // Monitor network changes
        NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
    }

    public bool IsConnected
    {
        get
        {
            _isConnected = CheckConnectivity();
            return _isConnected;
        }
    }

    public async Task<bool> CanReachOpenAIAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return false;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.GetAsync("https://api.openai.com", cancellationToken);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized; // 401 means API is reachable
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot reach OpenAI API");
            return false;
        }
    }

    private bool CheckConnectivity()
    {
        try
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking network availability");
            return false;
        }
    }

    private ConnectionType GetConnectionType()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .ToList();

            if (interfaces.Any(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                return ConnectionType.Wifi;

            if (interfaces.Any(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                return ConnectionType.Ethernet;

            if (interfaces.Any(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ppp))
                return ConnectionType.Mobile;

            return interfaces.Any() ? ConnectionType.Other : ConnectionType.None;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining connection type");
            return ConnectionType.Other;
        }
    }

    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        var wasConnected = _isConnected;
        _isConnected = e.IsAvailable;

        if (wasConnected != _isConnected)
        {
            _logger.LogInformation("Network connectivity changed: {IsConnected}", _isConnected);
            ConnectivityChanged?.Invoke(this, new ConnectivityChangedEventArgs
            {
                IsConnected = _isConnected,
                ConnectionType = GetConnectionType()
            });
        }
    }

    private void OnNetworkAddressChanged(object? sender, EventArgs e)
    {
        var wasConnected = _isConnected;
        _isConnected = CheckConnectivity();

        if (wasConnected != _isConnected)
        {
            _logger.LogInformation("Network address changed, connectivity: {IsConnected}", _isConnected);
            ConnectivityChanged?.Invoke(this, new ConnectivityChangedEventArgs
            {
                IsConnected = _isConnected,
                ConnectionType = GetConnectionType()
            });
        }
    }
}
