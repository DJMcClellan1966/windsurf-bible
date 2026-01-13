using AI_Bible_App.Core.Services;
using AI_Bible_App.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AI_Bible_App.Infrastructure.Services;

public class ConversationQuotaService : IConversationQuotaService
{
    private readonly ILogger<ConversationQuotaService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly string _quotaFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private const int FREE_TIER_DAILY_LIMIT = 10;

    public ConversationQuotaService(
        ILogger<ConversationQuotaService> logger,
        IUserRepository userRepository)
    {
        _logger = logger;
        _userRepository = userRepository;
        
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var quotaDir = Path.Combine(appDataPath, "AIBibleApp", "Quota");
        Directory.CreateDirectory(quotaDir);
        _quotaFilePath = Path.Combine(quotaDir, "daily-quota.json");
    }

    public async Task<bool> CanSendMessageAsync(string userId)
    {
        // Check if user has unlimited (Premium or higher)
        if (await HasUnlimitedAsync(userId))
        {
            return true;
        }

        var quota = await GetDailyQuotaAsync(userId);
        return !quota.HasReachedLimit;
    }

    public async Task<DailyQuotaInfo> GetDailyQuotaAsync(string userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user?.Subscription?.HasUnlimitedConversations == true)
        {
            return new DailyQuotaInfo
            {
                MessagesUsed = 0,
                MessagesLimit = int.MaxValue,
                ResetTime = DateTime.Today.AddDays(1)
            };
        }

        var usage = await GetTodayUsageAsync(userId);
        var now = DateTime.Now;
        var resetTime = DateTime.Today.AddDays(1); // Midnight tonight

        return new DailyQuotaInfo
        {
            MessagesUsed = usage,
            MessagesLimit = FREE_TIER_DAILY_LIMIT,
            ResetTime = resetTime
        };
    }

    public async Task RecordMessageSentAsync(string userId)
    {
        await _lock.WaitAsync();
        try
        {
            var quotaData = await LoadQuotaDataAsync();
            var today = DateTime.Today.ToString("yyyy-MM-dd");

            if (!quotaData.ContainsKey(userId))
            {
                quotaData[userId] = new Dictionary<string, int>();
            }

            if (!quotaData[userId].ContainsKey(today))
            {
                quotaData[userId][today] = 0;
            }

            quotaData[userId][today]++;

            // Clean up old dates (keep only last 7 days)
            CleanupOldDates(quotaData);

            await SaveQuotaDataAsync(quotaData);

            _logger.LogInformation("Recorded message for user {UserId}. Today's count: {Count}", 
                userId, quotaData[userId][today]);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> HasUnlimitedAsync(string userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        return user?.Subscription?.HasUnlimitedConversations == true;
    }

    public async Task ResetQuotaAsync(string userId)
    {
        await _lock.WaitAsync();
        try
        {
            var quotaData = await LoadQuotaDataAsync();
            if (quotaData.ContainsKey(userId))
            {
                quotaData.Remove(userId);
                await SaveQuotaDataAsync(quotaData);
                _logger.LogInformation("Reset quota for user {UserId}", userId);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<int> GetTodayUsageAsync(string userId)
    {
        var quotaData = await LoadQuotaDataAsync();
        var today = DateTime.Today.ToString("yyyy-MM-dd");

        if (quotaData.TryGetValue(userId, out var userQuota))
        {
            if (userQuota.TryGetValue(today, out var count))
            {
                return count;
            }
        }

        return 0;
    }

    private async Task<Dictionary<string, Dictionary<string, int>>> LoadQuotaDataAsync()
    {
        if (!File.Exists(_quotaFilePath))
        {
            return new Dictionary<string, Dictionary<string, int>>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_quotaFilePath);
            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(json) 
                   ?? new Dictionary<string, Dictionary<string, int>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading quota data");
            return new Dictionary<string, Dictionary<string, int>>();
        }
    }

    private async Task SaveQuotaDataAsync(Dictionary<string, Dictionary<string, int>> data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(_quotaFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving quota data");
        }
    }

    private void CleanupOldDates(Dictionary<string, Dictionary<string, int>> quotaData)
    {
        var cutoffDate = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");

        foreach (var userQuota in quotaData.Values)
        {
            var oldDates = userQuota.Keys.Where(date => string.Compare(date, cutoffDate) < 0).ToList();
            foreach (var oldDate in oldDates)
            {
                userQuota.Remove(oldDate);
            }
        }
    }
}
