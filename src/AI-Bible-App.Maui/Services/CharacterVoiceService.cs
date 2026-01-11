using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;

namespace AI_Bible_App.Maui.Services;

/// <summary>
/// Text-to-speech service for reading character responses with personalized voice settings
/// </summary>
public class CharacterVoiceService : ICharacterVoiceService
{
    private readonly ITextToSpeech _textToSpeech;
    private CancellationTokenSource? _currentSpeechCts;
    private bool _isSpeaking;

    public bool IsSpeaking => _isSpeaking;

    public CharacterVoiceService(ITextToSpeech textToSpeech)
    {
        _textToSpeech = textToSpeech;
    }

    public async Task SpeakAsync(string text, VoiceConfig voiceConfig, CancellationToken cancellationToken = default)
    {
        // Stop any ongoing speech
        await StopSpeakingAsync();

        // Clean the text - remove markdown formatting and emojis for cleaner speech
        var cleanedText = CleanTextForSpeech(text);
        
        if (string.IsNullOrWhiteSpace(cleanedText))
            return;

        _currentSpeechCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _isSpeaking = true;

        try
        {
            var options = new SpeechOptions
            {
                Pitch = voiceConfig.Pitch,
                Volume = voiceConfig.Volume
            };

            // Try to find a matching locale voice
            var locales = await _textToSpeech.GetLocalesAsync();
            var matchingLocale = locales.FirstOrDefault(l => 
                l.Language.StartsWith(voiceConfig.Locale.Split('-')[0], StringComparison.OrdinalIgnoreCase));
            
            if (matchingLocale != null)
            {
                options.Locale = matchingLocale;
            }

            await _textToSpeech.SpeakAsync(cleanedText, options, _currentSpeechCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Speech was cancelled - this is expected
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TTS Error: {ex.Message}");
        }
        finally
        {
            _isSpeaking = false;
            _currentSpeechCts?.Dispose();
            _currentSpeechCts = null;
        }
    }

    public Task StopSpeakingAsync()
    {
        if (_currentSpeechCts != null && !_currentSpeechCts.IsCancellationRequested)
        {
            _currentSpeechCts.Cancel();
        }
        _isSpeaking = false;
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<string>> GetAvailableLocalesAsync()
    {
        var locales = await _textToSpeech.GetLocalesAsync();
        return locales.Select(l => $"{l.Language}-{l.Country}").Distinct();
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
