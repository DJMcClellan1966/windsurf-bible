namespace AI_Bible_App.Core.Models;

/// <summary>
/// Represents a single Bible verse with its reference and text
/// </summary>
public class BibleVerse
{
    public string Book { get; set; } = string.Empty;
    public int Chapter { get; set; }
    public int Verse { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Translation { get; set; } = "KJV";
    
    /// <summary>
    /// Full reference (e.g., "John 3:16")
    /// </summary>
    public string Reference => $"{Book} {Chapter}:{Verse}";
    
    /// <summary>
    /// Complete verse with reference (e.g., "John 3:16: For God so loved the world...")
    /// </summary>
    public string FullText => $"{Reference}: {Text}";
    
    /// <summary>
    /// Testament this verse belongs to
    /// </summary>
    public string Testament { get; set; } = string.Empty;
    
    /// <summary>
    /// Book number (1-66) for ordering
    /// </summary>
    public int BookNumber { get; set; }
}

/// <summary>
/// Chunking strategy for Bible text
/// </summary>
public enum ChunkingStrategy
{
    /// <summary>
    /// One verse per chunk with reference included
    /// </summary>
    SingleVerse,
    
    /// <summary>
    /// Multiple verses grouped together (e.g., 3-5 verses)
    /// </summary>
    MultiVerse,
    
    /// <summary>
    /// Single verse with context from previous/next verse
    /// </summary>
    VerseWithOverlap
}

/// <summary>
/// Represents a chunk of Bible text for semantic search
/// </summary>
public class BibleChunk
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Book { get; set; } = string.Empty;
    public int Chapter { get; set; }
    public int StartVerse { get; set; }
    public int EndVerse { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Testament { get; set; } = string.Empty;
    public string Translation { get; set; } = "KJV";
    public ChunkingStrategy Strategy { get; set; } = ChunkingStrategy.SingleVerse;
    
    /// <summary>
    /// Context from surrounding verses (for overlap strategy)
    /// </summary>
    public string? ContextBefore { get; set; }
    public string? ContextAfter { get; set; }
    
    /// <summary>
    /// Reference range (e.g., "Psalm 23:1" or "Psalm 23:1-3")
    /// </summary>
    public string Reference => StartVerse == EndVerse 
        ? $"{Book} {Chapter}:{StartVerse}" 
        : $"{Book} {Chapter}:{StartVerse}-{EndVerse}";
    
    /// <summary>
    /// Full text with reference prefix
    /// </summary>
    public string FullText => $"{Reference}: {Text}";
    
    /// <summary>
    /// Token count estimate (rough approximation)
    /// </summary>
    public int EstimatedTokens => (Text.Length + (ContextBefore?.Length ?? 0) + (ContextAfter?.Length ?? 0)) / 4;
    
    /// <summary>
    /// Metadata for the chunk (for filtering and display)
    /// </summary>
    public Dictionary<string, object> Metadata => new()
    {
        { "book", Book },
        { "chapter", Chapter },
        { "startVerse", StartVerse },
        { "endVerse", EndVerse },
        { "testament", Testament },
        { "reference", Reference },
        { "translation", Translation },
        { "strategy", Strategy.ToString() },
        { "estimatedTokens", EstimatedTokens }
    };
}
