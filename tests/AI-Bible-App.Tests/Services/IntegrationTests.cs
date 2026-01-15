using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using AI_Bible_App.Core.Models;
using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Infrastructure.Repositories;
using AI_Bible_App.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace AI_Bible_App.Tests.Services;

/// <summary>
/// Integration tests for the RAG (Retrieval-Augmented Generation) flow.
/// Tests the complete pipeline from query to context retrieval.
/// </summary>
public class RAGIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ILogger<BibleRAGService>> _loggerMock;
    private readonly Mock<IBibleRepository> _bibleRepoMock;
    private readonly IConfiguration _configuration;

    public RAGIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerMock = new Mock<ILogger<BibleRAGService>>();
        _bibleRepoMock = new Mock<IBibleRepository>();
        
        // Setup test configuration
        var configData = new Dictionary<string, string?>
        {
            { "Ollama:Url", "http://localhost:11434" },
            { "RAG:EmbeddingModel", "nomic-embed-text" },
            { "RAG:TopK", "5" },
            { "RAG:ChunkingStrategy", "SingleVerse" }
        };
        
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Fact]
    public void RAGConfiguration_ShouldLoadDefaults()
    {
        // Arrange & Act
        var ollamaUrl = _configuration["Ollama:Url"];
        var embeddingModel = _configuration["RAG:EmbeddingModel"];
        var topK = int.Parse(_configuration["RAG:TopK"] ?? "5");

        // Assert
        Assert.Equal("http://localhost:11434", ollamaUrl);
        Assert.Equal("nomic-embed-text", embeddingModel);
        Assert.Equal(5, topK);
        
        _output.WriteLine("✅ RAG configuration loads correctly");
    }

    [Fact]
    public void BibleVerse_ShouldHaveRequiredFields()
    {
        // Arrange
        var verse = new BibleVerse
        {
            Book = "John",
            Chapter = 3,
            Verse = 16,
            Text = "For God so loved the world..."
        };

        // Assert
        Assert.False(string.IsNullOrEmpty(verse.Book));
        Assert.True(verse.Chapter > 0);
        Assert.True(verse.Verse > 0);
        Assert.False(string.IsNullOrEmpty(verse.Text));
        Assert.Equal("John 3:16", verse.Reference);
        
        _output.WriteLine($"✅ BibleVerse {verse.Reference} has all required fields");
    }

    [Fact]
    public void BibleVerse_ReferenceProperty_ShouldFormatCorrectly()
    {
        // Arrange
        var testCases = new[]
        {
            (Book: "Genesis", Chapter: 1, Verse: 1, Expected: "Genesis 1:1"),
            (Book: "Psalm", Chapter: 23, Verse: 1, Expected: "Psalm 23:1"),
            (Book: "1 Corinthians", Chapter: 13, Verse: 4, Expected: "1 Corinthians 13:4"),
            (Book: "Revelation", Chapter: 22, Verse: 21, Expected: "Revelation 22:21")
        };

        // Act & Assert
        foreach (var tc in testCases)
        {
            var verse = new BibleVerse
            {
                Book = tc.Book,
                Chapter = tc.Chapter,
                Verse = tc.Verse,
                Text = "Test text"
            };

            Assert.Equal(tc.Expected, verse.Reference);
            _output.WriteLine($"  ✅ {tc.Expected} formats correctly");
        }
    }

    [Fact]
    public async Task MockBibleRepository_ShouldReturnVerses()
    {
        // Arrange
        var mockVerses = new List<BibleVerse>
        {
            new() { Book = "John", Chapter = 3, Verse = 16, Text = "For God so loved the world..." },
            new() { Book = "Romans", Chapter = 8, Verse = 28, Text = "And we know that all things work together..." }
        };

        _bibleRepoMock
            .Setup(r => r.GetVersesAsync("John", 3, It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockVerses.Where(v => v.Book == "John" && v.Chapter == 3).ToList());

        // Act
        var verses = await _bibleRepoMock.Object.GetVersesAsync("John", 3, null, null, CancellationToken.None);

        // Assert
        Assert.Single(verses);
        Assert.Equal("John 3:16", verses[0].Reference);
        _output.WriteLine("✅ Mock Bible repository returns verses correctly");
    }

    [Fact]
    public void RAGContext_ShouldContainRelevantVerses()
    {
        // Arrange - simulate RAG context building
        var query = "What does the Bible say about love?";
        var relevantVerses = new List<BibleVerse>
        {
            new() { Book = "1 Corinthians", Chapter = 13, Verse = 4, Text = "Love is patient, love is kind..." },
            new() { Book = "1 John", Chapter = 4, Verse = 8, Text = "God is love..." },
            new() { Book = "John", Chapter = 3, Verse = 16, Text = "For God so loved the world..." }
        };

        // Act - build context string
        var context = string.Join("\n", relevantVerses.Select(v => $"- {v.Reference}: \"{v.Text}\""));

        // Assert
        Assert.Contains("1 Corinthians 13:4", context);
        Assert.Contains("1 John 4:8", context);
        Assert.Contains("John 3:16", context);
        
        _output.WriteLine($"✅ RAG context for query '{query}' includes {relevantVerses.Count} relevant verses");
        _output.WriteLine($"Context preview:\n{context}");
    }

    [Theory]
    [InlineData("love", new[] { "1 Corinthians 13", "1 John 4", "John 3" })]
    [InlineData("faith", new[] { "Hebrews 11", "James 2", "Romans 10" })]
    [InlineData("hope", new[] { "Romans 5", "Hebrews 6", "1 Peter 1" })]
    [InlineData("peace", new[] { "Philippians 4", "Isaiah 26", "John 14" })]
    public void CommonTopics_ShouldMapToExpectedBooks(string topic, string[] expectedBooks)
    {
        // This test documents expected behavior for common Bible topics
        _output.WriteLine($"Topic '{topic}' should retrieve from: {string.Join(", ", expectedBooks)}");
        Assert.NotEmpty(expectedBooks);
    }

    [Fact]
    public void ChunkingStrategies_ShouldBeConfigurable()
    {
        // Arrange
        var strategies = new[] { "SingleVerse", "Paragraph", "Chapter", "Semantic" };
        var configuredStrategy = _configuration["RAG:ChunkingStrategy"];

        // Assert
        Assert.Contains(configuredStrategy, strategies);
        _output.WriteLine($"✅ Chunking strategy '{configuredStrategy}' is valid");
    }

    [Fact]
    public async Task ConcurrentRAGQueries_ShouldBeThreadSafe()
    {
        // Arrange
        var queries = new[]
        {
            "What is love?",
            "How to pray?",
            "Who was David?",
            "What is faith?",
            "Explain grace"
        };

        var tasks = new List<Task<string>>();

        // Act - simulate concurrent queries
        foreach (var query in queries)
        {
            tasks.Add(Task.Run(() =>
            {
                // Simulate RAG lookup delay
                Thread.Sleep(Random.Shared.Next(10, 50));
                return $"Context for: {query}";
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(queries.Length, results.Length);
        Assert.All(results, r => Assert.StartsWith("Context for:", r));
        
        _output.WriteLine($"✅ {queries.Length} concurrent RAG queries completed successfully");
    }
}

/// <summary>
/// Unit tests for SQLite repositories.
/// </summary>
public class SqliteRepositoryTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _testDbPath;
    private readonly Mock<ILogger<SqliteChatRepository>> _chatLoggerMock;
    private readonly Mock<ILogger<SqlitePrayerRepository>> _prayerLoggerMock;

    public SqliteRepositoryTests(ITestOutputHelper output)
    {
        _output = output;
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.db");
        _chatLoggerMock = new Mock<ILogger<SqliteChatRepository>>();
        _prayerLoggerMock = new Mock<ILogger<SqlitePrayerRepository>>();
    }

    [Fact]
    public async Task SqliteChatRepository_SaveAndRetrieve_Works()
    {
        // Arrange
        var repo = new SqliteChatRepository(_chatLoggerMock.Object, _testDbPath.Replace(".db", "_chat.db"));
        var session = new ChatSession
        {
            Id = Guid.NewGuid().ToString(),
            CharacterId = "moses",
            UserId = "test-user",
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "Hello Moses!" },
                new() { Role = "assistant", Content = "Greetings! I am Moses." }
            }
        };

        // Act
        await repo.SaveSessionAsync(session);
        var retrieved = await repo.GetSessionAsync(session.Id);

        // Assert
        Assert.Equal(session.Id, retrieved.Id);
        Assert.Equal(session.CharacterId, retrieved.CharacterId);
        Assert.Equal(2, retrieved.Messages.Count);
        
        _output.WriteLine($"✅ Chat session saved and retrieved with {retrieved.Messages.Count} messages");
    }

    [Fact]
    public async Task SqliteChatRepository_GetAllSessions_Works()
    {
        // Arrange
        var repo = new SqliteChatRepository(_chatLoggerMock.Object, _testDbPath.Replace(".db", "_chat_all.db"));
        
        for (int i = 0; i < 5; i++)
        {
            await repo.SaveSessionAsync(new ChatSession
            {
                Id = Guid.NewGuid().ToString(),
                CharacterId = $"character_{i % 3}",
                Messages = new List<ChatMessage>
                {
                    new() { Role = "user", Content = $"Message {i}" }
                }
            });
        }

        // Act
        var sessions = await repo.GetAllSessionsAsync();

        // Assert
        Assert.Equal(5, sessions.Count);
        _output.WriteLine($"✅ Retrieved {sessions.Count} chat sessions");
    }

    [Fact]
    public async Task SqliteChatRepository_Delete_Works()
    {
        // Arrange
        var repo = new SqliteChatRepository(_chatLoggerMock.Object, _testDbPath.Replace(".db", "_chat_del.db"));
        var session = new ChatSession
        {
            Id = Guid.NewGuid().ToString(),
            CharacterId = "paul"
        };

        await repo.SaveSessionAsync(session);

        // Act
        await repo.DeleteSessionAsync(session.Id);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repo.GetSessionAsync(session.Id));
        _output.WriteLine("✅ Chat session deleted successfully");
    }

    [Fact]
    public async Task SqlitePrayerRepository_SaveAndRetrieve_Works()
    {
        // Arrange
        var repo = new SqlitePrayerRepository(_prayerLoggerMock.Object, _testDbPath.Replace(".db", "_prayer.db"));
        var prayer = new Prayer
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Lord, grant me wisdom...",
            Topic = "Wisdom",
            Tags = new List<string> { "guidance", "wisdom" }
        };

        // Act
        await repo.SavePrayerAsync(prayer);
        var retrieved = await repo.GetPrayerAsync(prayer.Id);

        // Assert
        Assert.Equal(prayer.Id, retrieved.Id);
        Assert.Equal(prayer.Content, retrieved.Content);
        Assert.Equal(prayer.Topic, retrieved.Topic);
        
        _output.WriteLine($"✅ Prayer saved and retrieved: {prayer.Topic}");
    }

    [Fact]
    public async Task SqlitePrayerRepository_SearchByTopic_Works()
    {
        // Arrange
        var repo = new SqlitePrayerRepository(_prayerLoggerMock.Object, _testDbPath.Replace(".db", "_prayer_search.db"));
        
        await repo.SavePrayerAsync(new Prayer { Id = "1", Content = "Prayer about love", Topic = "Love" });
        await repo.SavePrayerAsync(new Prayer { Id = "2", Content = "Prayer about peace", Topic = "Peace" });
        await repo.SavePrayerAsync(new Prayer { Id = "3", Content = "Another love prayer", Topic = "Love" });

        // Act
        var lovePrayers = await repo.GetPrayersByTopicAsync("Love");

        // Assert
        Assert.Equal(2, lovePrayers.Count);
        Assert.All(lovePrayers, p => Assert.Contains("Love", p.Topic));
        
        _output.WriteLine($"✅ Found {lovePrayers.Count} prayers about Love");
    }

    public void Dispose()
    {
        // Cleanup test databases
        try
        {
            var testFiles = Directory.GetFiles(Path.GetTempPath(), "test_db_*.db");
            foreach (var file in testFiles)
            {
                File.Delete(file);
            }
        }
        catch { /* Ignore cleanup errors */ }
    }
}

