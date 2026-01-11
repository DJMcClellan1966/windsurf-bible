namespace AI_Bible_App.Core.Interfaces;

/// <summary>
/// Service for managing push notifications and reminders
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Schedule a daily devotional reminder notification
    /// </summary>
    /// <param name="hour">Hour in 24-hour format (0-23)</param>
    /// <param name="minute">Minute (0-59)</param>
    /// <param name="title">Notification title</param>
    /// <param name="message">Notification message</param>
    /// <returns>True if scheduling succeeded</returns>
    Task<bool> ScheduleDailyReminderAsync(int hour, int minute, string title, string message);

    /// <summary>
    /// Cancel all scheduled notifications
    /// </summary>
    Task CancelAllNotificationsAsync();

    /// <summary>
    /// Cancel a specific notification by ID
    /// </summary>
    /// <param name="notificationId">The notification ID to cancel</param>
    Task CancelNotificationAsync(int notificationId);

    /// <summary>
    /// Check if notifications are enabled and have permission
    /// </summary>
    /// <returns>True if notifications can be sent</returns>
    Task<bool> AreNotificationsEnabledAsync();

    /// <summary>
    /// Request notification permission from the user
    /// </summary>
    /// <returns>True if permission was granted</returns>
    Task<bool> RequestPermissionAsync();

    /// <summary>
    /// Get the current notification settings
    /// </summary>
    Task<NotificationSettings> GetSettingsAsync();

    /// <summary>
    /// Save notification settings
    /// </summary>
    Task SaveSettingsAsync(NotificationSettings settings);

    /// <summary>
    /// Send an immediate notification (for testing or alerts)
    /// </summary>
    Task SendNotificationAsync(string title, string message);
}

/// <summary>
/// Notification settings configuration
/// </summary>
public class NotificationSettings
{
    /// <summary>
    /// Whether daily devotional reminders are enabled
    /// </summary>
    public bool DailyReminderEnabled { get; set; }

    /// <summary>
    /// Hour for daily reminder (0-23)
    /// </summary>
    public int ReminderHour { get; set; } = 8;

    /// <summary>
    /// Minute for daily reminder (0-59)
    /// </summary>
    public int ReminderMinute { get; set; } = 0;

    /// <summary>
    /// Custom notification title
    /// </summary>
    public string? ReminderTitle { get; set; } = "Daily Devotional";

    /// <summary>
    /// Custom notification message
    /// </summary>
    public string? ReminderMessage { get; set; } = "Time for your daily Bible study and devotional!";

    /// <summary>
    /// Whether to include a verse of the day in the notification
    /// </summary>
    public bool IncludeVerseOfDay { get; set; } = true;

    /// <summary>
    /// Last time settings were modified
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}
