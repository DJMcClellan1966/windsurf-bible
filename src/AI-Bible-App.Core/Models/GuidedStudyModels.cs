namespace AI_Bible_App.Core.Models;

public enum GuidedStudyStepType
{
    Passage,
    Background,
    Outline,
    Insights,
    Questions,
    Application
}

public class GuidedStudyStep
{
    public GuidedStudyStepType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? CharacterId { get; set; }
    public string? CharacterName { get; set; }
}

public class GuidedStudySession
{
    public string PlanId { get; set; } = string.Empty;
    public int DayNumber { get; set; }
    public string DayTitle { get; set; } = string.Empty;
    public List<string> Passages { get; set; } = new();
    public string PassageText { get; set; } = string.Empty;
    public bool MultiVoiceEnabled { get; set; }
    public string PrimaryGuideCharacterId { get; set; } = string.Empty;
    public List<string> AdditionalGuideCharacterIds { get; set; } = new();
    public List<GuidedStudyStep> Steps { get; set; } = new();
}
