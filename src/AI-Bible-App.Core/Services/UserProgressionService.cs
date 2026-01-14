using System.Text.Json;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Logging;

namespace AI_Bible_App.Core.Services;

/// <summary>
/// Tracks user spiritual growth and progression over time.
/// Adapts AI response complexity based on demonstrated understanding.
/// 
/// PROGRESSION SIGNALS:
/// 1. Conversation Depth - Are they asking deeper questions?
/// 2. Scripture Engagement - Are they referencing verses themselves?
/// 3. Theological Vocabulary - Using more advanced terms?
/// 4. Time Invested - Sessions getting longer and more substantive?
/// 5. Topic Complexity - Moving from basic to advanced topics?
/// </summary>
public class UserProgressionService
{
    private readonly ILogger<UserProgressionService> _logger;
    private readonly Func<string, string> _getPreference;
    private readonly Action<string, string> _setPreference;
    
    // Progression thresholds
    private const int ConversationsForBeginner = 10;
    private const int ConversationsForIntermediate = 50;
    private const int ConversationsForAdvanced = 150;
    private const int ConversationsForScholar = 300;
    
    public UserProgressionService(
        ILogger<UserProgressionService> logger,
        Func<string, string>? getPreference = null,
        Action<string, string>? setPreference = null)
    {
        _logger = logger;
        _getPreference = getPreference ?? (key => string.Empty);
        _setPreference = setPreference ?? ((key, value) => { });
    }
    
