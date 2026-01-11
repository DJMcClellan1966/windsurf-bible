using AI_Bible_App.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AI_Bible_App.Maui.Services;

/// <summary>
/// Service for managing local notifications and daily devotional reminders
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly string _settingsPath;
    private const int DailyReminderId = 1001;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _settingsPath = Path.Combine(appData, "AI-Bible-App", "notification_settings.json");
    }

    public async Task<bool> ScheduleDailyReminderAsync(int hour, int minute, string title, string message)
    {
        try
        {
            _logger.LogInformation("Scheduling daily reminder for {Hour}:{Minute:D2}", hour, minute);

            // Cancel any existing daily reminder first
            await CancelNotificationAsync(DailyReminderId);

#if WINDOWS
            // Windows implementation using ToastNotificationManager
            await ScheduleWindowsNotificationAsync(hour, minute, title, message);
#elif ANDROID
            // Android implementation using local notifications
            await ScheduleAndroidNotificationAsync(hour, minute, title, message);
#else
            _logger.LogWarning("Notifications not implemented for this platform");
            return false;
#endif

            // Save the settings
            var settings = await GetSettingsAsync();
            settings.DailyReminderEnabled = true;
            settings.ReminderHour = hour;
            settings.ReminderMinute = minute;
            settings.ReminderTitle = title;
            settings.ReminderMessage = message;
            await SaveSettingsAsync(settings);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule daily reminder");
            return false;
        }
    }

#if WINDOWS
    private async Task ScheduleWindowsNotificationAsync(int hour, int minute, string title, string message)
    {
        try
        {
            // Use Windows Community Toolkit scheduled notifications
            // For now, we'll use a simple approach with app restart scheduling
            var nextTriggerTime = GetNextTriggerTime(hour, minute);
            
            // Store the scheduled time for app startup check
            var settings = await GetSettingsAsync();
            settings.DailyReminderEnabled = true;
            settings.ReminderHour = hour;
            settings.ReminderMinute = minute;
            await SaveSettingsAsync(settings);
            
            _logger.LogInformation("Windows notification scheduled for next trigger: {Time}", nextTriggerTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule Windows notification");
            throw;
        }
    }
#endif

#if ANDROID
    private async Task ScheduleAndroidNotificationAsync(int hour, int minute, string title, string message)
    {
        try
        {
            var nextTriggerTime = GetNextTriggerTime(hour, minute);
            
            // Use Android AlarmManager for scheduling
            // This is a simplified implementation - in production, use WorkManager or AlarmManager
            var settings = await GetSettingsAsync();
            settings.DailyReminderEnabled = true;
            settings.ReminderHour = hour;
            settings.ReminderMinute = minute;
            await SaveSettingsAsync(settings);
            
            _logger.LogInformation("Android notification scheduled for next trigger: {Time}", nextTriggerTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule Android notification");
            throw;
        }
    }
#endif

    public async Task CancelAllNotificationsAsync()
    {
        try
        {
            _logger.LogInformation("Cancelling all notifications");
            
            var settings = await GetSettingsAsync();
            settings.DailyReminderEnabled = false;
            await SaveSettingsAsync(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel all notifications");
        }
    }

    public async Task CancelNotificationAsync(int notificationId)
    {
        try
        {
            _logger.LogInformation("Cancelling notification {Id}", notificationId);
            
            if (notificationId == DailyReminderId)
            {
                var settings = await GetSettingsAsync();
                settings.DailyReminderEnabled = false;
                await SaveSettingsAsync(settings);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel notification {Id}", notificationId);
        }
    }

    public Task<bool> AreNotificationsEnabledAsync()
    {
        // On Windows, notifications are generally always available
        // On Android, we'd check notification permissions
#if WINDOWS
        return Task.FromResult(true);
#elif ANDROID
        // Check Android notification permission
        return Task.FromResult(true); // Simplified - in production, check actual permission
#else
        return Task.FromResult(false);
#endif
    }

    public async Task<bool> RequestPermissionAsync()
    {
        try
        {
#if ANDROID
            // Request Android notification permission (Android 13+)
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                }
                return status == PermissionStatus.Granted;
            }
            return true;
#else
            return await Task.FromResult(true);
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request notification permission");
            return false;
        }
    }

    public async Task<NotificationSettings> GetSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new NotificationSettings();
            }

            var json = await File.ReadAllTextAsync(_settingsPath);
            var settings = JsonSerializer.Deserialize<NotificationSettings>(json);
            return settings ?? new NotificationSettings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load notification settings");
            return new NotificationSettings();
        }
    }

    public async Task SaveSettingsAsync(NotificationSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            settings.LastModified = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsPath, json);
            
            _logger.LogInformation("Notification settings saved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save notification settings");
        }
    }

    public async Task SendNotificationAsync(string title, string message)
    {
        try
        {
            _logger.LogInformation("Sending notification: {Title}", title);

#if WINDOWS
            await SendWindowsToastAsync(title, message);
#elif ANDROID
            await SendAndroidNotificationAsync(title, message);
#else
            _logger.LogWarning("Notifications not implemented for this platform");
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification");
        }
    }

#if WINDOWS
    private Task SendWindowsToastAsync(string title, string message)
    {
        try
        {
            // Use Windows App SDK or Community Toolkit for toast notifications
            // For now, log it - actual implementation would use Microsoft.Toolkit.Uwp.Notifications
            _logger.LogInformation("Windows Toast: {Title} - {Message}", title, message);
            
            // Could use: new ToastContentBuilder().AddText(title).AddText(message).Show();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Windows toast");
            return Task.CompletedTask;
        }
    }
#endif

#if ANDROID
    private Task SendAndroidNotificationAsync(string title, string message)
    {
        try
        {
            // Use Android NotificationCompat
            _logger.LogInformation("Android Notification: {Title} - {Message}", title, message);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Android notification");
            return Task.CompletedTask;
        }
    }
#endif

    /// <summary>
    /// Calculate the next trigger time for a daily notification
    /// </summary>
    private static DateTime GetNextTriggerTime(int hour, int minute)
    {
        var now = DateTime.Now;
        var today = now.Date.AddHours(hour).AddMinutes(minute);
        
        // If the time has already passed today, schedule for tomorrow
        if (today <= now)
        {
            today = today.AddDays(1);
        }
        
        return today;
    }

    /// <summary>
    /// Check if it's time to show a notification (for app-based checking)
    /// </summary>
    public async Task CheckAndShowNotificationAsync()
    {
        try
        {
            var settings = await GetSettingsAsync();
            if (!settings.DailyReminderEnabled)
            {
                return;
            }

            var now = DateTime.Now;
            var triggerTime = now.Date.AddHours(settings.ReminderHour).AddMinutes(settings.ReminderMinute);
            
            // Check if we're within 5 minutes of the scheduled time
            var diff = Math.Abs((now - triggerTime).TotalMinutes);
            if (diff <= 5)
            {
                // Check if we've already shown today
                var lastShown = Preferences.Get("LastNotificationDate", DateTime.MinValue.ToString());
                if (DateTime.TryParse(lastShown, out var lastDate) && lastDate.Date == now.Date)
                {
                    return; // Already shown today
                }

                await SendNotificationAsync(
                    settings.ReminderTitle ?? "Daily Devotional",
                    settings.ReminderMessage ?? "Time for your daily Bible study!");
                
                Preferences.Set("LastNotificationDate", now.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for notification");
        }
    }
}
