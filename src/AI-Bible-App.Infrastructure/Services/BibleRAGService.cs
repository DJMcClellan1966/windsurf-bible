using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.Ollama;
using OllamaSharp;
using System.Text;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0070

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// RAG service for retrieving relevant Bible verses using semantic search
/// </summary>
public class BibleRAGService : IBibleRAGService
{
    private readonly IBibleRepository _bibleRepository;
    private readonly ILogger<BibleRAGService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _ollamaUrl;
    private readonly string _embeddingModel;
    private readonly ChunkingStrategy _chunkingStrategy;
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly Dictionary<string, (BibleChunk Chunk, ReadOnlyMemory<float> Embedding)> _vectorStore;
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    public BibleRAGService(
        IBibleRepository bibleRepository,
        IConfiguration configuration,
        ILogger<BibleRAGService> logger)
    {
        _bibleRepository = bibleRepository;
        _configuration = configuration;
        _logger = logger;
        _ollamaUrl = configuration["Ollama:Url"] ?? "http://localhost:11434";
        _embeddingModel = configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
        _vectorStore = new Dictionary<string, (BibleChunk, ReadOnlyMemory<float>)>();
        
        // Get chunking strategy from config
        var strategyStr = configuration["RAG:ChunkingStrategy"] ?? "SingleVerse";
        _chunkingStrategy = Enum.Parse<ChunkingStrategy>(strategyStr, ignoreCase: true);
        
        // Initialize embedding service using OllamaApiClient (recommended approach)
        var ollamaClient = new OllamaApiClient(new Uri(_ollamaUrl))
        {
            SelectedModel = _embeddingModel
        };
        _embeddingService = ollamaClient.AsTextEmbeddingGenerationService();

        _logger.LogInformation(
            "BibleRAGService created with embedding model: {Model} at {Url}, Chunking: {Strategy}", 
            _embeddingModel, 
            _ollamaUrl,
            _chunkingStrategy);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            _logger.LogDebug("BibleRAGService already initialized");
            return;
        }

        try
        {
            _logger.LogInformation("Initializing BibleRAGService...");

            // Load all Bible verses
            var verses = await _bibleRepository.LoadAllVersesAsync(cancellationToken);
            _logger.LogInformation("Loaded {Count} verses from repository", verses.Count);

            // Chunk verses into meaningful groups (3-5 verses per chunk for context)
            var chunks = CreateChunks(verses);
            _logger.LogInformation("Created {Count} chunks from verses", chunks.Count);

            // Generate embeddings for each chunk
            _logger.LogInformation("Generating embeddings for {Count} chunks...", chunks.Count);
            var embeddingTasks = chunks.Select(async chunk =>
            {
                try
                {
                    var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Text, cancellationToken: cancellationToken);
                    return (chunk, embedding);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating embedding for chunk {ChunkId}", chunk.Id);
                    return (chunk, ReadOnlyMemory<float>.Empty);
                }
            });

            var embeddingResults = await Task.WhenAll(embeddingTasks);

            // Store in vector store
            foreach (var result in embeddingResults)
            {
                if (!result.Item2.IsEmpty)
                {
                    _vectorStore[result.Item1.Id] = (result.Item1, result.Item2);
                }
            }

