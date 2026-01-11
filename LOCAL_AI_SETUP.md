# Local AI Setup with Phi-4

The AI-Bible-App now uses **Phi-4** running locally via Ollama instead of Azure OpenAI. This provides:
- ✅ **True offline capability** - no internet required after setup
- ✅ **Complete privacy** - data never leaves your machine
- ✅ **No API costs** - free to use
- ✅ **Faster responses** - local inference (depending on hardware)

## Prerequisites

### 1. Install Ollama

**Windows:**
Download and install from [ollama.ai](https://ollama.ai/download)

**macOS:**
```bash
brew install ollama
```

**Linux:**
```bash
curl -fsSL https://ollama.ai/install.sh | sh
```

### 2. Pull the Phi-4 Model

After installing Ollama, open a terminal and run:

```bash
ollama pull phi4
```

This will download Microsoft's Phi-4 model (~8GB). The model provides excellent performance for its size.

### 3. Verify Ollama is Running

Ollama should start automatically. Verify it's running:

```bash
ollama list
```

You should see `phi4` in the list.

## Configuration

The app is already configured to use Ollama in [appsettings.json](src/AI-Bible-App.Console/appsettings.json):

```json
{
  "Ollama": {
    "Url": "http://localhost:11434",
    "ModelName": "phi4"
  }
}
```

### Using a Different Model

If you want to try other models, install them with Ollama and update `ModelName`:

```bash
ollama pull llama3.3    # Meta's Llama 3.3 (70B - requires powerful hardware)
ollama pull mistral     # Mistral 7B (good alternative)
ollama pull gemma2      # Google's Gemma 2
```

Then update `appsettings.json`:
```json
"ModelName": "mistral"
```

## Running the Application

### 1. Restore Dependencies

```bash
dotnet restore
```

This will download the OllamaSharp package that replaced Azure.AI.OpenAI.

### 2. Build

```bash
dotnet build
```

### 3. Run

```bash
cd src/AI-Bible-App.Console
dotnet run
```

The app will connect to your local Ollama instance and use Phi-4 for all conversations and prayer generation.

## Architecture Changes

### What Changed

| Component | Before | After |
|-----------|--------|-------|
| **Service** | `OpenAIService` | `LocalAIService` |
| **Package** | `Azure.AI.OpenAI` | `OllamaSharp` |
| **Endpoint** | Azure cloud | `http://localhost:11434` |
| **Model** | GPT-4 (cloud) | Phi-4 (local) |
| **Configuration** | API key + endpoint | Local URL + model name |

### Files Modified

- [AI-Bible-App.Infrastructure.csproj](src/AI-Bible-App.Infrastructure/AI-Bible-App.Infrastructure.csproj) - Updated dependencies
- [LocalAIService.cs](src/AI-Bible-App.Infrastructure/Services/LocalAIService.cs) - New local AI implementation
- [Program.cs](src/AI-Bible-App.Console/Program.cs) - Switched service registration
- [appsettings.json](src/AI-Bible-App.Console/appsettings.json) - Updated configuration

### Old Service (Preserved)

The original [OpenAIService.cs](src/AI-Bible-App.Infrastructure/Services/OpenAIService.cs) is still available if you want to switch back to Azure OpenAI.

## Performance Notes

### Hardware Requirements

**Minimum:**
- 8 GB RAM
- 4 CPU cores
- 10 GB disk space

**Recommended:**
- 16 GB RAM
- 8 CPU cores or Apple M1/M2/M3
- 20 GB disk space

### Expected Performance

- **First response:** 3-10 seconds (model loading)
- **Subsequent responses:** 1-3 seconds
- **GPU acceleration:** Automatic if NVIDIA/AMD GPU detected

## Troubleshooting

### "Connection refused" error

Ollama isn't running. Start it:

**Windows:** Start Ollama from Start Menu  
**macOS/Linux:**
```bash
ollama serve
```

### Model not found

Pull the model:
```bash
ollama pull phi4
```

### Slow responses

- Close other applications to free RAM
- Consider using a smaller model like `phi4:3.8b`
- Check CPU/GPU usage with task manager

### Out of memory

Use quantized versions:
```bash
ollama pull phi4:q4_0  # 4-bit quantization
```

Update config:
```json
"ModelName": "phi4:q4_0"
```

## Next Steps

With local AI working, you can now:

1. ✅ **Run completely offline** - disconnect from internet and keep using
2. 🔄 **Add RAG (Retrieval-Augmented Generation)** - ground responses in Scripture
3. 🔄 **Expand characters** - add Moses, Mary, Peter, Esther, John
4. 🔄 **Build MAUI UI** - create mobile/desktop interface

See [GROK_SUGGESTIONS.md](GROK_SUGGESTIONS.md) for the full roadmap.

## Resources

- [Ollama Documentation](https://github.com/ollama/ollama)
- [OllamaSharp Library](https://github.com/awaescher/OllamaSharp)
- [Phi-4 Model Card](https://ollama.ai/library/phi4)
- [Microsoft Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/) (for RAG)
