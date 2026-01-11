# Quick Start - Running with Phi-4

## Prerequisites Check

Before running, verify you have:
- [x] Ollama installed
- [x] Phi-4 model downloaded
- [x] .NET 8.0 SDK installed

## Step-by-Step First Run

### 1. Verify Ollama Installation

Open a terminal and check if Ollama is running:

```bash
ollama list
```

**Expected output:**
```
NAME            ID              SIZE    MODIFIED
phi4:latest     abc123def456    8.2 GB  2 hours ago
```

If you don't see Phi-4, download it:
```bash
ollama pull phi4
```

### 2. Verify Ollama Service

Test that Ollama is responding:

```bash
ollama run phi4 "Hello, what can you do?"
```

You should see a response. Type `/bye` to exit.

### 3. Build the Application

Navigate to the project root and build:

```bash
cd "c:\Users\DJMcC\OneDrive\Desktop\AI-Bible-app\AI-Bible-app"
dotnet build
```

**Expected:** "Build succeeded"

### 4. Run the Application

```bash
cd src\AI-Bible-App.Console
dotnet run
```

### 5. First Interaction

You should see the main menu:

```
╔════════════════════════════════════════╗
║      AI Bible Companion v1.0           ║
║      Now running offline with Phi-4    ║
╚════════════════════════════════════════╝

Main Menu:
1. Talk with a Biblical Character
2. Generate a Personalized Prayer
3. View Chat History
4. View Prayer History
5. Exit

Choose an option (1-5):
```

Try option 1 to chat with David or Paul!

## Troubleshooting First Run

### Error: "Connection refused" or "Unable to connect to Ollama"

**Problem:** Ollama service isn't running

**Solution:**
- **Windows:** Launch "Ollama" from Start Menu
- **macOS:** Run `ollama serve` in a terminal
- **Linux:** Run `systemctl start ollama` or `ollama serve`

### Error: "Model 'phi4' not found"

**Problem:** Phi-4 model not downloaded

**Solution:**
```bash
ollama pull phi4
```

Wait for ~8GB download to complete.

### Error: "Out of memory" during response generation

**Problem:** System doesn't have enough RAM

**Solutions:**
1. **Close other applications** to free memory
2. **Use a quantized version:**
   ```bash
   ollama pull phi4:q4_0
   ```
   Update `appsettings.json`:
   ```json
   "ModelName": "phi4:q4_0"
   ```

3. **Try a smaller model:**
   ```bash
   ollama pull phi3.5
   ```
   Update `appsettings.json`:
   ```json
   "ModelName": "phi3.5"
   ```

### Slow Response Times (> 10 seconds)

**Possible causes:**
- First request loads model into memory (normal)
- CPU-only inference (expected on most systems)
- System under load

**Optimizations:**
1. **Keep Ollama running** - model stays in memory
2. **Close resource-heavy apps**
3. **Check if GPU is being used:**
   ```bash
   ollama ps
   ```
   Should show GPU if available (NVIDIA/AMD)

## Testing Character Conversations

### Example 1: Talking with David

```
Choose an option: 1
Select a character:
1. David
2. Paul

Choose (1-2): 1

You are now talking with David, King of Israel
Type 'exit' to return to main menu

You: Tell me about facing Goliath
David: Ah, that day... I was but a youth when I stepped onto that valley...
```

### Example 2: Generating a Prayer

```
Choose an option: 2

What would you like prayer about? (or press Enter for daily prayer): 
strength during difficult times

Generating your prayer...

Heavenly Father,

We come before You seeking strength in these difficult times...
```

## Configuration Options

Edit [appsettings.json](src/AI-Bible-App.Console/appsettings.json) to customize:

```json
{
  "Ollama": {
    "Url": "http://localhost:11434",   // Change if Ollama runs elsewhere
    "ModelName": "phi4"                 // Change to use different model
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",         // Use "Debug" for verbose logging
      "Microsoft": "Warning"
    }
  }
}
```

## Performance Expectations

### First Response
- **Time:** 3-10 seconds (model loading)
- **Normal:** This happens once per session

### Subsequent Responses
- **Time:** 1-3 seconds on average CPU
- **Time:** < 1 second with GPU acceleration

### Memory Usage
- **Idle:** ~100 MB
- **With model loaded:** 8-10 GB (Ollama process)
- **Total system:** 16 GB recommended

## What to Expect

### Character Personality
Phi-4 should maintain distinct character voices:
- **David:** Poetic, warrior-king perspective, references Psalms
- **Paul:** Theological depth, references his letters and journeys

### Prayer Generation
Prayers should be:
- Reverent and biblical in tone
- 2-3 paragraphs
- Relevant to your topic
- Saved to `Data/prayers/` automatically

### Conversation History
- Last 10 messages kept in context
- Full history saved to `Data/chats/`
- Can be viewed from main menu (option 3)

## Tips for Best Results

1. **Keep questions focused** - Phi-4 works best with clear, specific questions
2. **Reference Scripture** - Mention specific books/chapters for better responses
3. **Be patient on first run** - Model loading takes time initially
4. **Restart Ollama periodically** - If responses degrade, restart Ollama service

## Getting Help

If you encounter issues:

1. **Check logs** - Look for errors in the console output
2. **Verify Ollama:** `ollama list` and `ollama ps`
3. **Check model:** `ollama show phi4`
4. **Test directly:** `ollama run phi4 "test message"`

## Next Steps

Once running successfully:
- ✅ Try both characters (David and Paul)
- ✅ Generate prayers on different topics
- ✅ Review chat history
- 📖 Read [GROK_SUGGESTIONS.md](GROK_SUGGESTIONS.md) for roadmap
- 🔧 Plan RAG implementation for Scripture grounding

---

**You're now running a fully offline AI Bible companion!** 🙏

No internet required, complete privacy, unlimited conversations.
