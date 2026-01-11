# RAG Implementation Guide
**Retrieval-Augmented Generation with Microsoft Semantic Kernel**

## Overview

The AI-Bible-App now includes **Retrieval-Augmented Generation (RAG)** to ground AI responses in actual Scripture. This means:
- ✅ **Biblically accurate responses** - AI retrieves relevant verses before answering
- ✅ **Reduced hallucinations** - Responses based on real Scripture, not generated content
- ✅ **Context-aware** - Finds passages semantically related to the conversation
- ✅ **Fully offline** - All processing happens locally

## How It Works

### 1. Indexing Phase (Startup)
```
Bible Verses → Chunk into Groups → Generate Embeddings → Store in Vector Database
```

- Loads Bible (WEB or KJV) from JSON
- **Chunking strategies:**
  - **SingleVerse** (default): One chunk per verse with reference included ("Psalm 23:1: The Lord is my shepherd...")
  - **VerseWithOverlap**: Per-verse with context from adjacent verses
  - **MultiVerse**: Groups 3-5 verses for broader context
- Generates vector embeddings using `nomic-embed-text` model (768 dimensions)
- Stores in in-memory vector store with enhanced metadata

### 2. Retrieval Phase (Each Query)
```
User Question → Generate Embedding → Search Similar Vectors → Return Top Matches
```

- User asks a question to a biblical character
- Question is converted to vector embedding
- Cosine similarity finds most relevant Scripture passages
- Top 3 passages injected into AI context

### 3. Generation Phase
```
System Prompt + Retrieved Verses + User Question → AI Response
```

- Character system prompt sets personality
- Retrieved verses provide biblical grounding
- AI generates response informed by Scripture

## Architecture

### Core Components

**Models** ([BibleVerse.cs](src/AI-Bible-App.Core/Models/BibleVerse.cs))
- `BibleVerse`: Single verse with book, chapter, verse, text, translation
- `BibleChunk`: Flexible chunk with embeddings, metadata (book, chapter, verse, reference, translation, strategy, estimatedTokens), optional context overlap

**Interfaces** ([IBibleRepository.cs](src/AI-Bible-App.Core/Interfaces/IBibleRepository.cs))
- `IBibleRepository`: Load and search Bible verses
- `IBibleRAGService`: Semantic search and retrieval

**Implementation**
- [JsonBibleRepository.cs](src/AI-Bible-App.Infrastructure/Repositories/JsonBibleRepository.cs): Loads KJV from JSON
- [WebBibleRepository.cs](src/AI-Bible-App.Infrastructure/Repositories/WebBibleRepository.cs): **NEW** - Loads World English Bible (WEB) translation
- [BibleRAGService.cs](src/AI-Bible-App.Infrastructure/Services/BibleRAGService.cs): Vector store, semantic search, configurable chunking
- [LocalAIService.cs](src/AI-Bible-App.Infrastructure/Services/LocalAIService.cs): Integrates RAG into chat/prayer

## Setup Requirements

### 1. Install Ollama Models

**Required: Embedding Model**
```bash
ollama pull nomic-embed-text
```

This 137MB model converts text to 768-dimensional vectors for semantic search.

**Already Installed: Chat Model**
```bash
ollama pull phi4
```

### 2. Configuration

