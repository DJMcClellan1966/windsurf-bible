using System.Text.Json;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Logging;

namespace AI_Bible_App.Core.Services;

/// <summary>
/// Service that applies onboarding profile data to personalize the user experience.
/// This includes AI prompt adjustments, homepage customization, and notification preferences.
/// </summary>
public class OnboardingProfileService
{
    private readonly ILogger<OnboardingProfileService> _logger;
    private readonly Func<string, string> _getPreference;
    private readonly Action<string, string> _setPreference;
    private OnboardingProfile? _cachedProfile;
    
    public OnboardingProfileService(
        ILogger<OnboardingProfileService> logger,
        Func<string, string>? getPreference = null,
        Action<string, string>? setPreference = null)
    {
        _logger = logger;
        _getPreference = getPreference ?? (key => string.Empty);
        _setPreference = setPreference ?? ((key, value) => { });
    }
    
    /// <summary>
    /// Load the onboarding profile from preferences (set during onboarding flow)
    /// </summary>
    public OnboardingProfile? GetProfile()
    {
        if (_cachedProfile != null)
            return _cachedProfile;
            
        try
        {
            var json = _getPreference("onboarding_profile");
            if (!string.IsNullOrEmpty(json))
            {
                _cachedProfile = JsonSerializer.Deserialize<OnboardingProfile>(json);
                return _cachedProfile;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load onboarding profile");
        }
        
        return null;
    }
    
    /// <summary>
    /// Load profile from JSON directly (useful when preferences aren't accessible)
    /// </summary>
    public void LoadProfile(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            _cachedProfile = null;
            return;
        }
        
        try
        {
            _cachedProfile = JsonSerializer.Deserialize<OnboardingProfile>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse onboarding profile JSON");
        }
    }
    
    /// <summary>
    /// Save the onboarding profile
    /// </summary>
    public void SaveProfile(OnboardingProfile profile)
    {
        try
        {
            var json = JsonSerializer.Serialize(profile);
            _setPreference("onboarding_profile", json);
            _cachedProfile = profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save onboarding profile");
        }
    }
    
    /// <summary>
    /// Clear cached profile (call on logout)
    /// </summary>
    public void ClearCache()
    {
        _cachedProfile = null;
    }
    
    /// <summary>
    /// Generate AI context based on user's faith background and Bible familiarity.
    /// This context is injected into system prompts to adjust AI tone and complexity.
    /// </summary>
    public string GenerateAIContext()
    {
        var profile = GetProfile();
        if (profile == null || !profile.IsComplete)
            return string.Empty;
            
        var contextParts = new List<string>();
        
        // Preferred name
        if (!string.IsNullOrWhiteSpace(profile.PreferredName))
        {
            contextParts.Add($"The user's name is {profile.PreferredName}. Address them by name occasionally.");
        }
        
        // Faith background - affects AI approach/tone
        var faithContext = profile.FaithBackground switch
        {
            FaithBackground.LifelongChristian => 
                "This person has been a Christian their whole life. You can engage in deeper theological discussions, reference church traditions, and assume familiarity with biblical concepts.",
            FaithBackground.ReturningToFaith => 
                "This person is returning to faith after time away. Be welcoming without judgment. Help them reconnect with what they once knew while being patient with re-learning.",
            FaithBackground.NewBeliever => 
                "This person is a new believer. Explain concepts clearly, avoid Christian jargon, and celebrate their new faith journey. Be encouraging and patient.",
            FaithBackground.Exploring => 
                "This person is exploring Christianity. Be welcoming and non-judgmental. Answer questions honestly, acknowledge doubts respectfully, and don't assume they believe everything yet.",
            FaithBackground.OtherFaith => 
                "This person comes from another faith tradition. Be respectful of their background, find common ground where possible, and explain Christian concepts without assuming prior knowledge.",
            FaithBackground.Skeptic => 
                "This person has doubts or skepticism. Engage thoughtfully with their questions, provide evidence-based responses when helpful, and don't dismiss their concerns. Meet them where they are.",
            _ => string.Empty
        };
        
        if (!string.IsNullOrEmpty(faithContext))
            contextParts.Add(faithContext);
        
        // Bible familiarity - affects complexity of responses
        var familiarityContext = profile.BibleFamiliarity switch
        {
            BibleFamiliarity.NeverRead => 
                "COMPLEXITY LEVEL: Very simple. This person has never read the Bible. Explain who biblical figures are, provide context for every reference, and avoid assuming any biblical knowledge.",
            BibleFamiliarity.Curious => 
                "COMPLEXITY LEVEL: Simple. This person has heard some Bible stories but hasn't studied it. Provide brief context for references and explain connections clearly.",
            BibleFamiliarity.Beginner => 
                "COMPLEXITY LEVEL: Accessible. This person has read some Bible passages. They know the basics but appreciate clear explanations of deeper concepts.",
            BibleFamiliarity.Intermediate => 
                "COMPLEXITY LEVEL: Standard. This person knows major Bible stories and figures. You can reference passages without lengthy explanations.",
            BibleFamiliarity.Advanced => 
                "COMPLEXITY LEVEL: Detailed. This person studies the Bible regularly. You can discuss themes, parallels between books, and theological nuances.",
            BibleFamiliarity.Scholar => 
                "COMPLEXITY LEVEL: Academic. This person has deep biblical knowledge. You can reference original languages, discuss scholarly interpretations, and engage with complex theology.",
            _ => string.Empty
        };
        
        if (!string.IsNullOrEmpty(familiarityContext))
            contextParts.Add(familiarityContext);
        
        // Goals - helps AI understand what kind of help to offer
        if (profile.Goals.Any())
        {
            var goalDescriptions = profile.Goals.Select(g => g switch
            {
                UserGoal.DeepBibleStudy => "deep Bible study and analysis",
                UserGoal.DailyDevotional => "daily spiritual encouragement",
                UserGoal.PrayerSupport => "prayer guidance and support",
                UserGoal.LifeGuidance => "applying biblical wisdom to life situations",
                UserGoal.HistoricalLearning => "understanding biblical history and context",
                UserGoal.SpiritualGrowth => "growing in their faith",
                UserGoal.TeachingOthers => "learning to teach and share with others",
                UserGoal.PersonalReflection => "personal spiritual reflection",
                UserGoal.FamilyDevotion => "family spiritual growth",
                _ => null
            }).Where(d => d != null);
            
            if (goalDescriptions.Any())
            {
                contextParts.Add($"This person is looking for: {string.Join(", ", goalDescriptions)}. Tailor your responses to help with these goals.");
            }
        }
        
        return contextParts.Any() 
            ? $"=== USER PROFILE ===\n{string.Join("\n\n", contextParts)}\n=== END PROFILE ===\n"
            : string.Empty;
    }
    
