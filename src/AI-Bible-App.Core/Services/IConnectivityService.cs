namespace AI_Bible_App.Core.Services;

/// <summary>
/// Service to detect internet connectivity
/// </summary>
public interface IConnectivityService
{
    /// <summary>
    /// Check if internet connection is available
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Event fired when connectivity changes
    /// </summary>
    event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;

    /// <summary>
    /// Test if OpenAI API is reachable
    /// </summary>
    Task<bool> CanReachOpenAIAsync(CancellationToken cancellationToken = default);
}

public class ConnectivityChangedEventArgs : EventArgs
{
    public bool IsConnected { get; set; }
    public ConnectionType ConnectionType { get; set; }
}

public enum ConnectionType
{
    None,
    Wifi,
    Ethernet,
    Mobile,
    Other
}
