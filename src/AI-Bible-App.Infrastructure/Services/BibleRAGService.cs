using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.Ollama;
using OllamaSharp;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0070

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// RAG service for retrieving relevant Bible verses using semantic search
/// with intelligent fallback to keyword search when embeddings fail
/// </summary>
public class BibleRAGService : IBibleRAGService
{
    private readonly IBibleRepository _bibleRepository;
    private readonly ILogger<BibleRAGService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _ollamaUrl;
    private readonly string _embeddingModel;
    private readonly ChunkingStrategy _chunkingStrategy;
    private ITextEmbeddingGenerationService? _embeddingService;
    private readonly Dictionary<string, (BibleChunk Chunk, ReadOnlyMemory<float> Embedding)> _vectorStore;
    private List<BibleChunk>? _allChunks; // For keyword fallback
    private bool _isInitialized;
    private bool _embeddingsAvailable = true;
    private SearchStatistics _lastSearchStats = new();
    private readonly int _embeddingParallelism;
    
    // Embedding persistence
    private readonly string _embeddingCachePath;
    private readonly SemaphoreSlim _persistLock = new(1, 1);

    public bool IsInitialized => _isInitialized;
    public SearchStatistics LastSearchStats => _lastSearchStats;

    // Common biblical keywords for better keyword matching
    private static readonly HashSet<string> _biblicalKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "faith", "hope", "love", "grace", "mercy", "forgiveness", "salvation", "sin", "redemption",
        "prayer", "praise", "worship", "trust", "fear", "peace", "joy", "comfort", "strength",
        "wisdom", "truth", "light", "darkness", "life", "death", "resurrection", "cross",
        "covenant", "promise", "blessing", "curse", "righteousness", "holiness", "spirit",
        "kingdom", "heaven", "hell", "judgment", "healing", "miracle", "prophecy", "angel"
    };

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
        _embeddingParallelism = int.TryParse(configuration["RAG:EmbeddingParallelism"], out var parallelism)
            ? Math.Max(1, parallelism)
            : Math.Max(2, Environment.ProcessorCount);
        
        // Set up embedding cache path
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _embeddingCachePath = Path.Combine(appData, "AI-Bible-App", "embeddings_cache.json");
        
        // Get chunking strategy from config
        var strategyStr = configuration["RAG:ChunkingStrategy"] ?? "SingleVerse";
        _chunkingStrategy = Enum.Parse<ChunkingStrategy>(strategyStr, ignoreCase: true);
        
        // Try to initialize embedding service
        try
        {
            var ollamaClient = new OllamaApiClient(new Uri(_ollamaUrl))
            {
                SelectedModel = _embeddingModel
            };
            _embeddingService = ollamaClient.AsTextEmbeddingGenerationService();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not initialize embedding service. Will use keyword fallback only.");
            _embeddingsAvailable = false;
        }

        _logger.LogInformation(
            "BibleRAGService created with embedding model: {Model} at {Url}, Chunking: {Strategy}, Embeddings: {Available}, CachePath: {CachePath}", 
            _embeddingModel, 
            _ollamaUrl,
            _chunkingStrategy,
            _embeddingsAvailable ? "Available" : "Fallback Only",
            _embeddingCachePath);
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
            var stopwatch = Stopwatch.StartNew();

            // Load all Bible verses
            var verses = await _bibleRepository.LoadAllVersesAsync(cancellationToken);
            _logger.LogInformation("Loaded {Count} verses from repository", verses.Count);

            // Chunk verses into meaningful groups
            var chunks = CreateChunks(verses);
            _allChunks = chunks; // Store for keyword fallback
            _logger.LogInformation("Created {Count} chunks from verses", chunks.Count);

            // Try to load cached embeddings first
            var loadedFromCache = await LoadEmbeddingsCacheAsync(chunks, cancellationToken);
            
            // Only generate embeddings if not loaded from cache and service is available
            if (!loadedFromCache && _embeddingsAvailable && _embeddingService != null)
            {
                try
                {
                    _logger.LogInformation("Generating embeddings for {Count} chunks (this may take a few minutes on first run)...", chunks.Count);
                    using var semaphore = new SemaphoreSlim(_embeddingParallelism);
                    var embeddingTasks = chunks.Select(async chunk =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
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
                        finally
                        {
                            semaphore.Release();
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
                    
                    _logger.LogInformation("Generated {Count} embeddings successfully", _vectorStore.Count);
                    
                    // Persist embeddings to disk for faster future startups
                    await PersistEmbeddingsCacheAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Embedding generation failed. Will use keyword fallback.");
                    _embeddingsAvailable = false;
                }
            }
            else if (!loadedFromCache)
            {
                _logger.LogInformation("Embeddings not available - using keyword search only");
            }

            stopwatch.Stop();
            _isInitialized = true;
            _logger.LogInformation(
                "BibleRAGService initialized in {Duration}ms: {Embeddings} embeddings, {Chunks} chunks available for keyword search, LoadedFromCache: {FromCache}", 
                stopwatch.ElapsedMilliseconds,
                _vectorStore.Count,
                _allChunks?.Count ?? 0,
                loadedFromCache);
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
        SearchStrictness strictness = SearchStrictness.Balanced,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var stats = new SearchStatistics
        {
            Query = query,
            StrictnessUsed = strictness
        };

        if (!_isInitialized)
        {
            _logger.LogWarning("BibleRAGService not initialized. Returning empty results.");
            _lastSearchStats = stats;
            return new List<BibleChunk>();
        }

        // Adjust thresholds based on strictness
        var effectiveMinScore = strictness switch
        {
            SearchStrictness.Strict => minRelevanceScore,
            SearchStrictness.Balanced => Math.Max(0.5, minRelevanceScore - 0.1),
            SearchStrictness.Relaxed => Math.Max(0.3, minRelevanceScore - 0.2),
            _ => minRelevanceScore
        };

        var results = new List<(BibleChunk Chunk, double Score, bool FromSemantic)>();

        // Try semantic search first if embeddings available
        if (_embeddingsAvailable && _vectorStore.Count > 0 && _embeddingService != null)
        {
            try
            {
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: cancellationToken);

                var semanticResults = _vectorStore.Select(kvp =>
                {
                    var similarity = CosineSimilarity(queryEmbedding, kvp.Value.Embedding);
                    return (Chunk: kvp.Value.Chunk, Similarity: similarity);
                })
                .Where(x => x.Similarity >= effectiveMinScore)
                .OrderByDescending(x => x.Similarity)
                .Take(limit * 2) // Get extra for merging with keyword results
                .ToList();

                foreach (var r in semanticResults)
                {
                    results.Add((r.Chunk, r.Similarity, true));
                }
                
                stats.SemanticResults = semanticResults.Count;
                _logger.LogInformation("Semantic search found {Count} results", semanticResults.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Semantic search failed, falling back to keyword search");
                stats.UsedFallback = true;
            }
        }

        // Fallback to keyword search if:
        // 1. Embeddings not available
        // 2. Semantic search returned too few results
        // 3. Strictness is not Strict
        var needsKeywordFallback = strictness != SearchStrictness.Strict && 
            (results.Count < limit || !_embeddingsAvailable);

        if (needsKeywordFallback && _allChunks != null)
        {
            stats.UsedFallback = true;
            var keywordResults = PerformKeywordSearch(query, limit * 2, effectiveMinScore);
            
            foreach (var r in keywordResults)
            {
                // Don't add duplicates
                if (!results.Any(existing => existing.Chunk.Id == r.Chunk.Id))
                {
                    results.Add((r.Chunk, r.Score, false));
                }
            }
            
            stats.KeywordFallbackResults = keywordResults.Count;
            _logger.LogInformation("Keyword fallback added {Count} results", keywordResults.Count);
        }

        // Combine and sort results
        var finalResults = results
            .OrderByDescending(r => r.Score)
            .Take(limit)
            .Select(r => r.Chunk)
            .ToList();

        // Update statistics
        stopwatch.Stop();
        stats.TotalResults = finalResults.Count;
        stats.SearchDuration = stopwatch.Elapsed;
        if (results.Any())
        {
            stats.HighestScore = results.Max(r => r.Score);
            stats.LowestScore = results.Min(r => r.Score);
        }
        _lastSearchStats = stats;

        _logger.LogInformation(
            "Search completed: {Total} results ({Semantic} semantic, {Keyword} keyword) in {Duration}ms",
            stats.TotalResults, stats.SemanticResults, stats.KeywordFallbackResults, 
            stats.SearchDuration.TotalMilliseconds);

        return finalResults;
    }

    /// <summary>
    /// Perform keyword-based search as fallback when embeddings fail
    /// </summary>
    private List<(BibleChunk Chunk, double Score)> PerformKeywordSearch(string query, int limit, double minScore)
    {
        if (_allChunks == null || _allChunks.Count == 0)
            return new List<(BibleChunk, double)>();

        // Extract keywords from query
        var queryWords = ExtractKeywords(query);
        if (queryWords.Count == 0)
            return new List<(BibleChunk, double)>();

        var scored = new List<(BibleChunk Chunk, double Score)>();

        foreach (var chunk in _allChunks)
        {
            var score = CalculateKeywordScore(chunk.Text, queryWords);
            if (score >= minScore)
            {
                scored.Add((chunk, score));
            }
        }

        return scored
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Extract meaningful keywords from a query
    /// </summary>
    private List<string> ExtractKeywords(string query)
    {
        // Remove common stop words and extract meaningful terms
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "is", "are", "was", "were", "be", "been", "being",
            "have", "has", "had", "do", "does", "did", "will", "would", "could",
            "should", "may", "might", "must", "shall", "can", "need", "dare",
            "to", "of", "in", "for", "on", "with", "at", "by", "from", "as",
            "into", "through", "during", "before", "after", "above", "below",
            "between", "under", "again", "further", "then", "once", "here",
            "there", "when", "where", "why", "how", "all", "each", "few",
            "more", "most", "other", "some", "such", "no", "nor", "not", "only",
            "own", "same", "so", "than", "too", "very", "just", "and", "but",
            "if", "or", "because", "until", "while", "about", "what", "which",
            "who", "whom", "this", "that", "these", "those", "am", "i", "me",
            "my", "myself", "we", "our", "ours", "ourselves", "you", "your",
            "yours", "yourself", "yourselves", "he", "him", "his", "himself",
            "she", "her", "hers", "herself", "it", "its", "itself", "they",
            "them", "their", "theirs", "themselves"
        };

        var words = Regex.Split(query.ToLowerInvariant(), @"\W+")
            .Where(w => w.Length > 2 && !stopWords.Contains(w))
            .Distinct()
            .ToList();

        // Boost biblical keywords
        var boosted = words
            .OrderByDescending(w => _biblicalKeywords.Contains(w) ? 1 : 0)
            .ToList();

        return boosted;
    }

    /// <summary>
    /// Calculate a relevance score based on keyword matching
    /// </summary>
    private double CalculateKeywordScore(string text, List<string> keywords)
    {
        if (keywords.Count == 0) return 0;

        var textLower = text.ToLowerInvariant();
        var matchCount = 0;
        var exactMatchBonus = 0.0;

        foreach (var keyword in keywords)
        {
            if (textLower.Contains(keyword))
            {
                matchCount++;
                
                // Bonus for biblical keywords
                if (_biblicalKeywords.Contains(keyword))
                {
                    exactMatchBonus += 0.1;
                }
                
                // Bonus for exact word match (not just substring)
                if (Regex.IsMatch(textLower, $@"\b{Regex.Escape(keyword)}\b"))
                {
                    exactMatchBonus += 0.05;
                }
            }
        }

        var baseScore = (double)matchCount / keywords.Count;
        return Math.Min(1.0, baseScore + exactMatchBonus);
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

    #region Embedding Cache Persistence

    /// <summary>
    /// Load embeddings from disk cache if available and valid
    /// </summary>
    private async Task<bool> LoadEmbeddingsCacheAsync(List<BibleChunk> chunks, CancellationToken cancellationToken)
    {
        await _persistLock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_embeddingCachePath))
            {
                _logger.LogInformation("No embedding cache found at {Path}", _embeddingCachePath);
                return false;
            }

            var json = await File.ReadAllTextAsync(_embeddingCachePath, cancellationToken);
            var cacheData = JsonSerializer.Deserialize<EmbeddingCacheData>(json);

            if (cacheData == null || cacheData.Entries == null || cacheData.Entries.Count == 0)
            {
                _logger.LogWarning("Embedding cache is empty or invalid");
                return false;
            }

            // Validate cache metadata
            if (cacheData.EmbeddingModel != _embeddingModel || 
                cacheData.ChunkingStrategy != _chunkingStrategy.ToString())
            {
                _logger.LogWarning("Embedding cache was created with different settings (model: {CachedModel} vs {CurrentModel}, strategy: {CachedStrategy} vs {CurrentStrategy}). Regenerating...",
                    cacheData.EmbeddingModel, _embeddingModel, cacheData.ChunkingStrategy, _chunkingStrategy);
                return false;
            }

            // Check if chunks match (same count and chunking strategy)
            if (cacheData.ChunkCount != chunks.Count)
            {
                _logger.LogWarning("Embedding cache has different chunk count ({Cached} vs {Current}). Regenerating...",
                    cacheData.ChunkCount, chunks.Count);
                return false;
            }

            // Build chunk lookup for fast matching
            var chunkLookup = chunks.ToDictionary(c => c.Id, c => c);

            var loadedCount = 0;
            foreach (var entry in cacheData.Entries)
            {
                if (entry.ChunkId != null && chunkLookup.TryGetValue(entry.ChunkId, out var chunk) && entry.Embedding != null)
                {
                    var embedding = new ReadOnlyMemory<float>(entry.Embedding);
                    _vectorStore[chunk.Id] = (chunk, embedding);
                    loadedCount++;
                }
            }

            _logger.LogInformation("Loaded {Count} embeddings from cache (created: {Created})", 
                loadedCount, cacheData.CreatedAt);
            return loadedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load embedding cache. Will regenerate embeddings.");
            return false;
        }
        finally
        {
            _persistLock.Release();
        }
    }

    /// <summary>
    /// Persist embeddings to disk for faster future startups
    /// </summary>
    private async Task PersistEmbeddingsCacheAsync(CancellationToken cancellationToken)
    {
        await _persistLock.WaitAsync(cancellationToken);
        try
        {
            var dir = Path.GetDirectoryName(_embeddingCachePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var entries = _vectorStore.Select(kvp => new EmbeddingCacheEntry
            {
                ChunkId = kvp.Key,
                Embedding = kvp.Value.Embedding.ToArray()
            }).ToList();

            var cacheData = new EmbeddingCacheData
            {
                CreatedAt = DateTime.UtcNow,
                EmbeddingModel = _embeddingModel,
                ChunkingStrategy = _chunkingStrategy.ToString(),
                ChunkCount = _allChunks?.Count ?? 0,
                Entries = entries
            };

            var json = JsonSerializer.Serialize(cacheData, new JsonSerializerOptions 
            { 
                WriteIndented = false 
            });
            
            await File.WriteAllTextAsync(_embeddingCachePath, json, cancellationToken);
            
            var fileSize = new FileInfo(_embeddingCachePath).Length / 1024.0 / 1024.0;
            _logger.LogInformation("Persisted {Count} embeddings to cache ({Size:F2} MB)", entries.Count, fileSize);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist embedding cache");
        }
        finally
        {
            _persistLock.Release();
        }
    }

    /// <summary>
    /// Clear the embedding cache (useful for testing or model updates)
    /// </summary>
    public void ClearEmbeddingCache()
    {
        try
        {
            if (File.Exists(_embeddingCachePath))
            {
                File.Delete(_embeddingCachePath);
                _logger.LogInformation("Embedding cache cleared");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear embedding cache");
        }
    }

    /// <summary>
    /// Get embedding cache statistics
    /// </summary>
    public EmbeddingCacheStats GetEmbeddingCacheStats()
    {
        var stats = new EmbeddingCacheStats
        {
            CacheExists = File.Exists(_embeddingCachePath),
            CachePath = _embeddingCachePath,
            EmbeddingsInMemory = _vectorStore.Count
        };

        if (stats.CacheExists)
        {
            var fileInfo = new FileInfo(_embeddingCachePath);
            stats.CacheSizeBytes = fileInfo.Length;
            stats.CacheLastModified = fileInfo.LastWriteTimeUtc;
        }

        return stats;
    }

    #endregion

    #region Cache Data Classes

    private class EmbeddingCacheData
    {
        public DateTime CreatedAt { get; set; }
        public string? EmbeddingModel { get; set; }
        public string? ChunkingStrategy { get; set; }
        public int ChunkCount { get; set; }
        public List<EmbeddingCacheEntry>? Entries { get; set; }
    }

    private class EmbeddingCacheEntry
    {
        public string? ChunkId { get; set; }
        public float[]? Embedding { get; set; }
    }

    #endregion
}

/// <summary>
/// Statistics about the embedding cache
/// </summary>
public class EmbeddingCacheStats
{
    public bool CacheExists { get; set; }
    public string? CachePath { get; set; }
    public long CacheSizeBytes { get; set; }
    public DateTime? CacheLastModified { get; set; }
    public int EmbeddingsInMemory { get; set; }
    
    public double CacheSizeMB => CacheSizeBytes / 1024.0 / 1024.0;
}