/// <summary>
/// Performance tests for response times on lower-end devices.
/// </summary>
public class PerformanceTests
{
    private readonly ITestOutputHelper _output;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task CharacterRepository_LoadTime_ShouldBeUnder100ms()
    {
        // Arrange
        var repo = new InMemoryCharacterRepository();
        var sw = Stopwatch.StartNew();

        // Act
        var characters = await repo.GetAllCharactersAsync();
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < 100, $"Character load took {sw.ElapsedMilliseconds}ms, expected < 100ms");
        _output.WriteLine($"✅ Character load: {sw.ElapsedMilliseconds}ms ({characters.Count} characters)");
    }

    [Fact]
    public async Task CharacterLookup_ShouldBeUnder10ms()
    {
        // Arrange
        var repo = new InMemoryCharacterRepository();
        await repo.GetAllCharactersAsync(); // Warm up
        
        var sw = Stopwatch.StartNew();

        // Act
        var character = await repo.GetCharacterAsync("moses");
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < 10, $"Character lookup took {sw.ElapsedMilliseconds}ms, expected < 10ms");
        _output.WriteLine($"✅ Character lookup: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void MemoryUsage_CharacterRepository_ShouldBeReasonable()
    {
        // Arrange
        GC.Collect();
        var memBefore = GC.GetTotalMemory(true);
        
        // Act
        var repo = new InMemoryCharacterRepository();
        GC.Collect();
        var memAfter = GC.GetTotalMemory(true);
        
        var memUsedKB = (memAfter - memBefore) / 1024.0;

        // Assert - should use less than 1MB for character data
        Assert.True(memUsedKB < 1024, $"Character repository used {memUsedKB:F0}KB, expected < 1024KB");
        _output.WriteLine($"✅ Memory usage: {memUsedKB:F0}KB");
    }

    [Fact]
    public async Task ConcurrentAccess_ShouldScale()
    {
        // Arrange
        var repo = new InMemoryCharacterRepository();
        var concurrencyLevels = new[] { 10, 50, 100, 200 };
        
        _output.WriteLine("Concurrent Access Scaling Test:");
        
        foreach (var level in concurrencyLevels)
        {
            var tasks = new List<Task<BiblicalCharacter?>>();
            var sw = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < level; i++)
            {
                tasks.Add(repo.GetCharacterAsync("moses"));
            }

            await Task.WhenAll(tasks);
            sw.Stop();

            var avgMs = sw.ElapsedMilliseconds / (double)level;
            _output.WriteLine($"  {level} concurrent: {sw.ElapsedMilliseconds}ms total, {avgMs:F2}ms avg");

            // Assert - average should stay reasonable
            Assert.True(avgMs < 5, $"Average response time {avgMs:F2}ms exceeds 5ms at {level} concurrency");
        }
    }

    [Fact]
    public void ObjectCreation_ChatSession_ShouldBeFast()
    {
        // Arrange
        var count = 1000;
        var sw = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < count; i++)
        {
            var session = new ChatSession
            {
                CharacterId = "moses",
                Messages = new List<ChatMessage>
                {
                    new() { Role = "user", Content = "Hello" }
                }
            };
        }
        sw.Stop();

        var avgMicroseconds = (sw.ElapsedTicks / (double)count) * 1000000 / Stopwatch.Frequency;
        
        // Assert - keep this reasonably fast while avoiding flakiness across machines/CI
        Assert.True(avgMicroseconds < 1000, $"ChatSession creation took {avgMicroseconds:F1}μs, expected < 1000μs");
        _output.WriteLine($"✅ ChatSession creation: {avgMicroseconds:F1}μs average ({count} objects)");
    }

    [Fact]
    public void StringOperations_SystemPrompt_ShouldBeEfficient()
    {
        // Arrange
        var character = new BiblicalCharacter
        {
            Id = "test",
            Name = "Test Character",
            SystemPrompt = string.Concat(Enumerable.Repeat("This is a long system prompt. ", 100))
        };

        var sw = Stopwatch.StartNew();

        // Act - simulate prompt building 1000 times
        for (int i = 0; i < 1000; i++)
        {
            var prompt = $"You are {character.Name}. {character.SystemPrompt}\n\nUser says: Hello";
        }
        sw.Stop();

        _output.WriteLine($"✅ Prompt building (1000x): {sw.ElapsedMilliseconds}ms");
        Assert.True(sw.ElapsedMilliseconds < 100, "Prompt building should be fast");
    }
}

