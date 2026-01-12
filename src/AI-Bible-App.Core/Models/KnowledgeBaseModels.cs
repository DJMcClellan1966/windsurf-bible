namespace AI_Bible_App.Core.Models;

/// <summary>
/// Historical and cultural context for a specific time period or topic
/// </summary>
public class HistoricalContext
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty; // e.g., "Egyptian Bondage", "Roman Occupation"
    public string Category { get; set; } = string.Empty; // e.g., "Politics", "Culture", "Religion", "Economy"
    public string Content { get; set; } = string.Empty;
    public List<string> RelatedCharacters { get; set; } = new(); // Characters this context applies to
    public List<string> Keywords { get; set; } = new(); // For semantic search
    public string Source { get; set; } = string.Empty; // Citation/reference
    public int RelevanceWeight { get; set; } = 1; // How important this context is (1-10)
}

/// <summary>
/// Language insight for Hebrew/Greek terms
/// </summary>
public class LanguageInsight
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Word { get; set; } = string.Empty; // English word
    public string OriginalLanguage { get; set; } = string.Empty; // "Hebrew" or "Greek"
    public string Transliteration { get; set; } = string.Empty; // e.g., "shalom", "agape"
    public string StrongsNumber { get; set; } = string.Empty; // e.g., "H7965", "G26"
    public string Definition { get; set; } = string.Empty;
    public List<string> AlternateMeanings { get; set; } = new();
    public string CulturalContext { get; set; } = string.Empty;
    public List<string> ExampleVerses { get; set; } = new();
}

/// <summary>
/// Cross-reference connection between passages
/// </summary>
public class ThematicConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Theme { get; set; } = string.Empty; // e.g., "Leadership Failure and Restoration"
    public string PrimaryPassage { get; set; } = string.Empty; // e.g., "Numbers 20:1-13"
    public string SecondaryPassage { get; set; } = string.Empty; // e.g., "John 21:15-19"
    public string ConnectionType { get; set; } = string.Empty; // "Parallel", "Contrast", "Fulfillment", "Echo"
    public string Insight { get; set; } = string.Empty; // What makes this connection meaningful
    public List<string> RelatedCharacters { get; set; } = new();
}
