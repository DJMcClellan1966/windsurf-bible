namespace AI_Bible_App.Core.Models;

/// <summary>
/// Represents a daily devotional message
/// </summary>
public class Devotional
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Scripture { get; set; } = string.Empty;
    public string ScriptureReference { get; set; } = string.Empty;
    public string Prayer { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Category { get; set; } = string.Empty; // Faith, Hope, Love, Wisdom, etc.
    public bool IsRead { get; set; }
}

/// <summary>
/// Repository interface for devotionals
/// </summary>
public interface IDevotionalRepository
{
    Task<Devotional?> GetDevotionalForDateAsync(DateTime date);
    Task<IEnumerable<Devotional>> GetRecentDevotionalsAsync(int count = 7);
    Task MarkDevotionalAsReadAsync(string devotionalId);
    Task<Devotional> GenerateDevotionalAsync(DateTime date);
}
