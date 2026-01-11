using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Service for indexing and quickly searching Bible verses
/// </summary>
public interface IBibleVerseIndexService
{
    Task InitializeAsync();
    Task<IEnumerable<VerseSearchResult>> SearchVersesAsync(string query, int maxResults = 20);
    Task<string?> GetVerseTextAsync(string reference);
    bool IsInitialized { get; }
}

public class VerseSearchResult
{
    public string Reference { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public double Relevance { get; set; }
}

public class BibleVerseIndexService : IBibleVerseIndexService
{
    private readonly ConcurrentDictionary<string, string> _verseIndex = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _wordIndex = new();
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await Task.Run(() =>
        {
            // This would normally load from a Bible database or API
            // For now, we'll create a basic structure that can be populated
            _isInitialized = true;
        });
    }

    public async Task<IEnumerable<VerseSearchResult>> SearchVersesAsync(string query, int maxResults = 20)
    {
        if (!_isInitialized)
            await InitializeAsync();

        return await Task.Run(() =>
        {
            var results = new List<VerseSearchResult>();
            var queryWords = NormalizeAndSplit(query);

            foreach (var kvp in _verseIndex)
            {
                var verseWords = NormalizeAndSplit(kvp.Value);
                var matchCount = queryWords.Count(qw => verseWords.Contains(qw));
                
                if (matchCount > 0)
                {
                    var relevance = (double)matchCount / queryWords.Count;
                    results.Add(new VerseSearchResult
                    {
                        Reference = kvp.Key,
                        Text = kvp.Value,
                        Relevance = relevance
                    });
                }
            }

            return results
                .OrderByDescending(r => r.Relevance)
                .Take(maxResults)
                .ToList();
        });
    }

    public async Task<string?> GetVerseTextAsync(string reference)
    {
        if (!_isInitialized)
            await InitializeAsync();

        var normalizedRef = NormalizeReference(reference);
        _verseIndex.TryGetValue(normalizedRef, out var text);
        return text;
    }

    private HashSet<string> NormalizeAndSplit(string text)
    {
        var normalized = text.ToLowerInvariant();
        var words = Regex.Split(normalized, @"\W+")
            .Where(w => w.Length > 2)
            .ToHashSet();
        return words;
    }

    private string NormalizeReference(string reference)
    {
        // Normalize verse references to a standard format
        // e.g., "John 3:16" -> "john_3_16"
        return Regex.Replace(reference.ToLowerInvariant(), @"[^\w]+", "_");
    }

    /// <summary>
    /// Add a verse to the index (for future population from Bible API)
    /// </summary>
    public void IndexVerse(string reference, string text)
    {
        var normalizedRef = NormalizeReference(reference);
        _verseIndex[normalizedRef] = text;

        // Index words for faster searching
        var words = NormalizeAndSplit(text);
        foreach (var word in words)
        {
            if (!_wordIndex.ContainsKey(word))
                _wordIndex[word] = new HashSet<string>();
            
            _wordIndex[word].Add(normalizedRef);
        }
    }
}
