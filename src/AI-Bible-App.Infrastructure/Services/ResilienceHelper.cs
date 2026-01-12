using Microsoft.Extensions.Logging;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Provides resilient execution patterns with retry and fallback
/// </summary>
public class ResilienceHelper
{
    private readonly ILogger _logger;
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;

    public ResilienceHelper(ILogger logger, int maxRetries = 3, int baseDelayMs = 500)
    {
        _logger = logger;
        _maxRetries = maxRetries;
        _baseDelay = TimeSpan.FromMilliseconds(baseDelayMs);
    }

    /// <summary>
    /// Execute an action with exponential backoff retry
    /// </summary>
    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> action,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default)
    {
        shouldRetry ??= ex => IsTransientException(ex);
        
        Exception? lastException = null;
        
        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await action();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (shouldRetry(ex) && attempt < _maxRetries)
            {
                lastException = ex;
                var delay = GetExponentialBackoffDelay(attempt);
                
                _logger.LogWarning(
                    "Transient error on attempt {Attempt}/{MaxRetries}: {Error}. Retrying in {Delay}ms",
                    attempt + 1, _maxRetries + 1, ex.Message, delay.TotalMilliseconds);
                
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogError(ex, "Non-transient error, not retrying: {Error}", ex.Message);
                throw;
            }
        }

        _logger.LogError(lastException, "All {Retries} retry attempts failed", _maxRetries + 1);
        throw lastException ?? new InvalidOperationException("Unknown error during retry");
    }

    /// <summary>
    /// Execute with fallback: try primary, then fallback on failure
    /// </summary>
    public async Task<T> ExecuteWithFallbackAsync<T>(
        Func<Task<T>> primary,
        Func<Task<T>> fallback,
        string primaryName = "primary",
        string fallbackName = "fallback",
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await primary();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Primary} failed with: {Error}. Falling back to {Fallback}",
                primaryName, ex.Message, fallbackName);
            
            return await fallback();
        }
    }

    /// <summary>
    /// Execute with timeout
    /// </summary>
    public async Task<T> ExecuteWithTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> action,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            return await action(cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds}s");
        }
    }

    private TimeSpan GetExponentialBackoffDelay(int attempt)
    {
        // Exponential backoff: baseDelay * 2^attempt + random jitter
        var exponentialDelay = _baseDelay.TotalMilliseconds * Math.Pow(2, attempt);
        var jitter = Random.Shared.Next(0, (int)(exponentialDelay * 0.2)); // 20% jitter
        return TimeSpan.FromMilliseconds(exponentialDelay + jitter);
    }

    private static bool IsTransientException(Exception ex)
    {
        // Common transient exceptions
        return ex is HttpRequestException ||
               ex is TimeoutException ||
               ex is TaskCanceledException ||
               ex.Message.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Standard error messages for user display
/// </summary>
public static class UserFriendlyErrors
{
    public const string OllamaConnectionError = "Unable to connect to Ollama. Please ensure Ollama is running:\n\n1. Open PowerShell\n2. Run: ollama serve\n\nOr restart Ollama from your system tray.";
    public const string TimeoutError = "The request took too long. The model may be loading (first request can take 30+ seconds). Please wait and try again.";
    public const string ServiceUnavailable = "The AI service is temporarily unavailable. Please try again in a moment.";
    public const string GeneralError = "Something went wrong. Please try again.";
    public const string OfflineMode = "You're offline. Using cached responses which may be limited.";
    
    public static string GetUserFriendlyMessage(Exception ex)
    {
        if (ex is TimeoutException)
            return TimeoutError;
            
        if (ex is HttpRequestException httpEx)
        {
            // Check if it's a localhost connection issue (Ollama not running)
            if (httpEx.Message.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
                httpEx.Message.Contains("11434", StringComparison.OrdinalIgnoreCase) ||
                httpEx.Message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase))
            {
                return OllamaConnectionError;
            }
            return OllamaConnectionError; // Default to Ollama error for HTTP issues
        }
            
        if (ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("11434", StringComparison.OrdinalIgnoreCase))
            return OllamaConnectionError;
            
        if (ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            return TimeoutError;
            
        if (ex.Message.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("503", StringComparison.OrdinalIgnoreCase))
            return ServiceUnavailable;
            
        return GeneralError;
    }
}
