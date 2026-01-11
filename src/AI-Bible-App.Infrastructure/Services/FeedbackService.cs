using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// In-app feedback collection service. 
/// Stores feedback locally for review by developers.
/// </summary>
public class FeedbackService : IFeedbackService
{
    private readonly ILogger<FeedbackService> _logger;
    private readonly string _feedbackDirectory;
    private readonly string _feedbackIndexPath;

    public FeedbackService(ILogger<FeedbackService> logger)
    {
        _logger = logger;
        _feedbackDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AIBibleApp",
            "feedback");
        _feedbackIndexPath = Path.Combine(_feedbackDirectory, "feedback_index.json");
        
        EnsureDirectoryExists();
    }

    /// <summary>
    /// Submit user feedback
    /// </summary>
    public async Task<FeedbackResult> SubmitFeedbackAsync(FeedbackSubmission feedback)
    {
        try
        {
            // Generate unique ID
            feedback.Id = Guid.NewGuid().ToString("N")[..8];
            feedback.SubmittedAt = DateTime.UtcNow;
            feedback.AppVersion = GetAppVersion();

            // Save feedback to individual file
            var feedbackPath = Path.Combine(_feedbackDirectory, $"feedback_{feedback.Id}.json");
            var json = JsonSerializer.Serialize(feedback, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(feedbackPath, json);

            // Update index
            await UpdateFeedbackIndexAsync(feedback);

            _logger.LogInformation("Feedback submitted: {Id} - {Type}", feedback.Id, feedback.Type);

            return new FeedbackResult
            {
                Success = true,
                FeedbackId = feedback.Id,
                Message = "Thank you for your feedback! It helps us improve the app."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit feedback");
            return new FeedbackResult
            {
                Success = false,
                Message = "Sorry, we couldn't save your feedback. Please try again later."
            };
        }
    }

    /// <summary>
    /// Get all submitted feedback (for developers)
    /// </summary>
    public async Task<List<FeedbackSubmission>> GetAllFeedbackAsync()
    {
        var feedback = new List<FeedbackSubmission>();
        
        try
        {
            if (Directory.Exists(_feedbackDirectory))
            {
                var files = Directory.GetFiles(_feedbackDirectory, "feedback_*.json")
                    .Where(f => !f.EndsWith("feedback_index.json"));

                foreach (var file in files)
                {
                    var json = await File.ReadAllTextAsync(file);
                    var item = JsonSerializer.Deserialize<FeedbackSubmission>(json);
                    if (item != null)
                        feedback.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load feedback");
        }

        return feedback.OrderByDescending(f => f.SubmittedAt).ToList();
    }

    /// <summary>
    /// Get feedback summary statistics
    /// </summary>
    public async Task<FeedbackSummary> GetFeedbackSummaryAsync()
    {
        var allFeedback = await GetAllFeedbackAsync();
        
        return new FeedbackSummary
        {
            TotalCount = allFeedback.Count,
            AverageRating = allFeedback.Where(f => f.Rating.HasValue).Average(f => f.Rating!.Value),
            ByType = allFeedback.GroupBy(f => f.Type).ToDictionary(g => g.Key, g => g.Count()),
            ByCategory = allFeedback.GroupBy(f => f.Category ?? "Uncategorized").ToDictionary(g => g.Key, g => g.Count()),
            RecentFeedback = allFeedback.Take(5).ToList()
        };
    }

    /// <summary>
    /// Export all feedback as JSON
    /// </summary>
    public async Task<string> ExportFeedbackAsync()
    {
        var allFeedback = await GetAllFeedbackAsync();
        return JsonSerializer.Serialize(allFeedback, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Delete all feedback (user request for privacy)
    /// </summary>
    public async Task DeleteAllFeedbackAsync()
    {
        try
        {
            if (Directory.Exists(_feedbackDirectory))
            {
                var files = Directory.GetFiles(_feedbackDirectory, "feedback_*.json");
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            _logger.LogInformation("All feedback deleted by user request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete feedback");
            throw;
        }
        await Task.CompletedTask;
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_feedbackDirectory))
            Directory.CreateDirectory(_feedbackDirectory);
    }

    private async Task UpdateFeedbackIndexAsync(FeedbackSubmission feedback)
    {
        var index = new List<FeedbackIndexEntry>();
        
        if (File.Exists(_feedbackIndexPath))
        {
            var json = await File.ReadAllTextAsync(_feedbackIndexPath);
            index = JsonSerializer.Deserialize<List<FeedbackIndexEntry>>(json) ?? new();
        }

        index.Add(new FeedbackIndexEntry
        {
            Id = feedback.Id,
            Type = feedback.Type,
            SubmittedAt = feedback.SubmittedAt,
            Rating = feedback.Rating
        });

        await File.WriteAllTextAsync(_feedbackIndexPath, 
            JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true }));
    }

    private string GetAppVersion()
    {
        var assembly = typeof(FeedbackService).Assembly;
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }
}

/// <summary>
/// Feedback submission model
/// </summary>
public class FeedbackSubmission
{
    public string Id { get; set; } = "";
    public DateTime SubmittedAt { get; set; }
    public string AppVersion { get; set; } = "";
    
    /// <summary>
    /// Type: Bug, Feature, Praise, Question, Other
    /// </summary>
    public string Type { get; set; } = "General";
    
    /// <summary>
    /// Category: Characters, Prayer, Bible, UI, Performance, Other
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// User's message/feedback
    /// </summary>
    public string Message { get; set; } = "";
    
    /// <summary>
    /// Optional rating 1-5
    /// </summary>
    public int? Rating { get; set; }
    
    /// <summary>
    /// Optional contact email (if user wants response)
    /// </summary>
    public string? ContactEmail { get; set; }
    
    /// <summary>
    /// Context about what user was doing
    /// </summary>
    public string? Context { get; set; }
    
    /// <summary>
    /// Device/OS info (optional, for debugging)
    /// </summary>
    public string? DeviceInfo { get; set; }
}

/// <summary>
/// Result of feedback submission
/// </summary>
public class FeedbackResult
{
    public bool Success { get; set; }
    public string? FeedbackId { get; set; }
    public string Message { get; set; } = "";
}

/// <summary>
/// Feedback statistics summary
/// </summary>
public class FeedbackSummary
{
    public int TotalCount { get; set; }
    public double AverageRating { get; set; }
    public Dictionary<string, int> ByType { get; set; } = new();
    public Dictionary<string, int> ByCategory { get; set; } = new();
    public List<FeedbackSubmission> RecentFeedback { get; set; } = new();
}

/// <summary>
/// Lightweight index entry for quick lookups
/// </summary>
public class FeedbackIndexEntry
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime SubmittedAt { get; set; }
    public int? Rating { get; set; }
}

/// <summary>
/// Interface for feedback service
/// </summary>
public interface IFeedbackService
{
    Task<FeedbackResult> SubmitFeedbackAsync(FeedbackSubmission feedback);
    Task<List<FeedbackSubmission>> GetAllFeedbackAsync();
    Task<FeedbackSummary> GetFeedbackSummaryAsync();
    Task<string> ExportFeedbackAsync();
    Task DeleteAllFeedbackAsync();
}
