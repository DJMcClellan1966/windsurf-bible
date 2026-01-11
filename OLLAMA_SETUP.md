# Ollama Setup Guide

## Prerequisites
The AI Bible App requires Ollama running locally with two models:
- **phi3.5:3.8b-mini-instruct-q4_K_M** - Fast, optimized model for conversations (recommended)
- **nomic-embed-text** - Embedding model for RAG (Retrieval Augmented Generation)

## Installation Steps

### 1. Download and Install Ollama

**Windows:**
```powershell
# Download from: https://ollama.ai/download
# Or use winget:
winget install Ollama.Ollama
```

**Verify Installation:**
```powershell
ollama --version
```

### 2. Start Ollama Service

Ollama should start automatically after installation. If not:

```powershell
# Check if Ollama is running
Get-Process ollama -ErrorAction SilentlyContinue

# If not running, start it (usually auto-starts on Windows)
# Look for "Ollama" in system tray
```

### 3. Pull Required Models

**Pull Phi-3.5-Mini Model (Recommended - ~2.3 GB, quantized for speed):**
```powershell
ollama pull phi3.5:3.8b-mini-instruct-q4_K_M
```

**Alternative Models (if you prefer different options):**
```powershell
# Llama 3.2 3B - Excellent quality/speed ratio
ollama pull llama3.2:3b-instruct-q4_K_M

# Phi-4 - Higher quality but slower
ollama pull phi4

# Original Phi-3 Mini
ollama pull phi3:mini
```

**Pull Nomic Embed Text Model (~274 MB):**
```powershell
ollama pull nomic-embed-text
```

### 4. Verify Models Are Available

```powershell
ollama list
```

Expected output:
```
NAME                                    ID              SIZE      MODIFIED
phi3.5:3.8b-mini-instruct-q4_K_M       abc123...       2.3 GB    X minutes ago
nomic-embed-text:latest                 def456...       274 MB    X minutes ago
```

### 5. Test Ollama Connection

```powershell
# Test basic connectivity
curl http://localhost:11434

# Should return: "Ollama is running"
```

**Test Phi-4 Model:**
```powershell
ollama run phi4 "What is the meaning of faith?"
```

**Test Embedding Model:**
```powershell
curl http://localhost:11434/api/embeddings -d '{
  "model": "nomic-embed-text",
  "prompt": "The Lord is my shepherd"
}'
```

## Troubleshooting

### Issue: "Ollama service not available"

**Solution 1: Check if Ollama is running**
```powershell
Get-Process ollama -ErrorAction SilentlyContinue
```

If not running:
- Look for Ollama icon in system tray
- Restart Ollama from Start Menu
- Reboot computer (Ollama starts on boot)

**Solution 2: Check port 11434**
```powershell
netstat -ano | findstr :11434
```

Should show Ollama listening on port 11434.

**Solution 3: Firewall settings**
- Ensure Windows Firewall allows Ollama
- Go to: Windows Security → Firewall → Allow an app
- Find "Ollama" and ensure it's checked

### Issue: "Model not found: phi4"

**Solution:**
```powershell
# Pull the model
ollama pull phi4

# Verify it downloaded
ollama list
```

### Issue: "Model not found: nomic-embed-text"

**Solution:**
```powershell
# Pull the embedding model
ollama pull nomic-embed-text

# Verify it downloaded
ollama list
```

### Issue: Ollama uses too much RAM/CPU

**Solution: Configure Ollama settings**

Create/edit: `%USERPROFILE%\.ollama\config.json`

```json
{
  "num_gpu": 1,
  "num_thread": 4
}
```

Restart Ollama after changes.

## System Requirements

**Minimum:**
- 8 GB RAM
- 5 GB free disk space
- Windows 10/11

**Recommended:**
- 16 GB RAM
- 10 GB free disk space
- GPU with 4+ GB VRAM (for faster inference)

## Configuration in AI Bible App

The app connects to Ollama at: `http://localhost:11434`

If you need to change this (e.g., Ollama on different machine):

Edit: `src/AI-Bible-App.Maui/MauiProgram.cs`

```csharp
services.AddSingleton<ILocalAIService>(sp => 
    new LocalAIService(
        new Uri("http://your-ollama-server:11434"), // Change this
        sp.GetRequiredService<ILogger<LocalAIService>>(),
        sp.GetRequiredService<IBibleRAGService>()
    ));
```

## Next Steps

Once Ollama is installed and both models are pulled:

1. Run the AI Bible App
2. Initialization screen will verify Ollama connection
3. If successful, you'll see the character selection screen
4. Start chatting with biblical characters!

## Additional Resources

- Ollama Documentation: https://github.com/ollama/ollama
- Phi-4 Model Info: https://ollama.ai/library/phi4
- Nomic Embed Text: https://ollama.ai/library/nomic-embed-text
