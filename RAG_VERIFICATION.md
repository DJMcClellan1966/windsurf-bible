# RAG Setup Verification Checklist

Use this checklist to ensure RAG is properly configured and working.

## ✅ Prerequisites

- [ ] **Ollama installed**
  ```bash
  ollama --version
  ```
  Should show version 0.1.x or higher

- [ ] **Phi-4 model downloaded**
  ```bash
  ollama list | grep phi4
  ```
  Should show: `phi4:latest`

- [ ] **Nomic-embed-text model downloaded** (NEW for RAG)
  ```bash
  ollama list | grep nomic
  ```
  Should show: `nomic-embed-text:latest`
  
  If missing, install:
  ```bash
  ollama pull nomic-embed-text
  ```

- [ ] **.NET 8.0 SDK installed**
  ```bash
  dotnet --version
  ```
  Should show 8.0.x

## ✅ Build Verification

- [ ] **Project builds successfully**
  ```bash
  cd "c:\Users\DJMcC\OneDrive\Desktop\AI-Bible-app\AI-Bible-app"
  dotnet build
  ```
  Should show: "Build succeeded"

- [ ] **All packages restored**
  Check for:
  - `Microsoft.SemanticKernel` (1.34.0)
  - `Microsoft.SemanticKernel.Connectors.Ollama` (1.34.0-alpha)
  - `OllamaSharp` (4.0.17)

## ✅ Configuration Check

- [ ] **appsettings.json has RAG config**
  
  Open: `src/AI-Bible-App.Console/appsettings.json`
  
  Verify:
  ```json
  {
    "Ollama": {
      "Url": "http://localhost:11434",
      "ModelName": "phi4",
      "EmbeddingModel": "nomic-embed-text"  ← NEW
    },
    "RAG": {
      "Enabled": true  ← NEW
    },
    "Bible": {
      "DataPath": "Data/Bible/kjv.json"  ← NEW
    }
  }
  ```

## ✅ First Run Test

- [ ] **Start Ollama service**
  - Windows: Check System Tray for Ollama icon
  - Mac/Linux: `ollama serve`

- [ ] **Run the application**
  ```bash
  cd src/AI-Bible-App.Console
  dotnet run
  ```

- [ ] **Watch for RAG initialization message**
  ```
  Initializing Scripture search (RAG)...
  ```
  
  Expected logs:
  ```
  [INF] JsonBibleRepository initialized with data path: Data/Bible/kjv.json
  [INF] BibleRAGService created with embedding model: nomic-embed-text at http://localhost:11434
  [INF] Loaded 15 verses from repository
  [INF] Created 5 chunks from verses
  [INF] Generating embeddings for 5 chunks...
  ```

- [ ] **Successful initialization confirmation**
  ```
  ✓ Scripture search initialized successfully
  ```

## ✅ Functional Tests

### Test 1: Character Chat with RAG

- [ ] Select option `1` (Talk with a Biblical Character)
- [ ] Choose `1` (David)
- [ ] Ask: "Tell me about facing Goliath"
- [ ] **Expected**: David's response should reference the actual biblical account
- [ ] Check logs for: `[INF] Retrieved X relevant Scripture passages for query`

### Test 2: Prayer with RAG

- [ ] Select option `2` (Generate a Personalized Prayer)
- [ ] Topic: "strength in difficult times"
- [ ] **Expected**: Prayer should incorporate themes from Psalms, Isaiah, or Romans
- [ ] Check logs for: `[INF] Added RAG context to prayer generation`

### Test 3: RAG Disabled

- [ ] Stop the app (Ctrl+C)
- [ ] Edit `appsettings.json`: `"RAG": { "Enabled": false }`
- [ ] Run again: `dotnet run`
- [ ] **Expected**: No RAG initialization message
- [ ] **Expected**: App works normally (without Scripture retrieval)
- [ ] Re-enable RAG: `"Enabled": true`

## ✅ Performance Check

- [ ] **Initialization time** (sample data): 2-10 seconds
  - If > 15 seconds, check Ollama is running
  
- [ ] **First response time**: 3-10 seconds
  - Includes model loading time
  
- [ ] **Subsequent responses**: 1-3 seconds
  - With RAG overhead ~200-400ms

## 🔍 Troubleshooting

### Issue: "Model 'nomic-embed-text' not found"

**Solution:**
```bash
ollama pull nomic-embed-text
```

### Issue: "Connection refused" to Ollama

**Solution:**
- Ensure Ollama is running
- Windows: Start from Start Menu
- Mac/Linux: `ollama serve`

### Issue: Initialization hangs at "Generating embeddings"

**Possible Causes:**
1. Ollama service not responding
2. Model not downloaded
3. System low on resources

**Solutions:**
```bash
# Test Ollama directly
ollama run nomic-embed-text "test"

# Check Ollama logs
ollama logs

# Restart Ollama
# Windows: Restart from System Tray
# Mac/Linux: Kill process and restart
```

### Issue: Responses don't mention Scripture

**Check:**
1. RAG is enabled in config
2. Initialization succeeded
3. Bible data file exists: `Data/Bible/kjv.json`
4. Query has relevant verses (try broader topics)

## ✅ Advanced Verification

### Check Vector Store Population

Look for this log after initialization:
```
[INF] BibleRAGService initialized successfully with X indexed chunks
```

X should be:
- **Sample data**: ~5 chunks (15 verses ÷ 3)
- **Full KJV**: ~10,367 chunks (31,102 verses ÷ 3)

### Verify Embedding Dimensions

Enable debug logging in `appsettings.json`:
```json
"Logging": {
  "LogLevel": {
    "Default": "Debug"
  }
}
```

Should see: embedding vectors with 768 dimensions (nomic-embed-text standard)

### Test Similarity Scores

Logs should show relevance scores for retrieved verses:
```
[DBG] Retrieved chunks with scores: [0.82, 0.75, 0.68]
```

Good scores: 0.6-0.9 (relevant matches)
Low scores: < 0.5 (not relevant, won't be used)

## ✅ Final Confirmation

All checks passed! RAG is fully operational. 🎉

**You now have:**
- ✅ Local AI (Phi-4) running
- ✅ RAG system active
- ✅ Scripture retrieval working
- ✅ Biblically grounded responses

## 📚 Next Steps

1. **Expand Bible data**: Add full KJV (see [RAG_IMPLEMENTATION.md](RAG_IMPLEMENTATION.md))
2. **Test with various topics**: Explore different biblical themes
3. **Monitor logs**: Watch which verses are retrieved
4. **Adjust thresholds**: Tune relevance scores if needed
5. **Add characters**: Implement Moses, Mary, Peter (see [GROK_SUGGESTIONS.md](GROK_SUGGESTIONS.md))

---

**Need help?** See:
- [RAG_IMPLEMENTATION.md](RAG_IMPLEMENTATION.md) - Full documentation
- [QUICK_START.md](QUICK_START.md) - Getting started guide
- [RAG_SUMMARY.md](RAG_SUMMARY.md) - Implementation overview
