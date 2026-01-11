using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Windows.Media.SpeechSynthesis;
using Windows.Media.Playback;
using Windows.Media.Core;

namespace AI_Bible_App.Maui.Platforms.Windows.Services;

/// <summary>
/// Windows-specific TTS implementation using Windows.Media.SpeechSynthesis
/// This provides better TTS support on Windows than the MAUI default
/// </summary>
public class WindowsSpeechService : ICharacterVoiceService
{
    private readonly SpeechSynthesizer _synthesizer;
    private MediaPlayer? _mediaPlayer;
    private CancellationTokenSource? _currentCts;
    private bool _isSpeaking;

    public bool IsSpeaking => _isSpeaking;

    public WindowsSpeechService()
    {
        _synthesizer = new SpeechSynthesizer();
    }

    public async Task SpeakAsync(string text, VoiceConfig voiceConfig, CancellationToken cancellationToken = default)
    {
        // Stop any ongoing speech
        await StopSpeakingAsync();

        // Clean the text for better speech
        var cleanedText = CleanTextForSpeech(text);
        
        if (string.IsNullOrWhiteSpace(cleanedText))
            return;

        _currentCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _isSpeaking = true;

        try
        {
            // Configure voice options
            ConfigureVoice(voiceConfig);

            // Synthesize speech to stream
            var synthesisStream = await _synthesizer.SynthesizeTextToStreamAsync(cleanedText);

            // Create media player for playback
            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.Source = MediaSource.CreateFromStream(synthesisStream, synthesisStream.ContentType);
            
            // Set playback rate based on Rate property (not Pitch) for more natural speech
            // Rate of 0.8-1.2 sounds more natural than varying pitch
            _mediaPlayer.PlaybackSession.PlaybackRate = Math.Max(0.5, Math.Min(1.5, voiceConfig.Rate));
            
            // Set volume
            _mediaPlayer.Volume = voiceConfig.Volume;

            // Use TaskCompletionSource to await playback completion
            var playbackCompleted = new TaskCompletionSource<bool>();
            
            _mediaPlayer.MediaEnded += (s, e) =>
            {
                playbackCompleted.TrySetResult(true);
            };
            
            _mediaPlayer.MediaFailed += (s, e) =>
            {
                playbackCompleted.TrySetException(new Exception($"Media playback failed: {e.ErrorMessage}"));
            };

            // Start playback
            _mediaPlayer.Play();

            // Wait for completion or cancellation
            using (cancellationToken.Register(() => 
            {
                _mediaPlayer?.Pause();
                playbackCompleted.TrySetCanceled();
            }))
            {
                await playbackCompleted.Task;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled
            System.Diagnostics.Debug.WriteLine("[TTS] Speech cancelled");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TTS] Error: {ex.Message}");
            throw;
        }
        finally
        {
            _isSpeaking = false;
            _mediaPlayer?.Dispose();
            _mediaPlayer = null;
            _currentCts?.Dispose();
            _currentCts = null;
        }
    }

    public Task StopSpeakingAsync()
    {
        if (_currentCts != null && !_currentCts.IsCancellationRequested)
        {
            _currentCts.Cancel();
        }
        
        _mediaPlayer?.Pause();
        _isSpeaking = false;
        
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetAvailableLocalesAsync()
    {
        var voices = SpeechSynthesizer.AllVoices;
        var locales = voices.Select(v => v.Language).Distinct();
        return Task.FromResult(locales);
    }

    private void ConfigureVoice(VoiceConfig voiceConfig)
    {
        // Try to find a matching voice by locale
        var voices = SpeechSynthesizer.AllVoices;
        
        // First try exact locale match
        var matchingVoice = voices.FirstOrDefault(v => 
            v.Language.Equals(voiceConfig.Locale, StringComparison.OrdinalIgnoreCase));
        
        // Then try language prefix match
        if (matchingVoice == null)
        {
            var langPrefix = voiceConfig.Locale.Split('-')[0];
            matchingVoice = voices.FirstOrDefault(v => 
                v.Language.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase));
        }

        // Prefer gender match if specified in description
        if (matchingVoice == null && !string.IsNullOrEmpty(voiceConfig.Description))
        {
            var isMale = voiceConfig.Description.Contains("male", StringComparison.OrdinalIgnoreCase) ||
                         voiceConfig.Description.Contains("deep", StringComparison.OrdinalIgnoreCase);
            
            var isFemale = voiceConfig.Description.Contains("female", StringComparison.OrdinalIgnoreCase) ||
                           voiceConfig.Description.Contains("gentle", StringComparison.OrdinalIgnoreCase);

            if (isMale)
            {
                matchingVoice = voices.FirstOrDefault(v => v.Gender == VoiceGender.Male);
            }
            else if (isFemale)
            {
                matchingVoice = voices.FirstOrDefault(v => v.Gender == VoiceGender.Female);
            }
        }

        if (matchingVoice != null)
        {
            _synthesizer.Voice = matchingVoice;
            System.Diagnostics.Debug.WriteLine($"[TTS] Using voice: {matchingVoice.DisplayName} ({matchingVoice.Language})");
        }
    }

    /// <summary>
    /// Cleans text for better speech synthesis - removes markdown, extra whitespace, etc.
    /// </summary>
    private static string CleanTextForSpeech(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var cleaned = text;

        // Remove markdown bold/italic
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\*\*(.+?)\*\*", "$1");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\*(.+?)\*", "$1");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"__(.+?)__", "$1");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"_(.+?)_", "$1");

        // Remove markdown headers
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^#{1,6}\s*", "", System.Text.RegularExpressions.RegexOptions.Multiline);

        // Remove markdown links [text](url) -> text
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\[(.+?)\]\(.+?\)", "$1");

        // Remove code blocks
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"```[\s\S]*?```", "");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"`(.+?)`", "$1");

        // Convert bullet points to pauses
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^[\-\*]\s+", ". ", System.Text.RegularExpressions.RegexOptions.Multiline);

        // Remove emojis (using regex that matches common emoji patterns)
        // This pattern matches emoji surrogate pairs and other Unicode emoji characters
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[\uD800-\uDBFF][\uDC00-\uDFFF]|[\u2600-\u27BF]|[\uE000-\uF8FF]|[\u2300-\u23FF]|[\u2B50]|[\u231A-\u231B]|[\u23E9-\u23F3]|[\u25AA-\u25AB]|[\u25B6]|[\u25C0]|[\u25FB-\u25FE]|[\u2934-\u2935]|[\u2B05-\u2B07]|[\u2B1B-\u2B1C]|[\u3030]|[\u303D]|[\u3297]|[\u3299]", "");

        // Normalize whitespace
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");

        // Improve Bible reference reading - replace colons with natural language
        // "Psalm 23:1" becomes "Psalm 23, verse 1" (more natural than "Psalm twenty-three colon one")
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"(\d+):(\d+)", "$1, verse $2");
        // Handle verse ranges like "John 3:16-17" -> "John 3, verses 16 through 17"
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"(\d+), verse (\d+)-(\d+)", "$1, verses $2 through $3");

        return cleaned.Trim();
    }
}