    /// <summary>
    /// Get a personalized greeting for the home page
    /// </summary>
    public string GetGreeting()
    {
        var profile = GetProfile();
        var name = profile?.PreferredName;
        var hour = DateTime.Now.Hour;
        
        var timeGreeting = hour switch
        {
            < 12 => "Good morning",
            < 17 => "Good afternoon",
            < 21 => "Good evening",
            _ => "Hello"
        };
        
        return string.IsNullOrWhiteSpace(name) 
            ? $"{timeGreeting}!" 
            : $"{timeGreeting}, {name}!";
    }
    
    /// <summary>
    /// Get recommended features based on user goals
    /// </summary>
    public List<string> GetRecommendedFeatures()
    {
        var profile = GetProfile();
        var features = new List<string>();
        
        if (profile?.Goals == null || !profile.Goals.Any())
        {
            // Default recommendations
            return new List<string> { "chat", "prayer", "bible" };
        }
        
        // Map goals to features
        foreach (var goal in profile.Goals)
        {
            var feature = goal switch
            {
                UserGoal.DeepBibleStudy => "bible",
                UserGoal.DailyDevotional => "devotional",
                UserGoal.PrayerSupport => "prayer",
                UserGoal.LifeGuidance => "chat",
                UserGoal.HistoricalLearning => "bible",
                UserGoal.SpiritualGrowth => "chat",
                UserGoal.TeachingOthers => "roundtable",
                UserGoal.PersonalReflection => "journal",
                UserGoal.FamilyDevotion => "devotional",
                _ => null
            };
            
            if (feature != null && !features.Contains(feature))
                features.Add(feature);
        }
        
        return features.Any() ? features : new List<string> { "chat", "prayer", "bible" };
    }
    
    /// <summary>
    /// Check if user should receive a notification based on their preferred frequency
    /// </summary>
    public bool ShouldNotifyToday(DateTime? lastNotification)
    {
        var profile = GetProfile();
        if (profile?.PreferredFrequency == null || lastNotification == null)
            return true;
            
        var daysSinceLastNotification = (DateTime.Now - lastNotification.Value).TotalDays;
        
        return profile.PreferredFrequency switch
        {
            EngagementFrequency.Daily => daysSinceLastNotification >= 1,
            EngagementFrequency.FewTimesWeek => daysSinceLastNotification >= 2,
            EngagementFrequency.Weekly => daysSinceLastNotification >= 7,
            EngagementFrequency.Occasionally => daysSinceLastNotification >= 14,
            EngagementFrequency.WhenNeeded => false, // Never auto-notify
            _ => daysSinceLastNotification >= 3
        };
    }
}
