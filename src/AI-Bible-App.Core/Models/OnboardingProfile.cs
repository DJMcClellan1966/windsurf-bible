namespace AI_Bible_App.Core.Models;

/// <summary>
/// User's onboarding profile containing preferences gathered during initial setup.
/// Used to personalize the app experience.
/// </summary>
public class OnboardingProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// User's preferred name (optional)
    /// </summary>
    public string? PreferredName { get; set; }
    
    /// <summary>
    /// Age range for content personalization
    /// </summary>
    public AgeRange AgeRange { get; set; } = AgeRange.NotSpecified;
    
    /// <summary>
    /// Gender for personalization (optional)
    /// </summary>
    public Gender Gender { get; set; } = Gender.NotSpecified;
    
    /// <summary>
    /// Primary faith background
    /// </summary>
    public FaithBackground FaithBackground { get; set; } = FaithBackground.NotSpecified;
    
    /// <summary>
    /// Bible familiarity level
    /// </summary>
    public BibleFamiliarity BibleFamiliarity { get; set; } = BibleFamiliarity.Curious;
    
    /// <summary>
    /// What the user hopes to gain from the app
    /// </summary>
    public List<UserGoal> Goals { get; set; } = new();
    
    /// <summary>
    /// Preferred topics of interest
    /// </summary>
    public List<TopicInterest> Interests { get; set; } = new();
    
    /// <summary>
    /// How often they want to engage
    /// </summary>
    public EngagementFrequency PreferredFrequency { get; set; } = EngagementFrequency.FewTimesWeek;
    
    /// <summary>
    /// Whether onboarding is complete
    /// </summary>
    public bool IsComplete { get; set; }
    
    /// <summary>
    /// When onboarding was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Whether the user chose to stay logged in
    /// </summary>
    public bool StayLoggedIn { get; set; } = true;
}

public enum AgeRange
{
    NotSpecified,
    Under18,
    Age18To24,
    Age25To34,
    Age35To44,
    Age45To54,
    Age55To64,
    Age65Plus
}

public enum Gender
{
    NotSpecified,
    Male,
    Female,
    Other,
    PreferNotToSay
}

public enum FaithBackground
{
    NotSpecified,
    LifelongChristian,
    ReturningToFaith,
    NewBeliever,
    Exploring,
    OtherFaith,
    Skeptic
}

public enum BibleFamiliarity
{
    NeverRead,
    Curious,
    Beginner,
    Intermediate,
    Advanced,
    Scholar
}

public enum UserGoal
{
    DeepBibleStudy,
    DailyDevotional,
    PrayerSupport,
    LifeGuidance,
    HistoricalLearning,
    SpiritualGrowth,
    TeachingOthers,
    PersonalReflection,
    FamilyDevotion
}

public enum TopicInterest
{
    OldTestamentStories,
    NewTestament,
    Prophecy,
    Wisdom,
    Prayer,
    Faith,
    Love,
    Forgiveness,
    Suffering,
    Hope,
    Leadership,
    Family,
    Relationships,
    Purpose,
    Heaven
}

public enum EngagementFrequency
{
    Daily,
    FewTimesWeek,
    Weekly,
    Occasionally,
    WhenNeeded
}
