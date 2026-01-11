using System.Text.Json;
using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AI_Bible_App.Infrastructure.Repositories;

/// <summary>
/// Repository for loading Bible verses from JSON files
/// </summary>
public class JsonBibleRepository : IBibleRepository
{
    private readonly ILogger<JsonBibleRepository> _logger;
    private readonly string _dataPath;
    private List<BibleVerse>? _cachedVerses;

    public JsonBibleRepository(IConfiguration configuration, ILogger<JsonBibleRepository> logger)
    {
        _logger = logger;
        _dataPath = configuration["Bible:DataPath"] ?? Path.Combine("Data", "Bible", "kjv.json");
        
        _logger.LogInformation("JsonBibleRepository initialized with data path: {DataPath}", _dataPath);
    }

    public async Task<List<BibleVerse>> LoadAllVersesAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedVerses != null)
        {
            _logger.LogDebug("Returning cached Bible verses ({Count} verses)", _cachedVerses.Count);
            return _cachedVerses;
        }

        try
        {
            if (!File.Exists(_dataPath))
            {
                _logger.LogWarning("Bible data file not found at {DataPath}. Creating sample data.", _dataPath);
                await CreateSampleBibleDataAsync(cancellationToken);
            }

            var json = await File.ReadAllTextAsync(_dataPath, cancellationToken);
            var verses = JsonSerializer.Deserialize<List<BibleVerse>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _cachedVerses = verses ?? new List<BibleVerse>();
            _logger.LogInformation("Loaded {Count} Bible verses from {DataPath}", _cachedVerses.Count, _dataPath);
            
            return _cachedVerses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Bible verses from {DataPath}", _dataPath);
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
    /// Create sample Bible data for initial testing
    /// </summary>
    private async Task CreateSampleBibleDataAsync(CancellationToken cancellationToken)
    {
        var sampleVerses = new List<BibleVerse>
        {
            // John 3:16-17
            new BibleVerse { Book = "John", Chapter = 3, Verse = 16, Testament = "NT", 
                Text = "For God so loved the world, that he gave his only begotten Son, that whosoever believeth in him should not perish, but have everlasting life." },
            new BibleVerse { Book = "John", Chapter = 3, Verse = 17, Testament = "NT",
                Text = "For God sent not his Son into the world to condemn the world; but that the world through him might be saved." },
            
            // Psalm 23 (complete)
            new BibleVerse { Book = "Psalms", Chapter = 23, Verse = 1, Testament = "OT",
                Text = "The LORD is my shepherd; I shall not want." },
            new BibleVerse { Book = "Psalms", Chapter = 23, Verse = 2, Testament = "OT",
                Text = "He maketh me to lie down in green pastures: he leadeth me beside the still waters." },
            new BibleVerse { Book = "Psalms", Chapter = 23, Verse = 3, Testament = "OT",
                Text = "He restoreth my soul: he leadeth me in the paths of righteousness for his name's sake." },
            new BibleVerse { Book = "Psalms", Chapter = 23, Verse = 4, Testament = "OT",
                Text = "Yea, though I walk through the valley of the shadow of death, I will fear no evil: for thou art with me; thy rod and thy staff they comfort me." },
            new BibleVerse { Book = "Psalms", Chapter = 23, Verse = 5, Testament = "OT",
                Text = "Thou preparest a table before me in the presence of mine enemies: thou anointest my head with oil; my cup runneth over." },
            new BibleVerse { Book = "Psalms", Chapter = 23, Verse = 6, Testament = "OT",
                Text = "Surely goodness and mercy shall follow me all the days of my life: and I will dwell in the house of the LORD for ever." },
            
            // Romans 8:28
            new BibleVerse { Book = "Romans", Chapter = 8, Verse = 28, Testament = "NT",
                Text = "And we know that all things work together for good to them that love God, to them who are the called according to his purpose." },
            
            // Proverbs 3:5-6
            new BibleVerse { Book = "Proverbs", Chapter = 3, Verse = 5, Testament = "OT",
                Text = "Trust in the LORD with all thine heart; and lean not unto thine own understanding." },
            new BibleVerse { Book = "Proverbs", Chapter = 3, Verse = 6, Testament = "OT",
                Text = "In all thy ways acknowledge him, and he shall direct thy paths." },
            
            // Philippians 4:13
            new BibleVerse { Book = "Philippians", Chapter = 4, Verse = 13, Testament = "NT",
                Text = "I can do all things through Christ which strengtheneth me." },
            
            // Genesis 1:1
            new BibleVerse { Book = "Genesis", Chapter = 1, Verse = 1, Testament = "OT",
                Text = "In the beginning God created the heaven and the earth." },
            
            // Matthew 28:19-20
            new BibleVerse { Book = "Matthew", Chapter = 28, Verse = 19, Testament = "NT",
                Text = "Go ye therefore, and teach all nations, baptizing them in the name of the Father, and of the Son, and of the Holy Ghost:" },
            new BibleVerse { Book = "Matthew", Chapter = 28, Verse = 20, Testament = "NT",
                Text = "Teaching them to observe all things whatsoever I have commanded you: and, lo, I am with you always, even unto the end of the world. Amen." }
        };

        Directory.CreateDirectory(Path.GetDirectoryName(_dataPath)!);
        
        var json = JsonSerializer.Serialize(sampleVerses, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(_dataPath, json, cancellationToken);
        _logger.LogInformation("Created sample Bible data at {DataPath} with {Count} verses", _dataPath, sampleVerses.Count);
    }
}
