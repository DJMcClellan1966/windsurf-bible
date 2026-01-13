namespace AI_Bible_App.Core.Services;

public class DailyQuotaInfo
{
    public int MessagesUsed { get; set; }
    public int MessagesLimit { get; set; }
    public int MessagesRemaining => Math.Max(0, MessagesLimit - MessagesUsed);
    public bool HasReachedLimit => MessagesUsed >= MessagesLimit;
    public DateTime ResetTime { get; set; }
}

public interface IConversationQuotaService
{
    /// <summary>
    /// Check if user can send a message based on their subscription tier and daily quota
    /// </summary>
    Task<bool> CanSendMessageAsync(string userId);

    /// <summary>
    /// Get remaining messages for the user today
    /// </summary>
    Task<DailyQuotaInfo> GetDailyQuotaAsync(string userId);

    /// <summary>
    /// Record that a message was sent by the user
    /// </summary>
    Task RecordMessageSentAsync(string userId);

    /// <summary>
    /// Check if user has unlimited conversations (Premium or higher)
    /// </summary>
    Task<bool> HasUnlimitedAsync(string userId);

    /// <summary>
    /// Reset quota for testing purposes
    /// </summary>
    Task ResetQuotaAsync(string userId);
}
