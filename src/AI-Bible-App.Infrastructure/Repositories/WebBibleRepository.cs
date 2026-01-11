using System.Text.Json;
using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AI_Bible_App.Infrastructure.Repositories;

/// <summary>
/// Repository for loading World English Bible verses from JSON
/// </summary>
public class WebBibleRepository : IBibleRepository
{
    private readonly ILogger<WebBibleRepository> _logger;
    private readonly string _dataPath;
    private List<BibleVerse>? _cachedVerses;

    public WebBibleRepository(IConfiguration configuration, ILogger<WebBibleRepository> logger)
    {
        _logger = logger;
        _dataPath = configuration["Bible:WebDataPath"] ?? Path.Combine("Data", "Bible", "web.json");
        
        _logger.LogInformation("WebBibleRepository initialized with data path: {DataPath}", _dataPath);
    }

    public async Task<List<BibleVerse>> LoadAllVersesAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedVerses != null)
        {
            _logger.LogDebug("Returning cached WEB verses ({Count} verses)", _cachedVerses.Count);
            return _cachedVerses;
        }

        try
        {
            if (!File.Exists(_dataPath))
            {
                _logger.LogWarning("WEB Bible data file not found at {DataPath}. Creating sample data.", _dataPath);
                await CreateSampleWebDataAsync(cancellationToken);
            }

            var json = await File.ReadAllTextAsync(_dataPath, cancellationToken);
            var verses = JsonSerializer.Deserialize<List<BibleVerse>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _cachedVerses = verses ?? new List<BibleVerse>();
            _logger.LogInformation("Loaded {Count} WEB verses from {DataPath}", _cachedVerses.Count, _dataPath);
            
            return _cachedVerses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading WEB verses from {DataPath}", _dataPath);
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
    /// Create sample WEB Bible data for initial testing
    /// </summary>
    private async Task CreateSampleWebDataAsync(CancellationToken cancellationToken)
    {
        var sampleVerses = new List<BibleVerse>
        {
            // Psalm 23 (complete) - WEB translation
            new BibleVerse { Book = "Psalms", Chapter = 23, Verse = 1, Testament = "OT", Translation = "WEB", BookNumber = 19,
                Text = "Yahweh is my shepherd; I shall lack nothing." },
            new BibleVerse { Book = "Psalms", Chapter = 23, Verse = 2, Testament = "OT", Translation = "WEB", BookNumber = 19,
                Text = "He makes me lie down in green pastures. He leads me beside still waters." },
            new BibleVerse { Book = "Psalms", Chapter = 23, Verse = 3, Testament = "OT", Translation = "WEB", BookNumber = 19,
                Text = "He restores my soul. He guides me in the paths of righteousness for his name's sake." },
            new BibleVerse { Book = "Psalms", Chapter = 23, Verse = 4, Testament = "OT", Translation = "WEB", BookNumber = 19,
                Text = "Even though I walk through the valley of the shadow of death, I will fear no evil, for you are with me. Your rod and your staff, they comfort me." },
            new BibleVerse { Book = "Psalms", Chapter = 23, Verse = 5, Testament = "OT", Translation = "WEB", BookNumber = 19,
                Text = "You prepare a table before me in the presence of my enemies. You anoint my head with oil. My cup runs over." },
            new BibleVerse { Book = "Psalms", Chapter = 23, Verse = 6, Testament = "OT", Translation = "WEB", BookNumber = 19,
                Text = "Surely goodness and loving kindness shall follow me all the days of my life, and I will dwell in Yahweh's house forever." },
            
            // John 3:16-17 - WEB translation
            new BibleVerse { Book = "John", Chapter = 3, Verse = 16, Testament = "NT", Translation = "WEB", BookNumber = 43,
                Text = "For God so loved the world, that he gave his only born Son, that whoever believes in him should not perish, but have eternal life." },
            new BibleVerse { Book = "John", Chapter = 3, Verse = 17, Testament = "NT", Translation = "WEB", BookNumber = 43,
                Text = "For God didn't send his Son into the world to judge the world, but that the world should be saved through him." },
            
            // Romans 8:28 - WEB translation
            new BibleVerse { Book = "Romans", Chapter = 8, Verse = 28, Testament = "NT", Translation = "WEB", BookNumber = 45,
                Text = "We know that all things work together for good for those who love God, for those who are called according to his purpose." },
            
            // Proverbs 3:5-6 - WEB translation
            new BibleVerse { Book = "Proverbs", Chapter = 3, Verse = 5, Testament = "OT", Translation = "WEB", BookNumber = 20,
                Text = "Trust in Yahweh with all your heart, and don't lean on your own understanding." },
            new BibleVerse { Book = "Proverbs", Chapter = 3, Verse = 6, Testament = "OT", Translation = "WEB", BookNumber = 20,
                Text = "In all your ways acknowledge him, and he will make your paths straight." },
            
            // Philippians 4:13 - WEB translation
            new BibleVerse { Book = "Philippians", Chapter = 4, Verse = 13, Testament = "NT", Translation = "WEB", BookNumber = 50,
                Text = "I can do all things through Christ, who strengthens me." },
            
            // Genesis 1:1 - WEB translation
            new BibleVerse { Book = "Genesis", Chapter = 1, Verse = 1, Testament = "OT", Translation = "WEB", BookNumber = 1,
                Text = "In the beginning, God created the heavens and the earth." },
            
            // Matthew 28:19-20 - WEB translation
            new BibleVerse { Book = "Matthew", Chapter = 28, Verse = 19, Testament = "NT", Translation = "WEB", BookNumber = 40,
                Text = "Go and make disciples of all nations, baptizing them in the name of the Father and of the Son and of the Holy Spirit," },
            new BibleVerse { Book = "Matthew", Chapter = 28, Verse = 20, Testament = "NT", Translation = "WEB", BookNumber = 40,
                Text = "teaching them to observe all things that I commanded you. Behold, I am with you always, even to the end of the age." },
                
            // Additional popular verses - WEB translation
            new BibleVerse { Book = "Jeremiah", Chapter = 29, Verse = 11, Testament = "OT", Translation = "WEB", BookNumber = 24,
                Text = "For I know the thoughts that I think toward you, says Yahweh, thoughts of peace, and not of evil, to give you hope and a future." },
            new BibleVerse { Book = "Isaiah", Chapter = 40, Verse = 31, Testament = "OT", Translation = "WEB", BookNumber = 23,
                Text = "But those who wait for Yahweh will renew their strength. They will mount up with wings like eagles. They will run, and not be weary. They will walk, and not faint." },
            new BibleVerse { Book = "Joshua", Chapter = 1, Verse = 9, Testament = "OT", Translation = "WEB", BookNumber = 6,
                Text = "Haven't I commanded you? Be strong and courageous. Don't be afraid. Don't be dismayed, for Yahweh your God is with you wherever you go." }
        };

        Directory.CreateDirectory(Path.GetDirectoryName(_dataPath)!);
        
        var json = JsonSerializer.Serialize(sampleVerses, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(_dataPath, json, cancellationToken);
        _logger.LogInformation("Created sample WEB Bible data at {DataPath} with {Count} verses", _dataPath, sampleVerses.Count);
    }
}