            _isInitialized = true;
            _logger.LogInformation(
                "BibleRAGService initialized successfully with {Count} indexed chunks", 
                _vectorStore.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing BibleRAGService");
            throw;
        }
    }

    public async Task<List<BibleChunk>> RetrieveRelevantVersesAsync(
        string query,
        int limit = 5,
        double minRelevanceScore = 0.7,
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            _logger.LogWarning("BibleRAGService not initialized. Returning empty results.");
            return new List<BibleChunk>();
        }

        try
        {
            // Generate embedding for the query
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: cancellationToken);

            // Calculate cosine similarity with all chunks
            var similarities = _vectorStore.Select(kvp =>
            {
                var similarity = CosineSimilarity(queryEmbedding, kvp.Value.Embedding);
                return (Chunk: kvp.Value.Chunk, Similarity: similarity);
            })
            .Where(x => x.Similarity >= minRelevanceScore)
            .OrderByDescending(x => x.Similarity)
            .Take(limit)
            .ToList();

            _logger.LogInformation(
                "Retrieved {Count} relevant chunks for query (min score: {MinScore})", 
                similarities.Count, 
                minRelevanceScore);

            return similarities.Select(x => x.Chunk).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving relevant verses for query: {Query}", query);
            return new List<BibleChunk>();
        }
    }

    /// <summary>
    /// Create chunks from verses based on configured strategy
    /// </summary>
    private List<BibleChunk> CreateChunks(List<BibleVerse> verses)
    {
        return _chunkingStrategy switch
        {
            ChunkingStrategy.SingleVerse => CreateSingleVerseChunks(verses),
            ChunkingStrategy.VerseWithOverlap => CreateVerseChunksWithOverlap(verses),
            ChunkingStrategy.MultiVerse => CreateMultiVerseChunks(verses, 3),
            _ => CreateSingleVerseChunks(verses)
        };
    }

    /// <summary>
    /// Create one chunk per verse with reference included (Primary strategy for WEB)
    /// </summary>
    private List<BibleChunk> CreateSingleVerseChunks(List<BibleVerse> verses)
    {
        var chunks = new List<BibleChunk>();

        foreach (var verse in verses.OrderBy(v => v.BookNumber).ThenBy(v => v.Chapter).ThenBy(v => v.Verse))
        {
            var chunk = new BibleChunk
            {
                Book = verse.Book,
                Chapter = verse.Chapter,
                StartVerse = verse.Verse,
                EndVerse = verse.Verse,
                Testament = verse.Testament,
                Translation = verse.Translation,
                Strategy = ChunkingStrategy.SingleVerse,
                // Include reference in text: "Psalm 23:1: The Lord is my shepherd..."
                Text = $"{verse.Reference}: {verse.Text}"
            };

            chunks.Add(chunk);
        }

        _logger.LogInformation("Created {Count} single-verse chunks", chunks.Count);
        return chunks;
    }

    /// <summary>
    /// Create verse chunks with context from previous/next verse for better understanding
    /// </summary>
    private List<BibleChunk> CreateVerseChunksWithOverlap(List<BibleVerse> verses)
    {
        var chunks = new List<BibleChunk>();
        
        // Group by book and chapter for proper context
        var groupedVerses = verses
            .OrderBy(v => v.BookNumber)
            .ThenBy(v => v.Chapter)
            .ThenBy(v => v.Verse)
            .GroupBy(v => (v.Book, v.Chapter))
            .ToList();

        foreach (var group in groupedVerses)
        {
            var sortedVerses = group.OrderBy(v => v.Verse).ToList();
            
            for (int i = 0; i < sortedVerses.Count; i++)
            {
                var currentVerse = sortedVerses[i];
                var previousVerse = i > 0 ? sortedVerses[i - 1] : null;
                var nextVerse = i < sortedVerses.Count - 1 ? sortedVerses[i + 1] : null;

                var chunk = new BibleChunk
                {
                    Book = currentVerse.Book,
                    Chapter = currentVerse.Chapter,
                    StartVerse = currentVerse.Verse,
                    EndVerse = currentVerse.Verse,
                    Testament = currentVerse.Testament,
                    Translation = currentVerse.Translation,
                    Strategy = ChunkingStrategy.VerseWithOverlap,
                    Text = $"{currentVerse.Reference}: {currentVerse.Text}",
                    ContextBefore = previousVerse != null ? $"[{previousVerse.Verse}] {previousVerse.Text}" : null,
                    ContextAfter = nextVerse != null ? $"[{nextVerse.Verse}] {nextVerse.Text}" : null
                };

                chunks.Add(chunk);
            }
        }

        _logger.LogInformation("Created {Count} verse chunks with overlap", chunks.Count);
        return chunks;
    }

    /// <summary>
    /// Create multi-verse chunks (original strategy - 3-5 verses grouped)
    /// </summary>
    private List<BibleChunk> CreateMultiVerseChunks(List<BibleVerse> verses, int chunkSize = 3)
    {
        var chunks = new List<BibleChunk>();

        // Group by book and chapter
        var groupedVerses = verses
            .GroupBy(v => (v.Book, v.Chapter))
            .OrderBy(g => verses.First(v => v.Book == g.Key.Book).BookNumber)
            .ThenBy(g => g.Key.Chapter);

        foreach (var group in groupedVerses)
        {
            var sortedVerses = group.OrderBy(v => v.Verse).ToList();
            
            // Create chunks of consecutive verses
            for (int i = 0; i < sortedVerses.Count; i += chunkSize)
            {
                var chunkVerses = sortedVerses.Skip(i).Take(chunkSize).ToList();
                
                if (!chunkVerses.Any()) continue;

                var chunk = new BibleChunk
                {
                    Book = group.Key.Book,
                    Chapter = group.Key.Chapter,
                    StartVerse = chunkVerses.First().Verse,
                    EndVerse = chunkVerses.Last().Verse,
                    Testament = chunkVerses.First().Testament,
                    Translation = chunkVerses.First().Translation,
                    Strategy = ChunkingStrategy.MultiVerse,
                    Text = string.Join(" ", chunkVerses.Select(v => $"[{v.Verse}] {v.Text}"))
                };

                chunks.Add(chunk);
            }
        }

        _logger.LogInformation("Created {Count} multi-verse chunks ({Size} verses each)", chunks.Count, chunkSize);
        return chunks;
    }

    /// <summary>
    /// Calculate cosine similarity between two vectors
    /// </summary>
    private static double CosineSimilarity(ReadOnlyMemory<float> vector1, ReadOnlyMemory<float> vector2)
    {
        var v1 = vector1.Span;
        var v2 = vector2.Span;

        if (v1.Length != v2.Length)
        {
            throw new ArgumentException("Vectors must have the same length");
        }

        double dotProduct = 0.0;
        double magnitude1 = 0.0;
        double magnitude2 = 0.0;

        for (int i = 0; i < v1.Length; i++)
        {
            dotProduct += v1[i] * v2[i];
            magnitude1 += v1[i] * v1[i];
            magnitude2 += v2[i] * v2[i];
        }

        magnitude1 = Math.Sqrt(magnitude1);
        magnitude2 = Math.Sqrt(magnitude2);

        if (magnitude1 == 0.0 || magnitude2 == 0.0)
        {
            return 0.0;
        }

        return dotProduct / (magnitude1 * magnitude2);
    }
}
