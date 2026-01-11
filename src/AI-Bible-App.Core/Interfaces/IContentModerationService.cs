namespace AI_Bible_App.Core.Interfaces;

/// <summary>
/// Service for moderating content to filter inappropriate language
/// </summary>
public interface IContentModerationService
{
    /// <summary>
    /// Check if the text contains inappropriate content
    /// </summary>
    /// <param name="text">The text to check</param>
    /// <returns>Moderation result with details</returns>
    ModerationResult CheckContent(string text);
    
    /// <summary>
    /// Sanitize text by replacing inappropriate words
    /// </summary>
    /// <param name="text">The text to sanitize</param>
    /// <returns>Sanitized text with inappropriate words replaced</returns>
    string SanitizeContent(string text);
}

/// <summary>
/// Result of content moderation check
/// </summary>
public class ModerationResult
{
    /// <summary>
    /// Whether the content passed moderation
    /// </summary>
    public bool IsAppropriate { get; set; }
    
    /// <summary>
    /// Reason for flagging (if inappropriate)
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// Categories of issues found
    /// </summary>
    public List<string> FlaggedCategories { get; set; } = new();
    
    /// <summary>
    /// Severity level (0=none, 1=mild, 2=moderate, 3=severe)
    /// </summary>
    public int Severity { get; set; }
    
    public static ModerationResult Appropriate() => new() { IsAppropriate = true };
    
    public static ModerationResult Inappropriate(string reason, int severity = 2, params string[] categories) 
        => new() 
        { 
            IsAppropriate = false, 
            Reason = reason, 
            Severity = severity,
            FlaggedCategories = categories.ToList() 
        };
}
