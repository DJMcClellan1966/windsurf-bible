using AI_Bible_App.Core.Models;

namespace AI_Bible_App.Core.Services;

/// <summary>
/// Service for autonomous character research from web sources
/// </summary>
public interface ICharacterResearchService
{
    /// <summary>
    /// Start autonomous research for a character
    /// </summary>
    Task<ResearchSession> StartResearchAsync(string characterId, ResearchPriority priority = ResearchPriority.Normal);
    
    /// <summary>
    /// Get current research status
    /// </summary>
    Task<List<ResearchSession>> GetActiveResearchAsync();
    
    /// <summary>
    /// Get pending findings awaiting review
    /// </summary>
    Task<List<ResearchFinding>> GetPendingFindingsAsync(string? characterId = null);
    
    /// <summary>
    /// Approve a research finding for integration
    /// </summary>
    Task<bool> ApproveFindingAsync(string findingId);
    
    /// <summary>
    /// Reject a research finding
    /// </summary>
    Task<bool> RejectFindingAsync(string findingId, string reason);
    
    /// <summary>
    /// Get research statistics
    /// </summary>
    Task<ResearchStatistics> GetStatisticsAsync();
    
    /// <summary>
    /// Enable/disable autonomous research
    /// </summary>
    Task SetResearchEnabledAsync(bool enabled);
    
    /// <summary>
    /// Set research schedule (off-peak hours)
    /// </summary>
    Task SetResearchScheduleAsync(TimeSpan startTime, TimeSpan endTime);
}

/// <summary>
/// Research priority level
/// </summary>
public enum ResearchPriority
{
    Low,
    Normal,
    High,
    Urgent
}

/// <summary>
/// Ongoing research session
/// </summary>
public class ResearchSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CharacterId { get; set; } = string.Empty;
    public ResearchPriority Priority { get; set; }
    public DateTime StartedAt { get; set; }
    public ResearchStatus Status { get; set; }
    public int TopicsResearched { get; set; }
    public int FindingsCollected { get; set; }
    public int FindingsValidated { get; set; }
    public int FindingsApproved { get; set; }
    public List<string> CurrentTopics { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Research session status
/// </summary>
public enum ResearchStatus
{
    Queued,
    Scraping,
    Validating,
    AwaitingReview,
    Integrating,
    Completed,
    Failed
}

/// <summary>
/// A research finding from web sources
/// </summary>
public class ResearchFinding
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CharacterId { get; set; } = string.Empty;
    public FindingType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Sources { get; set; } = new();
    public ConfidenceLevel Confidence { get; set; }
    public bool RequiresReview { get; set; }
    public ReviewStatus ReviewStatus { get; set; }
    public DateTime DiscoveredAt { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public List<string> RelatedCharacters { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
}

/// <summary>
/// Type of research finding
/// </summary>
public enum FindingType
{
    HistoricalContext,
    CulturalInsight,
    LanguageInsight,
    Archaeological,
    Geographical,
    ThematicConnection
}

/// <summary>
/// Confidence level in finding accuracy
/// </summary>
public enum ConfidenceLevel
{
    Low,        // Single source or Tier 3
    Medium,     // Multiple Tier 2 sources
    High,       // Multiple Tier 1 sources
    VeryHigh    // 3+ Tier 1 sources + cross-validation
}

/// <summary>
/// Review status for findings
/// </summary>
public enum ReviewStatus
{
    Pending,
    Approved,
    Rejected,
    AutoApproved  // High confidence, no review needed
}

/// <summary>
/// Research statistics and metrics
/// </summary>
public class ResearchStatistics
{
    public bool ResearchEnabled { get; set; }
    public int TotalCharactersResearched { get; set; }
    public int TotalFindingsCollected { get; set; }
    public int TotalFindingsApproved { get; set; }
    public int TotalFindingsRejected { get; set; }
    public int PendingReviews { get; set; }
    public DateTime? LastResearchRun { get; set; }
    public DateTime? NextScheduledRun { get; set; }
    public Dictionary<string, int> FindingsByCharacter { get; set; } = new();
    public Dictionary<string, int> FindingsByType { get; set; } = new();
    public Dictionary<ConfidenceLevel, int> FindingsByConfidence { get; set; } = new();
    public TimeSpan AverageResearchTime { get; set; }
    public double ApprovalRate { get; set; }
}
