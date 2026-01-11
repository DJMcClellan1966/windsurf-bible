using AI_Bible_App.Core.Models;

namespace AI_Bible_App.Core.Interfaces;

/// <summary>
/// Service for text-to-speech reading of character responses
/// </summary>
public interface ICharacterVoiceService
{
    /// <summary>
    /// Whether the service is currently speaking
    /// </summary>
    bool IsSpeaking { get; }
    
    /// <summary>
    /// Speaks the given text using the character's voice configuration
    /// </summary>
    /// <param name="text">The text to speak</param>
    /// <param name="voiceConfig">Voice configuration for the character</param>
    /// <param name="cancellationToken">Cancellation token to stop speech</param>
    Task SpeakAsync(string text, VoiceConfig voiceConfig, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops any ongoing speech
    /// </summary>
    Task StopSpeakingAsync();
    
    /// <summary>
    /// Gets available voice locales on the device
    /// </summary>
    Task<IEnumerable<string>> GetAvailableLocalesAsync();
}
