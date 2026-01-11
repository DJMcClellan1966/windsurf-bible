namespace AI_Bible_App.Core.Models;

/// <summary>
/// Represents an interactive biblical story for experiential learning
/// </summary>
public class BiblicalStory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Biblical reference (e.g., "John 6:16-21")
    /// </summary>
    public string Reference { get; set; } = string.Empty;
    
    /// <summary>
    /// Character ID who narrates/participates in this story
    /// </summary>
    public string CharacterId { get; set; } = string.Empty;
    
    /// <summary>
    /// Story scenes/moments for interactive participation
    /// </summary>
    public List<StoryScene> Scenes { get; set; } = new();
    
    /// <summary>
    /// Key themes and lessons in this story
    /// </summary>
    public List<string> Themes { get; set; } = new();
    
    /// <summary>
    /// Difficulty level (beginner, intermediate, advanced)
    /// </summary>
    public string DifficultyLevel { get; set; } = "beginner";
    
    /// <summary>
    /// Estimated time to complete (in minutes)
    /// </summary>
    public int EstimatedMinutes { get; set; } = 10;
}

/// <summary>
/// A scene within an interactive biblical story
/// </summary>
public class StoryScene
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Setting { get; set; } = string.Empty;
    
    /// <summary>
    /// Narrative description from character's perspective
    /// </summary>
    public string Narrative { get; set; } = string.Empty;
    
    /// <summary>
    /// Suggested questions user can ask during this scene
    /// </summary>
    public List<string> SuggestedQuestions { get; set; } = new();
    
    /// <summary>
    /// Key moment or lesson in this scene
    /// </summary>
    public string KeyMoment { get; set; } = string.Empty;
}

/// <summary>
/// Wisdom Council request for multi-character guidance
/// </summary>
public class WisdomCouncilRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Responses from each character
    /// </summary>
    public Dictionary<string, WisdomResponse> Responses { get; set; } = new();
}

/// <summary>
/// A character's response in the Wisdom Council
/// </summary>
public class WisdomResponse
{
    public string CharacterId { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
    public EmotionalTone Tone { get; set; } = EmotionalTone.Balanced;
}

/// <summary>
/// Prayer Chain request for multiple character prayers
/// </summary>
public class PrayerChainRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Request { get; set; } = string.Empty;
    public List<string> ParticipatingCharacterIds { get; set; } = new();
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Prayers from each character
    /// </summary>
    public Dictionary<string, CharacterPrayer> Prayers { get; set; } = new();
}

/// <summary>
/// A character's prayer in the Prayer Chain
/// </summary>
public class CharacterPrayer
{
    public string CharacterId { get; set; } = string.Empty;
    public string Prayer { get; set; } = string.Empty;
    public PrayerStyle Style { get; set; } = PrayerStyle.Traditional;
    public DateTime PrayedAt { get; set; } = DateTime.UtcNow;
}