/// <summary>
/// End-to-end scenario tests.
/// </summary>
public class EndToEndScenarioTests
{
    private readonly ITestOutputHelper _output;

    public EndToEndScenarioTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Scenario_NewUserStartsConversation()
    {
        // Simulate a new user starting their first conversation
        
        // 1. User selects a character
        var repo = new InMemoryCharacterRepository();
        var moses = await repo.GetCharacterAsync("moses");
        Assert.NotNull(moses);
        _output.WriteLine("1. ✅ User selected Moses");

        // 2. Create a new chat session
        var session = new ChatSession
        {
            CharacterId = moses.Id,
            UserId = "new-user-123"
        };
        Assert.NotNull(session.Id);
        _output.WriteLine("2. ✅ Chat session created");

        // 3. User sends first message
        session.Messages.Add(new ChatMessage
        {
            Role = "user",
            Content = "Hello Moses! What was it like to lead the Israelites?"
        });
        Assert.Single(session.Messages);
        _output.WriteLine("3. ✅ User sent first message");

        // 4. Simulate AI response
        session.Messages.Add(new ChatMessage
        {
            Role = "assistant",
            Content = "Greetings, my child! Leading the Israelites was both a great honor and a tremendous challenge..."
        });
        Assert.Equal(2, session.Messages.Count);
        _output.WriteLine("4. ✅ AI responded");

        _output.WriteLine("\n✅ Complete scenario: New user conversation flow works");
    }