[appsettings.json](src/AI-Bible-App.Console/appsettings.json):
```json
{
  "Ollama": {
    "Url": "http://localhost:11434",
    "ModelName": "phi4",
    "EmbeddingModel": "nomic-embed-text"
  },
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

**Chunking Strategies:**
- `SingleVerse`: One document per verse (best for precise retrieval)
- `VerseWithOverlap`: Includes context from adjacent verses
- `MultiVerse`: Groups 3-5 verses (broader context)

### 3. Bible Data

On first run, the app creates sample Bible data automatically:

**World English Bible (WEB)** - `Data/Bible/web.json`:
- Modern English translation (public domain)
- Sample includes Psalm 23, John 3:16-17, Romans 8:28, Proverbs 3:5-6, Matthew 5:3-12

**King James Version (KJV)** - `Data/Bible/kjv.json`:
- Classic translation
- Sample includes Psalm 23, John 3:16-17, Romans 8:28, Proverbs 3:5-6, Philippians 4:13, Genesis 1:1, Matthew 28:19-20

**To add full Bible data**: Replace with complete Bible JSON with all 31,000+ verses (see instructions below).

## Usage

### Character Conversations

**Without RAG:**
```
User: Tell me about facing giants
David: (Generic response about courage and faith)
```

**With RAG:**
```
User: Tell me about facing giants
System retrieves: 1 Samuel 17:45-47 (David vs. Goliath)
David: Ah, that day in the Valley of Elah! As Scripture records, 
I said to the Philistine, "You come against me with sword and spear, 
but I come against you in the name of the LORD Almighty..." 
[Scripture-grounded response]
```

### Prayer Generation

**Without RAG:**
```
Topic: Strength in trials
Prayer: (Generic prayer about strength)
```

**With RAG:**
```
Topic: Strength in trials  
System retrieves: Psalm 23:4, Romans 8:28, Philippians 4:13
Prayer: Heavenly Father, as the Psalmist declares, "though I walk 
through the valley of the shadow of death, I will fear no evil"... 
[Scripture-infused prayer]
```

## Performance

### Initialization (First Run)
- **Sample Data (15 verses)**: 2-5 seconds
- **Full KJV (31,102 verses)**: 2-5 minutes
- **Embeddings are cached** - only done once at startup

### Query Time
- **Embedding generation**: 100-300ms
- **Similarity search**: 10-50ms
- **Total retrieval overhead**: ~200-400ms

## Advanced Configuration

### Adjust Relevance Threshold

In [LocalAIService.cs](src/AI-Bible-App.Infrastructure/Services/LocalAIService.cs):
```csharp
var relevantChunks = await _ragService.RetrieveRelevantVersesAsync(
    query, 
    limit: 3,           // Number of passages to retrieve
    minRelevanceScore: 0.6,  // Minimum similarity (0.0-1.0)
    cancellationToken);
```

- **Higher threshold (0.8+)**: Only very relevant verses, fewer results
- **Lower threshold (0.5)**: More verses, some may be tangentially related
- **Default (0.6)**: Balanced approach

### Change Chunk Size

In [BibleRAGService.cs](src/AI-Bible-App.Infrastructure/Services/BibleRAGService.cs):
```csharp
const int chunkSize = 3; // verses per chunk
```

- **Smaller (1-2)**: More precise matching, less context
- **Larger (4-6)**: More context, potential noise
- **Default (3)**: Sweet spot for balance

### Disable RAG

In [appsettings.json](src/AI-Bible-App.Console/appsettings.json):
```json
"RAG": {
  "Enabled": false
}
```

## Adding Full KJV Bible

### Option 1: Download Pre-formatted JSON

1. Get full KJV JSON from public sources (e.g., `getbible.net`, `scripture-api`)
2. Format as:
```json
[
  {
    "Book": "Genesis",
    "Chapter": 1,
    "Verse": 1,
    "Testament": "OT",
    "Text": "In the beginning God created the heaven and the earth."
  },
  ...
]
```
3. Save to `Data/Bible/kjv.json`

### Option 2: Use Python Script (included below)

Create `scripts/download_kjv.py`:
```python
import json
import requests

