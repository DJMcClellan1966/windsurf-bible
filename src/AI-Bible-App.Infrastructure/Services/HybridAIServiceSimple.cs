using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Simple hybrid AI service that tries local Ollama first with timeout,
/// then falls back to Groq cloud for fast responses.
/// </summary>
public class HybridAIServiceSimple : IAIService
{
    private readonly LocalAIService _localService;
    private readonly GroqAIService _groqService;
    private readonly CachedResponseAIService _cachedService;
    private readonly ILogger<HybridAIServiceSimple> _logger;
    private readonly bool _preferLocal;
    private readonly TimeSpan _localTimeout;
    private readonly bool _groqAvailable;

    public HybridAIServiceSimple(
        LocalAIService localService,
        GroqAIService groqService,
        CachedResponseAIService cachedService,
        IConfiguration configuration,
        ILogger<HybridAIServiceSimple> logger)
    {
        _localService = localService;
        _groqService = groqService;
        _cachedService = cachedService;
        _logger = logger;
        
        _preferLocal = configuration["AI:PreferLocal"] != "false";
        _groqAvailable = _groqService.IsAvailable;
        
        // Timeout for local before trying cloud (20 seconds default)
        var timeoutSeconds = int.TryParse(configuration["AI:LocalTimeoutSeconds"], out var t) ? t : 20;
        _localTimeout = TimeSpan.FromSeconds(timeoutSeconds);
        
        _logger.LogInformation("HybridAIServiceSimple initialized. PreferLocal={PreferLocal}, GroqAvailable={GroqAvailable}, LocalTimeout={Timeout}s",
            _preferLocal, _groqAvailable, timeoutSeconds);
    }

    public async Task<string> GetChatResponseAsync(
        BiblicalCharacter character,
        List<ChatMessage> conversationHistory,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        if (_preferLocal)
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(_localTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                
                _logger.LogDebug("Trying local AI service with {Timeout}s timeout", _localTimeout.TotalSeconds);
                var response = await _localService.GetChatResponseAsync(character, conversationHistory, userMessage, linkedCts.Token);
                _logger.LogDebug("Local AI responded successfully");
                return response;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Local AI timed out after {Timeout}s, falling back to Groq", _localTimeout.TotalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Local AI failed, falling back to Groq");
            }
        }

        // Fallback to Groq
        if (_groqAvailable)
        {
            try
            {
                _logger.LogInformation("Using Groq cloud service for fast response");
                return await _groqService.GetChatResponseAsync(character, conversationHistory, userMessage, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Groq failed, trying cached responses");
            }
        }

        // Final fallback to cached responses
        return await _cachedService.GetChatResponseAsync(character, conversationHistory, userMessage, cancellationToken);
    }

    public async IAsyncEnumerable<string> StreamChatResponseAsync(
        BiblicalCharacter character,
        List<ChatMessage> conversationHistory,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Try local first with timeout
        if (_preferLocal)
        {
            Exception? localError = null;
            using var timeoutCts = new CancellationTokenSource(_localTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            var localEnumerator = _localService.StreamChatResponseAsync(character, conversationHistory, userMessage, linkedCts.Token).GetAsyncEnumerator(linkedCts.Token);
            
            while (true)
            {
                string? chunk = null;
                try
                {
                    if (!await localEnumerator.MoveNextAsync())
                        break;
                    chunk = localEnumerator.Current;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Local AI stream timed out, falling back to Groq");
                    localError = new TimeoutException();
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Local AI stream failed, falling back to Groq");
                    localError = ex;
                    break;
                }
                
                if (chunk != null)
                    yield return chunk;
            }
            
            await localEnumerator.DisposeAsync();
            
            if (localError == null)
                yield break;
        }

        // Fallback to Groq (non-streaming, yields full response)
        if (_groqAvailable)
        {
            await foreach (var chunk in _groqService.StreamChatResponseAsync(character, conversationHistory, userMessage, cancellationToken))
            {
                yield return chunk;
            }
            yield break;
        }

        // Final fallback
        await foreach (var chunk in _cachedService.StreamChatResponseAsync(character, conversationHistory, userMessage, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async Task<string> GeneratePrayerAsync(string topic, CancellationToken cancellationToken = default)
    {
        // For prayers, try Groq first if available (much faster for short content)
        if (_groqAvailable)
        {
            try
            {
                _logger.LogDebug("Using Groq for fast prayer generation");
                return await _groqService.GeneratePrayerAsync(topic, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Groq prayer failed, trying local");
            }
        }

        // Fallback to local
        try
        {
            return await _localService.GeneratePrayerAsync(topic, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Local prayer failed, using cached");
        }

        return await _cachedService.GeneratePrayerAsync(topic, cancellationToken);
    }

    public async Task<string> GeneratePersonalizedPrayerAsync(PrayerOptions options, CancellationToken cancellationToken = default)
    {
        // For prayers, try Groq first if available
        if (_groqAvailable)
        {
            try
            {
                return await _groqService.GeneratePersonalizedPrayerAsync(options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Groq personalized prayer failed, trying local");
            }
        }

        try
        {
            return await _localService.GeneratePersonalizedPrayerAsync(options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Local personalized prayer failed, using cached");
        }

        return await _cachedService.GeneratePersonalizedPrayerAsync(options, cancellationToken);
    }

    public async Task<string> GenerateDevotionalAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        if (_groqAvailable)
        {
            try
            {
                return await _groqService.GenerateDevotionalAsync(date, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Groq devotional failed, trying local");
            }
        }

        try
        {
            return await _localService.GenerateDevotionalAsync(date, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Local devotional failed, using cached");
        }

        return await _cachedService.GenerateDevotionalAsync(date, cancellationToken);
    }
}
