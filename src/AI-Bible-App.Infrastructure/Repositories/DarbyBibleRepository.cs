using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AI_Bible_App.Infrastructure.Repositories;

/// <summary>
/// Repository for loading Darby Translation verses from JSON file
/// </summary>
public class DarbyBibleRepository : IBibleRepository
{
    private readonly ILogger<DarbyBibleRepository> _logger;
    private readonly string _dataPath;
    private List<BibleVerse>? _cachedVerses;

    public DarbyBibleRepository(IConfiguration configuration, ILogger<DarbyBibleRepository> logger)
    {
        _logger = logger;
        _dataPath = configuration["Bible:DarbyDataPath"] ?? "Data/Bible/darby.json";
    }

    public async Task<List<BibleVerse>> LoadAllVersesAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedVerses != null)
        {
            return _cachedVerses;
        }

        try
        {
            if (!File.Exists(_dataPath))
            {
                _logger.LogWarning("Darby Bible data file not found at {Path}", _dataPath);
                return new List<BibleVerse>();
            }

            var json = await File.ReadAllTextAsync(_dataPath, cancellationToken);
            _cachedVerses = JsonSerializer.Deserialize<List<BibleVerse>>(json) ?? new List<BibleVerse>();

            _logger.LogInformation("Loaded {Count} verses from Darby Translation", _cachedVerses.Count);
            return _cachedVerses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Darby Translation verses from {Path}", _dataPath);
            throw;
        }
    }

    public async Task<List<BibleVerse>> GetVersesAsync(
        string book,
        int chapter,
        int? startVerse = null,
        int? endVerse = null,
        CancellationToken cancellationToken = default)
    {
        var allVerses = await LoadAllVersesAsync(cancellationToken);

        var query = allVerses
            .Where(v => v.Book.Equals(book, StringComparison.OrdinalIgnoreCase) && v.Chapter == chapter);

        if (startVerse.HasValue)
        {
            query = query.Where(v => v.Verse >= startVerse.Value);
        }

        if (endVerse.HasValue)
        {
            query = query.Where(v => v.Verse <= endVerse.Value);
        }

        return query.OrderBy(v => v.Verse).ToList();
    }

    public async Task<List<BibleVerse>> SearchVersesAsync(
        string searchText,
        CancellationToken cancellationToken = default)
    {
        var allVerses = await LoadAllVersesAsync(cancellationToken);

        return allVerses
            .Where(v => v.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