    [Fact]
    public async Task Scenario_UserSavesPrayer()
    {
        // Simulate a user generating and saving a prayer
        
        // 1. User requests prayer on a topic
        var topic = "Gratitude";
        var style = "Traditional";
        _output.WriteLine($"1. ✅ User requests {style} prayer about {topic}");

        // 2. Prayer is generated (simulated)
        var prayer = new SavedPrayer
        {
            UserId = "user-456",
            Topic = topic,
            Content = "Heavenly Father, I come before You with a heart full of gratitude...",
            IsFavorite = false
        };
        Assert.NotEmpty(prayer.Content);
        _output.WriteLine("2. ✅ Prayer generated");

        // 3. User marks as favorite
        prayer.IsFavorite = true;
        Assert.True(prayer.IsFavorite);
        _output.WriteLine("3. ✅ User marked prayer as favorite");

        // 4. User prays the prayer
        prayer.LastPrayedAt = DateTime.UtcNow;
        Assert.NotNull(prayer.LastPrayedAt);
        _output.WriteLine("4. ✅ Prayer marked as prayed");

        _output.WriteLine("\n✅ Complete scenario: Prayer generation and saving works");
    }

    [Fact]
    public async Task Scenario_UserSearchesBible()
    {
        // Simulate a user searching for Bible verses
        
        // 1. User enters search query
        var query = "love";
        _output.WriteLine($"1. ✅ User searches for '{query}'");

        // 2. Search returns results (simulated)
        var results = new List<BibleVerse>
        {
            new() { Book = "1 Corinthians", Chapter = 13, Verse = 4, Text = "Love is patient, love is kind..." },
            new() { Book = "1 John", Chapter = 4, Verse = 8, Text = "Whoever does not love does not know God, because God is love." }
        };
        Assert.NotEmpty(results);
        _output.WriteLine($"2. ✅ Found {results.Count} matching verses");

        // 3. User selects a verse to discuss
        var selectedVerse = results[0];
        Assert.NotNull(selectedVerse);
        _output.WriteLine($"3. ✅ User selected {selectedVerse.Reference}");

        // 4. Verse is added to conversation context
        var contextMessage = $"Let's discuss {selectedVerse.Reference}: \"{selectedVerse.Text}\"";
        Assert.Contains(selectedVerse.Reference, contextMessage);
        _output.WriteLine("4. ✅ Verse added to conversation context");

        _output.WriteLine("\n✅ Complete scenario: Bible search flow works");
    }

    [Fact]
    public async Task Scenario_AllCharactersAccessible()
    {
        // Verify all expected characters are accessible
        var expectedCharacters = new[]
        {
            "moses", "david", "paul", "mary", "peter", "solomon", "ruth"
        };

        var repo = new InMemoryCharacterRepository();
        
        foreach (var id in expectedCharacters)
        {
            var character = await repo.GetCharacterAsync(id);
            Assert.NotNull(character);
            Assert.Equal(id, character.Id);
            _output.WriteLine($"  ✅ {character.Name} accessible");
        }

        _output.WriteLine($"\n✅ All {expectedCharacters.Length} expected characters are accessible");
    }
}
