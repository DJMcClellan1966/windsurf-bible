using System.Text.Json;
using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AI_Bible_App.Infrastructure.Repositories;

/// <summary>
/// Repository for loading Bible verses from multiple translation JSON files
/// </summary>
public class MultiBibleRepository : IBibleRepository
{
    private readonly ILogger<MultiBibleRepository> _logger;
    private readonly string _bibleDataDirectory;
    private readonly List<string> _enabledTranslations;
    private List<BibleVerse>? _cachedVerses;

    public MultiBibleRepository(IConfiguration configuration, ILogger<MultiBibleRepository> logger)
    {
        _logger = logger;
        _bibleDataDirectory = configuration["Bible:DataDirectory"] ?? Path.Combine("Data", "Bible");
        
        // Get enabled translations from config, default to all available
        var translations = configuration["Bible:Translations"];
        _enabledTranslations = string.IsNullOrEmpty(translations) 
            ? new List<string> { "web", "asv", "ylt" }
            : translations.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim().ToLower()).ToList();
        
        _logger.LogInformation("MultiBibleRepository initialized with directory: {Dir}, Translations: {Translations}", 
            _bibleDataDirectory, string.Join(", ", _enabledTranslations));
    }

    public async Task<List<BibleVerse>> LoadAllVersesAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedVerses != null)
        {
            _logger.LogDebug("Returning cached Bible verses ({Count} verses)", _cachedVerses.Count);
            return _cachedVerses;
        }

        var allVerses = new List<BibleVerse>();

        try
        {
            if (!Directory.Exists(_bibleDataDirectory))
            {
                _logger.LogWarning("Bible data directory not found: {Dir}", _bibleDataDirectory);
                Directory.CreateDirectory(_bibleDataDirectory);
                return allVerses;
            }

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var translation in _enabledTranslations)
            {
                var filePath = Path.Combine(_bibleDataDirectory, $"{translation}.json");
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Translation file not found: {FilePath}", filePath);
                    continue;
                }

                try
                {
                    var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                    var verses = JsonSerializer.Deserialize<List<BibleVerse>>(json, jsonOptions);
                    
                    if (verses != null && verses.Count > 0)
                    {
                        allVerses.AddRange(verses);
                        _logger.LogInformation("Loaded {Count} verses from {Translation}", verses.Count, translation.ToUpper());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading translation file: {FilePath}", filePath);
                }
            }

            _cachedVerses = allVerses;
            _logger.LogInformation("Total: {Count} Bible verses loaded from {TranslationCount} translations", 
                allVerses.Count, _enabledTranslations.Count);
            
            return allVerses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Bible verses");
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
        
        var query = allVerses.Where(v => 
            v.Book.Equals(book, StringComparison.OrdinalIgnoreCase) && 
            v.Chapter == chapter);

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

    /// <summary>
    /// Get verses from a specific translation
    /// </summary>
    public async Task<List<BibleVerse>> GetVersesByTranslationAsync(
        string translation,
        CancellationToken cancellationToken = default)
    {
        var allVerses = await LoadAllVersesAsync(cancellationToken);
        return allVerses.Where(v => v.Translation.Equals(translation, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Get list of available translations
    /// </summary>
    public List<string> GetAvailableTranslations()
    {
        var translations = new List<string>();
        
        if (Directory.Exists(_bibleDataDirectory))
        {
            foreach (var file in Directory.GetFiles(_bibleDataDirectory, "*.json"))
            {
                translations.Add(Path.GetFileNameWithoutExtension(file).ToUpper());
            }
        }
        
        return translations;
    }
}
