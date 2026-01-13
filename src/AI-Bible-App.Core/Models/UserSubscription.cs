namespace AI_Bible_App.Core.Models;

public enum SubscriptionTier
{
    Free = 0,
    Premium = 1,
    PremiumPlus = 2,
    Enterprise = 3
}

public enum SubscriptionStatus
{
    None = 0,
    Active = 1,
    Canceled = 2,
    PastDue = 3,
    Expired = 4,
    Trial = 5
}

public class UserSubscription
{
    public string UserId { get; set; } = string.Empty;
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.None;
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public bool IsRecurring { get; set; }
    public string BillingPeriod { get; set; } = "monthly"; // monthly or yearly
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public bool IsActive => Status == SubscriptionStatus.Active || Status == SubscriptionStatus.Trial;
    public bool HasUnlimitedConversations => Tier >= SubscriptionTier.Premium;
    public int MaxUsersAllowed => Tier switch
    {
        SubscriptionTier.Free => 1,
        SubscriptionTier.Premium => 5,
        SubscriptionTier.PremiumPlus => 10,
        SubscriptionTier.Enterprise => 50,
        _ => 1
    };
}