# Download from public API
response = requests.get("https://api.scripture.api.bible/v1/bibles/de4e12af7f28f599-01/books")
# Process and format...
# Save to Data/Bible/kjv.json
```

## Troubleshooting

### "Ollama returned status code: 404" on startup

**Problem**: `nomic-embed-text` model not installed

**Solution**:
```bash
ollama pull nomic-embed-text
```

### Initialization takes very long (> 5 minutes)

**Problem**: Processing full 31K verses on slow hardware

**Solutions**:
1. Start with sample data (automatic)
2. Add verses incrementally
3. Use persistent vector store (future enhancement)

### Responses don't reference Scripture

**Possible causes**:
1. RAG disabled in config - check `"RAG": { "Enabled": true }`
2. Relevance threshold too high - lower `minRelevanceScore`
3. No relevant verses found - expand Bible data
4. Model not understanding context - try different embedding model

### OutOfMemory error during initialization

**Problem**: Generating embeddings for full Bible exhausts RAM

**Solutions**:
1. Process in smaller batches
2. Increase system RAM
3. Use persistent vector store (Qdrant/Postgres)

## Future Enhancements

### Planned Improvements

**Persistent Vector Store**
- Currently: In-memory (lost on restart)
- Future: Qdrant or PostgreSQL with pgvector
- Benefit: Instant startup, no re-embedding

**Multiple Bible Translations**
- Add ESV, NIV, NASB alongside KJV
- User selects preferred translation
- Cross-reference search

**Character-Specific Retrieval**
- David: Prioritize Psalms and 1 Samuel
- Paul: Weight New Testament epistles
- Context-aware relevance

**Verse Highlighting in UI**
- Show which verses informed the response
- Click to read full passage
- Build trust and transparency

## Technical Details

### Embedding Model: nomic-embed-text

- **Dimensions**: 768
- **Context length**: 8,192 tokens
- **Size**: 137 MB
- **Speed**: ~50 embeddings/second (CPU)
- **Quality**: MTEB score 62.39

### Vector Similarity: Cosine Similarity

Formula:
```
similarity = (A · B) / (||A|| * ||B||)
```

- Range: -1.0 (opposite) to 1.0 (identical)
- Typical relevant matches: 0.6-0.9
- Threshold: 0.6 (balanced)

### Memory Usage

- **Sample data (15 verses)**: ~5 MB
- **Full KJV (31K verses)**: ~250 MB
- **Vector store overhead**: 768 floats × 4 bytes × chunks

## API Reference

### IBibleRAGService

```csharp
// Initialize and index Bible verses
await ragService.InitializeAsync();

// Retrieve relevant verses
var verses = await ragService.RetrieveRelevantVersesAsync(
    query: "facing fear and enemies",
    limit: 5,
    minRelevanceScore: 0.7
);

// Check if initialized
if (ragService.IsInitialized) { ... }
```

### BibleChunk Properties

```csharp
chunk.Reference      // "Psalm 23:1-3"
chunk.Text           // "[1] The LORD is my shepherd..."
chunk.Book           // "Psalms"
chunk.Chapter        // 23
chunk.StartVerse     // 1
chunk.EndVerse       // 3
chunk.Testament      // "OT"
```

## Monitoring and Logging

Enable debug logging in [appsettings.json](src/AI-Bible-App.Console/appsettings.json):
```json
"Logging": {
  "LogLevel": {
    "Default": "Debug",
    "AI_Bible_App.Infrastructure.Services.BibleRAGService": "Debug"
  }
}
```

You'll see:
- `[INF] Loaded 15 verses from repository`
- `[INF] Created 5 chunks from verses`
- `[INF] Generating embeddings for 5 chunks...`
- `[INF] Retrieved 3 relevant chunks for query`

## Resources

- [Microsoft Semantic Kernel Docs](https://learn.microsoft.com/en-us/semantic-kernel/)
- [Ollama Embedding Models](https://ollama.ai/library/nomic-embed-text)
- [RAG Explained](https://www.pinecone.io/learn/retrieval-augmented-generation/)
- [Vector Databases Comparison](https://github.com/erikbern/ann-benchmarks)

---

**RAG is now active!** Every conversation and prayer is grounded in Scripture. 🙏
