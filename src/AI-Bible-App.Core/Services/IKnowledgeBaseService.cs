namespace AI_Bible_App.Core.Services;

/// <summary>
/// Service for retrieving historical/cultural context and language insights
/// </summary>
public interface IKnowledgeBaseService
{
    /// <summary>
    /// Get relevant historical context for a character and topic
    /// </summary>
    Task<List<HistoricalContext>> GetHistoricalContextAsync(
        string characterId,
        string userQuestion,
        int maxResults = 3);
    
    /// <summary>
    /// Get language insights for key terms in a passage
    /// </summary>
    Task<List<LanguageInsight>> GetLanguageInsightsAsync(
        string passage,
        int maxResults = 5);
    
    /// <summary>
    /// Find thematic connections between passages
    /// </summary>
    Task<List<ThematicConnection>> FindThematicConnectionsAsync(
        string passage,
        string theme,
        int maxResults = 3);
    
    /// <summary>
    /// Initialize/load the knowledge base data
    /// </summary>
    Task InitializeAsync();
}
