using AI_Bible_App.Core.Models;
using System.Text.Json;

namespace AI_Bible_App.Infrastructure.Repositories;

public class VerseBookmarkRepository : IVerseBookmarkRepository
{
    private readonly string _bookmarksFilePath;

    public VerseBookmarkRepository()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "VoicesOfScripture");
        Directory.CreateDirectory(appFolder);
        _bookmarksFilePath = Path.Combine(appFolder, "verse_bookmarks.json");
    }

    public async Task<IEnumerable<VerseBookmark>> GetAllBookmarksAsync(string userId)
    {
        var allBookmarks = await LoadBookmarksAsync();
        return allBookmarks
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToList();
    }

    public async Task<IEnumerable<VerseBookmark>> GetBookmarksByCategoryAsync(string userId, string category)
    {
        var allBookmarks = await LoadBookmarksAsync();
        return allBookmarks
            .Where(b => b.UserId == userId && b.Category == category)
            .OrderByDescending(b => b.CreatedAt)
            .ToList();
    }

    public async Task<IEnumerable<VerseBookmark>> SearchBookmarksAsync(string userId, string query)
    {
        var allBookmarks = await LoadBookmarksAsync();
        var searchLower = query.ToLowerInvariant();
        
        return allBookmarks
            .Where(b => b.UserId == userId && (
                b.VerseReference.ToLowerInvariant().Contains(searchLower) ||
                b.VerseText.ToLowerInvariant().Contains(searchLower) ||
                (b.Note != null && b.Note.ToLowerInvariant().Contains(searchLower)) ||
                b.Tags.Any(t => t.ToLowerInvariant().Contains(searchLower))
            ))
            .OrderByDescending(b => b.CreatedAt)
            .ToList();
    }

    public async Task<VerseBookmark?> GetBookmarkAsync(string bookmarkId)
    {
        var allBookmarks = await LoadBookmarksAsync();
        return allBookmarks.FirstOrDefault(b => b.Id == bookmarkId);
    }

    public async Task AddBookmarkAsync(VerseBookmark bookmark)
    {
        var allBookmarks = await LoadBookmarksAsync();
        allBookmarks.Add(bookmark);
        await SaveBookmarksAsync(allBookmarks);
    }

    public async Task UpdateBookmarkAsync(VerseBookmark bookmark)
    {
        var allBookmarks = await LoadBookmarksAsync();
        var index = allBookmarks.FindIndex(b => b.Id == bookmark.Id);
        if (index >= 0)
        {
            allBookmarks[index] = bookmark;
            await SaveBookmarksAsync(allBookmarks);
        }
    }

    public async Task DeleteBookmarkAsync(string bookmarkId)
    {
        var allBookmarks = await LoadBookmarksAsync();
        allBookmarks.RemoveAll(b => b.Id == bookmarkId);
        await SaveBookmarksAsync(allBookmarks);
    }

    public async Task<bool> IsVerseBookmarkedAsync(string userId, string verseReference)
    {
        var allBookmarks = await LoadBookmarksAsync();
        return allBookmarks.Any(b => b.UserId == userId && 
            string.Equals(b.VerseReference, verseReference, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<VerseBookmark>> LoadBookmarksAsync()
    {
        if (!File.Exists(_bookmarksFilePath))
            return new List<VerseBookmark>();

        try
        {
            var json = await File.ReadAllTextAsync(_bookmarksFilePath);
            return JsonSerializer.Deserialize<List<VerseBookmark>>(json) ?? new List<VerseBookmark>();
        }
        catch
        {
            return new List<VerseBookmark>();
        }
    }

    private async Task SaveBookmarksAsync(List<VerseBookmark> bookmarks)
    {
        var json = JsonSerializer.Serialize(bookmarks, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_bookmarksFilePath, json);
    }
}
