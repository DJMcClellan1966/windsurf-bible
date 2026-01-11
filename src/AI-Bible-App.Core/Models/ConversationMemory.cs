namespace AI_Bible_App.Core.Models;

/// <summary>
/// Tracks conversation history and themes for character memory system
/// </summary>
public class ConversationMemory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    
    /// <summary>
    /// Key topics and themes discussed with this character
    /// </summary>
    public Dictionary<string, int> ThemeFrequency { get; set; } = new();
    
    /// <summary>
    /// Recent conversation summaries (last 5-10 chats)
    /// </summary>
    public List<ConversationSummary> RecentSummaries { get; set; } = new();
    
    /// <summary>
    /// User's spiritual growth milestones noted by character
    /// </summary>
    public List<GrowthMilestone> Milestones { get; set; } = new();
    
    /// <summary>
    /// Prayer requests shared with this character
    /// </summary>
    public List<PrayerRequest> PrayerRequests { get; set; } = new();
    
    public DateTime LastInteraction { get; set; } = DateTime.UtcNow;
    public DateTime FirstInteraction { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Summary of a past conversation
/// </summary>
public class ConversationSummary
{
    public string SessionId { get; set; } = string.Empty;
    public string MainTheme { get; set; } = string.Empty;
    public string BriefSummary { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Spiritual growth milestone
/// </summary>
public class GrowthMilestone
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Description { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public DateTime AchievedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Prayer request tracking
/// </summary>
public class PrayerRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Request { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AnsweredAt { get; set; }
    public string? Answer { get; set; }
    public bool IsOngoing { get; set; } = true;
}
