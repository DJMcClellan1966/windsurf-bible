# World English Bible (WEB) Addition

## Overview

The AI-Bible-App now supports **multiple Bible translations**, with the **World English Bible (WEB)** as the default translation. WEB is a modern English translation in the public domain.

## Features

### ✅ Multiple Translation Support
- **WEB (World English Bible)**: Modern English, public domain
- **KJV (King James Version)**: Classic translation for historical context
- Easily switch between translations via configuration

### ✅ Enhanced Chunking Strategies
Three configurable chunking strategies optimized for different retrieval needs:

#### 1. SingleVerse (Default - Recommended)
- **One document per verse** for precise retrieval
- **Format**: "Psalm 23:1: The Lord is my shepherd, I shall not want."
- **Metadata**: Book, chapter, verse, reference, translation, strategy, estimatedTokens
- **Use case**: Best for exact verse lookup and focused context

#### 2. VerseWithOverlap
- Per-verse chunks **with context from adjacent verses**
- **Properties**: `ContextBefore`, `ContextAfter` for surrounding verses
- **Use case**: When you need verse-level precision but want surrounding context

#### 3. MultiVerse
- **Groups 3-5 verses** for broader context
- **Use case**: When you need paragraph-level understanding

### ✅ Rich Metadata
Every chunk includes:
- `Book`: "Psalms"
- `Chapter`: 23
- `Verse`: 1
- `Reference`: "Psalm 23:1"
- `Translation`: "WEB" or "KJV"
- `ChunkingStrategy`: Strategy used to create chunk
- `EstimatedTokens`: Approximate token count for LLM context management

## Configuration

### appsettings.json
```json
{
  "RAG": {
    "Enabled": true,
    "ChunkingStrategy": "SingleVerse"
  },
  "Bible": {
    "DataPath": "Data/Bible/kjv.json",
    "WebDataPath": "Data/Bible/web.json",
    "DefaultTranslation": "WEB"
  }
}
```

### Switch Translations
Change `DefaultTranslation` to:
- `"WEB"` - World English Bible (modern English)
- `"KJV"` - King James Version (classic English)

### Change Chunking Strategy
Set `ChunkingStrategy` to:
- `"SingleVerse"` - One verse per chunk (recommended)
- `"VerseWithOverlap"` - Verse + surrounding context
- `"MultiVerse"` - 3-5 verse groups

## Architecture

### New Components

**WebBibleRepository.cs**
- Implements `IBibleRepository`
- Loads World English Bible translation
- Creates sample WEB data with 18 verses on first run
- Located: `src/AI-Bible-App.Infrastructure/Repositories/WebBibleRepository.cs`

**Enhanced BibleVerse.cs Models**
- `BibleVerse.Translation`: "WEB" or "KJV"
- `BibleVerse.BookNumber`: For canonical ordering
- `BibleChunk.ContextBefore/After`: Adjacent verse context
- `BibleChunk.EstimatedTokens`: Token estimation
- `ChunkingStrategy` enum: SingleVerse, VerseWithOverlap, MultiVerse

**Updated BibleRAGService.cs**
- `CreateSingleVerseChunks()`: One chunk per verse with reference
- `CreateVerseChunksWithOverlap()`: Adds surrounding verse context
- `CreateMultiVerseChunks()`: Original 3-5 verse grouping
- Configurable strategy selection from appsettings

## Sample Data

### WEB Translation (18 verses)
```
Psalm 23:1-6 (complete psalm)
John 3:16-17
Romans 8:28
Proverbs 3:5-6
Matthew 5:3-10 (Beatitudes)
```

### Chunk Format Example
```json
{
  "Id": "psalm_23_1_web",
  "Content": "Psalm 23:1: Yahweh is my shepherd; I shall lack nothing.",
  "Metadata": {
    "book": "Psalms",
    "chapter": "23",
    "verse": "1",
    "reference": "Psalm 23:1",
    "translation": "WEB",
    "chunkingStrategy": "SingleVerse",
    "estimatedTokens": "15"
  }
}
```

## Benefits

### 🎯 Precision Retrieval
Single-verse chunks allow for exact verse matching without irrelevant context.

### 📚 Modern Language
WEB uses contemporary English that's easier for modern readers to understand while maintaining accuracy.

### 🔍 Better Filtering
Enhanced metadata enables:
- Filter by book/chapter/verse
- Filter by translation
- Display verse references in UI
- Track token usage

### 🔄 Context Flexibility
Choose chunking strategy based on use case:
- **SingleVerse**: Q&A, verse lookup, precise citations
- **VerseWithOverlap**: Teaching, explanations with context
- **MultiVerse**: Thematic search, narrative understanding

## Usage Example

### Character Conversation with WEB
```
User: What does the Bible say about God as our shepherd?

System:
- Generates embedding for question
- Searches WEB verse chunks
- Finds: "Psalm 23:1: Yahweh is my shepherd; I shall lack nothing."
- Injects into character context

David: Ah, my friend! That precious Psalm I wrote speaks of Yahweh 
as our shepherd. "Yahweh is my shepherd; I shall lack nothing." 
(Psalm 23:1) As a shepherd boy myself, I knew the care and protection 
a good shepherd provides...
```

## Future Enhancements

### Full Bible Data
- Current: 18 sample verses
- Full WEB: ~31,000 verses
- Download full WEB JSON from public domain sources

### Additional Translations
- ESV (English Standard Version)
- NIV (New International Version)
- NASB (New American Standard Bible)
- Translation comparison mode

### Advanced Features
- **Hybrid parent-child structure**: Verse chunks + chapter summaries
- **Cross-reference linking**: Connect related passages
- **Topical indexing**: Pre-indexed themes (love, faith, hope)
- **Multi-translation retrieval**: Compare same verse across translations

## Technical Details

### Token Estimation
```csharp
EstimatedTokens = (int)Math.Ceiling(fullText.Length / 4.0)
```
Approximates tokens as 1 token ≈ 4 characters (conservative estimate for English text).

### Vector Embeddings
- Model: `nomic-embed-text`
- Dimensions: 768
- Similarity: Cosine similarity with 0.6 minimum relevance threshold

### Memory Footprint
- Sample (18 verses): <1KB JSON
- Full Bible (~31K verses): ~5MB JSON
- Embeddings: 768 floats × verses = ~95MB for full Bible

## Implementation Notes

✅ **Completed:**
- WebBibleRepository with sample data
- Three chunking strategies
- Enhanced metadata system
- Configuration support
- Program.cs registration with translation selection

📝 **Next Steps:**
1. Add full WEB Bible JSON data (31K verses)
2. Test all chunking strategies with real queries
3. Benchmark retrieval performance
4. Add translation selection UI
5. Document full Bible data sources

## References

- **World English Bible**: https://worldenglish.bible/
- **Semantic Kernel**: https://learn.microsoft.com/semantic-kernel/
- **Ollama Embeddings**: https://ollama.com/library/nomic-embed-text
- **RAG Implementation**: See [RAG_IMPLEMENTATION.md](RAG_IMPLEMENTATION.md)
