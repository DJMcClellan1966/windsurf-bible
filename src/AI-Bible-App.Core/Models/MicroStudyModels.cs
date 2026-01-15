namespace AI_Bible_App.Core.Models;

public class MicroStudyQuestion
{
    public string Question { get; set; } = string.Empty;
}

public class MicroStudySession
{
    public string PlanId { get; set; } = string.Empty;
    public int DayNumber { get; set; }
    public string DayTitle { get; set; } = string.Empty;
    public List<string> Passages { get; set; } = new();

    public string ExcerptReference { get; set; } = string.Empty;
    public string ExcerptText { get; set; } = string.Empty;

    public string Claim { get; set; } = string.Empty;

    public List<MicroStudyQuestion> Questions { get; set; } = new();

    public bool MultiVoiceEnabled { get; set; }
    public string PrimaryGuideCharacterId { get; set; } = string.Empty;
}

public class SocraticCritique
{
    public string Feedback { get; set; } = string.Empty;
    public List<string> VerseReferences { get; set; } = new();
}
