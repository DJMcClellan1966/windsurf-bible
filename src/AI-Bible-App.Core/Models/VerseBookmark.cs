namespace AI_Bible_App.Core.Models;

/// <summary>
/// Represents a bookmarked Bible verse
/// </summary>
public class VerseBookmark
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string VerseReference { get; set; } = string.Empty; // e.g., "John 3:16"
    public string VerseText { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? Category { get; set; } // e.g., "Comfort", "Strength", "Wisdom"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessedAt { get; set; }
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Repository interface for verse bookmarks
/// </summary>
public interface IVerseBookmarkRepository
{
    Task<IEnumerable<VerseBookmark>> GetAllBookmarksAsync(string userId);
    Task<IEnumerable<VerseBookmark>> GetBookmarksByCategoryAsync(string userId, string category);
    Task<IEnumerable<VerseBookmark>> SearchBookmarksAsync(string userId, string query);
    Task<VerseBookmark?> GetBookmarkAsync(string bookmarkId);
    Task AddBookmarkAsync(VerseBookmark bookmark);
    Task UpdateBookmarkAsync(VerseBookmark bookmark);
    Task DeleteBookmarkAsync(string bookmarkId);
    Task<bool> IsVerseBookmarkedAsync(string userId, string verseReference);
}
