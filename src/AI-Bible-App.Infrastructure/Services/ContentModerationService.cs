using AI_Bible_App.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Local content moderation service using word lists and patterns.
/// Provides offline-capable filtering for inappropriate language.
/// </summary>
public class ContentModerationService : IContentModerationService
{
    private readonly ILogger<ContentModerationService> _logger;
    private readonly HashSet<string> _profanityList;
    private readonly HashSet<string> _mildProfanityList;
    private readonly List<Regex> _patterns;
    private readonly char[] _replacementChars = { '*', '#', '@', '!' };

    public ContentModerationService(ILogger<ContentModerationService> logger)
    {
        _logger = logger;
        _profanityList = LoadProfanityList();
        _mildProfanityList = LoadMildProfanityList();
        _patterns = LoadPatterns();
    }

    public ModerationResult CheckContent(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ModerationResult.Appropriate();

        var lowerText = text.ToLowerInvariant();
        var words = ExtractWords(lowerText);
        var flaggedCategories = new List<string>();
        var highestSeverity = 0;
        
        // Check for severe profanity
        foreach (var word in words)
        {
            if (_profanityList.Contains(word))
            {
                flaggedCategories.Add("profanity");
                highestSeverity = Math.Max(highestSeverity, 3);
                _logger.LogWarning("Severe profanity detected in content");
                break;
            }
        }
        
        // Check for mild profanity
        if (highestSeverity < 3)
        {
            foreach (var word in words)
            {
                if (_mildProfanityList.Contains(word))
                {
                    flaggedCategories.Add("mild_language");
                    highestSeverity = Math.Max(highestSeverity, 1);
                    break;
                }
            }
        }
        
        // Check patterns (hate speech, threats, etc.)
        foreach (var pattern in _patterns)
        {
            if (pattern.IsMatch(lowerText))
            {
                flaggedCategories.Add("harmful_pattern");
                highestSeverity = Math.Max(highestSeverity, 2);
                _logger.LogWarning("Harmful pattern detected in content");
                break;
            }
        }
        
        if (highestSeverity > 1)
        {
            return ModerationResult.Inappropriate(
                GetModerationMessage(highestSeverity),
                highestSeverity,
                flaggedCategories.Distinct().ToArray());
        }

        return ModerationResult.Appropriate();
    }

    public string SanitizeContent(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var result = text;
        
        // Replace profanity with asterisks
        foreach (var word in _profanityList.Concat(_mildProfanityList))
        {
            var pattern = new Regex($@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase);
            result = pattern.Replace(result, match => MaskWord(match.Value));
        }

        return result;
    }

    private string MaskWord(string word)
    {
        if (word.Length <= 2)
            return new string('*', word.Length);
        
        // Keep first and last character, mask the middle
        return word[0] + new string('*', word.Length - 2) + word[^1];
    }

    private static string GetModerationMessage(int severity) => severity switch
    {
        3 => "Your message contains language that isn't appropriate for this app. Please rephrase with respect.",
        2 => "Your message may contain inappropriate content. Please consider rephrasing.",
        _ => "Please keep conversation respectful and appropriate."
    };

    private static string[] ExtractWords(string text)
    {
        return Regex.Split(text.ToLowerInvariant(), @"[^a-z0-9]+")
            .Where(w => !string.IsNullOrEmpty(w))
            .ToArray();
    }

    private static HashSet<string> LoadProfanityList()
    {
        // Severe profanity list - these will be blocked
        // Note: This is a minimal representative list. In production, 
        // consider using a comprehensive external word list.
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Common severe English profanity (abbreviated for code review)
            "fuck", "fucking", "fucked", "fucker",
            "shit", "shitting", "shitty",
            "ass", "asshole", "arse",
            "bitch", "bitching",
            "damn", "damned", "damnit",
            "bastard",
            "crap", "crappy",
            "piss", "pissed",
            "cunt",
            "dick", "dickhead",
            "cock",
            "whore",
            "slut",
            
            // Slurs and hate speech (critical to block)
            "nigger", "nigga",
            "faggot", "fag",
            "retard", "retarded",
            "kike",
            "spic",
            "chink",
            "wetback",
            "dyke",
            
            // Religious disrespect in context of Bible app
            "goddamn", "goddamnit"
        };
    }

    private static HashSet<string> LoadMildProfanityList()
    {
        // Mild language that may be flagged but not blocked
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hell", "heck",
            "darn", "dang",
            "crap",
            "suck", "sucks",
            "stupid", "idiot", "moron",
            "jerk",
            "butt"
        };
    }

    private static List<Regex> LoadPatterns()
    {
        return new List<Regex>
        {
            // Threat patterns
            new(@"\b(kill|murder|hurt|harm|attack)\s+(you|him|her|them|someone|people)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            
            // Self-harm indicators
            new(@"\b(want\s+to\s+die|kill\s+myself|suicide|end\s+my\s+life)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            
            // Hate speech patterns  
            new(@"\b(hate\s+(all|every)\s+\w+|death\s+to\s+\w+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            
            // Sexual content (explicit)
            new(@"\b(sex|sexual|porn|nude|naked)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            
            // Leetspeak evasion for common words
            new(@"\bf[u\*@][ck\*@]+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"\bs[h\*@][i1\*@]t\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        };
    }
}
