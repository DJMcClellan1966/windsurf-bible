namespace AI_Bible_App.Core.Services;

/// <summary>
/// Tracks character usage to prioritize research
/// </summary>
public interface ICharacterUsageTracker
{
    /// <summary>
    /// Record a conversation with a character
    /// </summary>
    Task RecordConversationAsync(string characterId, int messageCount, TimeSpan duration);
    
    /// <summary>
    /// Get most popular characters (by usage)
    /// </summary>
    Task<List<CharacterUsageStats>> GetTopCharactersAsync(int count = 10);
    
    /// <summary>
    /// Get usage statistics for a specific character
    /// </summary>
    Task<CharacterUsageStats> GetCharacterStatsAsync(string characterId);
    
    /// <summary>
    /// Get characters that need research (popular but low KB data)
    /// </summary>
    Task<List<string>> GetCharactersNeedingResearchAsync();
}

/// <summary>
/// Usage statistics for a character
/// </summary>
public class CharacterUsageStats
{
    public string CharacterId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public int TotalConversations { get; set; }
    public int TotalMessages { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public DateTime LastUsed { get; set; }
    public DateTime FirstUsed { get; set; }
    public double PopularityScore { get; set; }  // Weighted by recency and frequency
    public int KnowledgeBaseEntries { get; set; }
    public bool NeedsResearch { get; set; }  // Popular but low KB data
}