    /// <summary>
    /// Get the user's current progression data
    /// </summary>
    public UserProgression GetProgression(string userId)
    {
        try
        {
            var json = _getPreference($"user_progression_{userId}");
            if (!string.IsNullOrEmpty(json))
            {
                return JsonSerializer.Deserialize<UserProgression>(json) ?? new UserProgression { UserId = userId };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load progression for user {UserId}", userId);
        }
        
        return new UserProgression { UserId = userId };
    }
    
    /// <summary>
    /// Save user progression data
    /// </summary>
    public void SaveProgression(UserProgression progression)
    {
        try
        {
            var json = JsonSerializer.Serialize(progression);
            _setPreference($"user_progression_{progression.UserId}", json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save progression for user {UserId}", progression.UserId);
        }
    }
    
    /// <summary>
    /// Record a conversation and analyze for progression signals.
    /// Call this after each AI response.
    /// </summary>
    public async Task RecordConversationAsync(
        string userId, 
        string userMessage, 
        string aiResponse,
        string? characterId = null)
    {
        var progression = GetProgression(userId);
        
        // Update basic stats
        progression.TotalConversations++;
        progression.LastActiveDate = DateTime.UtcNow;
        progression.TotalWordsTyped += userMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        
        // Analyze message for progression signals
        var analysis = AnalyzeMessage(userMessage);
        
        // Update progression metrics
        if (analysis.ContainsScriptureReference)
        {
            progression.ScriptureReferencesMade++;
            progression.ProgressionPoints += 5; // Bonus for citing scripture
        }
        
        if (analysis.TheologicalDepth > 0)
        {
            progression.TheologicalTermsUsed += analysis.TheologicalDepth;
            progression.ProgressionPoints += analysis.TheologicalDepth * 2;
        }
        
        if (analysis.QuestionDepth == QuestionDepth.Deep)
        {
            progression.DeepQuestionsAsked++;
            progression.ProgressionPoints += 10;
        }
        else if (analysis.QuestionDepth == QuestionDepth.Intermediate)
        {
            progression.ProgressionPoints += 3;
        }
        
        // Track topic engagement
        if (!string.IsNullOrEmpty(analysis.DetectedTopic))
        {
            if (!progression.TopicsExplored.Contains(analysis.DetectedTopic))
            {
                progression.TopicsExplored.Add(analysis.DetectedTopic);
                progression.ProgressionPoints += 5; // Exploring new topics
            }
        }
        
        // Check for level advancement
        var previousLevel = progression.CurrentLevel;
        progression.CurrentLevel = CalculateLevel(progression);
        
        if (progression.CurrentLevel != previousLevel)
        {
            progression.LevelHistory.Add(new LevelChange
            {
                FromLevel = previousLevel,
                ToLevel = progression.CurrentLevel,
                ChangedAt = DateTime.UtcNow,
                Reason = $"Reached {progression.ProgressionPoints} progression points"
            });
            
            _logger.LogInformation("User {UserId} advanced from {From} to {To}", 
                userId, previousLevel, progression.CurrentLevel);
        }
        
        SaveProgression(progression);
    }
    
    /// <summary>
    /// Calculate the user's Bible familiarity level based on their progression
    /// </summary>
    public BibleFamiliarity CalculateLevel(UserProgression progression)
    {
        // Weight different signals
        var score = progression.ProgressionPoints;
        
        // Bonus for scripture engagement
        if (progression.ScriptureReferencesMade > 20)
            score += 50;
        else if (progression.ScriptureReferencesMade > 5)
            score += 20;
        
        // Bonus for theological vocabulary
        if (progression.TheologicalTermsUsed > 50)
            score += 50;
        else if (progression.TheologicalTermsUsed > 15)
            score += 20;
        
        // Bonus for asking deep questions
        if (progression.DeepQuestionsAsked > 20)
            score += 40;
        else if (progression.DeepQuestionsAsked > 5)
            score += 15;
        
        // Determine level based on score
        return score switch
        {
            < 50 => BibleFamiliarity.Curious,
            < 150 => BibleFamiliarity.Beginner,
            < 400 => BibleFamiliarity.Intermediate,
            < 800 => BibleFamiliarity.Advanced,
            _ => BibleFamiliarity.Scholar
        };
    }
    
    /// <summary>
    /// Analyze a user message for progression signals
    /// </summary>
    private MessageAnalysis AnalyzeMessage(string message)
    {
        var analysis = new MessageAnalysis();
        var lowerMessage = message.ToLowerInvariant();
        
        // Check for scripture references (e.g., "John 3:16", "Romans 8", "Psalm 23:1")
        var scripturePattern = @"\b\d?\s*[A-Za-z]+\s+\d+(?::\d+(?:-\d+)?)?(?:\s*-\s*\d+)?\b";
        analysis.ContainsScriptureReference = System.Text.RegularExpressions.Regex.IsMatch(message, scripturePattern);
        
        // Check for theological vocabulary (indicates growing understanding)
        var advancedTerms = new[]
        {
            // Basic theological terms (1 point each)
            "grace", "faith", "salvation", "sin", "redemption", "holy", "righteous",
            // Intermediate terms (2 points each)
            "covenant", "sanctification", "justification", "atonement", "incarnation",
            "sovereignty", "omniscient", "omnipotent", "trinity",
            // Advanced terms (3 points each)
            "eschatology", "hermeneutics", "exegesis", "soteriology", "christology",
            "pneumatology", "ecclesiology", "theodicy", "dispensation", "typology",
            "septuagint", "masoretic", "pericope", "chiasm", "theophany"
        };
        
        var basicTerms = advancedTerms.Take(7);
        var intermediateTerms = advancedTerms.Skip(7).Take(9);
        var advancedTermsList = advancedTerms.Skip(16);
        
        analysis.TheologicalDepth = 
            basicTerms.Count(t => lowerMessage.Contains(t)) * 1 +
            intermediateTerms.Count(t => lowerMessage.Contains(t)) * 2 +
            advancedTermsList.Count(t => lowerMessage.Contains(t)) * 3;
        
        // Analyze question depth
        if (message.Contains("?"))
        {
            var deepIndicators = new[]
            {
                "why does god", "how do we reconcile", "what's the relationship between",
                "theological implications", "how should we interpret", "original hebrew",
                "original greek", "context of", "historical background", "what did",
                "mean when", "significance of", "symbolism", "parallel", "foreshadow"
            };
            
            var intermediateIndicators = new[]
            {
                "what does the bible say", "how can i", "why did", "meaning of",
                "difference between", "application", "practical"
            };
            
            if (deepIndicators.Any(d => lowerMessage.Contains(d)))
                analysis.QuestionDepth = QuestionDepth.Deep;
            else if (intermediateIndicators.Any(d => lowerMessage.Contains(d)))
                analysis.QuestionDepth = QuestionDepth.Intermediate;
            else
                analysis.QuestionDepth = QuestionDepth.Basic;
        }
        
        // Detect topic
        var topicKeywords = new Dictionary<string, string[]>
        {
            ["prayer"] = new[] { "pray", "prayer", "praying" },
            ["salvation"] = new[] { "saved", "salvation", "born again", "accept christ" },
            ["suffering"] = new[] { "suffering", "pain", "struggle", "hardship", "trial" },
            ["relationships"] = new[] { "marriage", "family", "friend", "relationship", "forgive" },
            ["faith"] = new[] { "faith", "trust", "believe", "doubt" },
            ["prophecy"] = new[] { "prophecy", "revelation", "end times", "second coming" },
            ["wisdom"] = new[] { "wisdom", "proverbs", "discernment", "decision" },
            ["worship"] = new[] { "worship", "praise", "psalm", "hymn" },
            ["mission"] = new[] { "mission", "evangel", "witness", "share faith" },
            ["holy_spirit"] = new[] { "holy spirit", "spirit", "gifts", "fruit of the spirit" }
        };
        
        foreach (var (topic, keywords) in topicKeywords)
        {
            if (keywords.Any(k => lowerMessage.Contains(k)))
            {
                analysis.DetectedTopic = topic;
                break;
            }
        }
        
        return analysis;
    }
    
    /// <summary>
    /// Generate AI context based on user's progression level.
    /// This supplements the initial onboarding profile with learned progression.
    /// </summary>
    public string GenerateProgressionContext(string userId)
    {
        var progression = GetProgression(userId);
        
        if (progression.TotalConversations < 5)
            return string.Empty; // Not enough data yet
        
        var context = new List<string>();
        
        // Current level guidance
        var levelGuidance = progression.CurrentLevel switch
        {
            BibleFamiliarity.NeverRead => 
                "PROGRESSION: This user is still very new. Keep explanations simple and welcoming.",
            BibleFamiliarity.Curious => 
                "PROGRESSION: This user is in early stages. Explain concepts clearly, define biblical terms.",
            BibleFamiliarity.Beginner => 
                "PROGRESSION: This user is growing! They understand basics. You can introduce slightly deeper concepts.",
            BibleFamiliarity.Intermediate => 
                "PROGRESSION: This user has solid foundation. Engage with more theological depth, reference cross-book connections.",
            BibleFamiliarity.Advanced => 
                "PROGRESSION: This user studies seriously. Feel free to discuss nuances, original languages, scholarly perspectives.",
            BibleFamiliarity.Scholar => 
                "PROGRESSION: This user has deep knowledge. Engage at scholarly level - Greek/Hebrew, theological debates, historical context.",
            _ => string.Empty
        };
        
        if (!string.IsNullOrEmpty(levelGuidance))
            context.Add(levelGuidance);
        
        // Topic interests
        if (progression.TopicsExplored.Count > 3)
        {
            var recentTopics = progression.TopicsExplored.TakeLast(5);
            context.Add($"This user has been exploring: {string.Join(", ", recentTopics)}. Build on these interests.");
        }
        
        // Growth encouragement
        if (progression.LevelHistory.Count > 0)
        {
            var lastAdvancement = progression.LevelHistory.LastOrDefault();
            if (lastAdvancement != null && (DateTime.UtcNow - lastAdvancement.ChangedAt).TotalDays < 30)
            {
                context.Add($"This user recently advanced to {lastAdvancement.ToLevel} level - acknowledge their growth when appropriate.");
            }
        }
        
        return context.Any() 
            ? $"=== USER GROWTH CONTEXT ===\n{string.Join("\n", context)}\n=== END GROWTH ===\n"
            : string.Empty;
    }
}

/// <summary>
/// Tracks a user's spiritual growth progression
/// </summary>
public class UserProgression
{
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveDate { get; set; } = DateTime.UtcNow;
    
    // Engagement metrics
    public int TotalConversations { get; set; }
    public int TotalWordsTyped { get; set; }
    public int TotalMinutesEngaged { get; set; }
    
    // Progression signals
    public int ScriptureReferencesMade { get; set; }
    public int TheologicalTermsUsed { get; set; }
    public int DeepQuestionsAsked { get; set; }
    public int ProgressionPoints { get; set; }
    
    // Topics explored
    public List<string> TopicsExplored { get; set; } = new();
    
    // Current level (can differ from onboarding as user grows)
    public BibleFamiliarity CurrentLevel { get; set; } = BibleFamiliarity.Curious;
    
    // Level history for tracking growth
    public List<LevelChange> LevelHistory { get; set; } = new();
}

public class LevelChange
{
    public BibleFamiliarity FromLevel { get; set; }
    public BibleFamiliarity ToLevel { get; set; }
    public DateTime ChangedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class MessageAnalysis
{
    public bool ContainsScriptureReference { get; set; }
    public int TheologicalDepth { get; set; }
    public QuestionDepth QuestionDepth { get; set; }
    public string? DetectedTopic { get; set; }
}

public enum QuestionDepth
{
    Basic,
    Intermediate,
    Deep
}
