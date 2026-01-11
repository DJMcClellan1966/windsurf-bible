# Voices of Scripture

A cross-platform application that allows users to interact with biblical figures through AI-powered conversations and receive personalized daily prayers - **now fully offline with Scripture-grounded responses and a beautiful modern UI**.

## ✨ New Features

### 🎨 Modern Cross-Platform UI (NEW!)
- **Beautiful .NET MAUI interface** for mobile and desktop
- Native apps for **Windows, Android, iOS, and macOS**
- Intuitive card-based character selection
- Modern chat interface with message bubbles
- Touch-optimized prayer generator
- See [MAUI_IMPLEMENTATION.md](MAUI_IMPLEMENTATION.md) for details

### 🔒 Fully Offline AI
- No internet required after setup
- Complete privacy - all data stays on your device
- Uses local **Phi-4** model via Ollama
- Zero API costs

### 📖 RAG-Powered Scripture Grounding
- **Retrieval-Augmented Generation (RAG)** ensures biblically accurate responses
- Automatically retrieves relevant Bible verses before generating responses
- Reduces AI hallucinations with real Scripture
- **Multiple Bible translations**: World English Bible (WEB) and King James Version (KJV)
- **Flexible chunking**: Per-verse, overlap, or multi-verse strategies
- Semantic search with enhanced metadata (book, chapter, verse, reference)
- See [RAG_IMPLEMENTATION.md](RAG_IMPLEMENTATION.md) and [WEB_BIBLE_ADDITION.md](WEB_BIBLE_ADDITION.md) for details

## Features

### 1. Biblical Character Chat System
- **Interactive Conversations**: Chat with biblical figures who respond in character
- **Scripture-Grounded**: RAG retrieves relevant Bible passages to inform responses
- **Available Characters**:
  - **David** - King of Israel, Psalmist, and Shepherd
  - **Paul** - Apostle to the Gentiles, Missionary, and Letter Writer
- **Extensible Design**: Easy to add more biblical characters
- **Character Authenticity**: Each character has a unique personality and speaking style based on biblical context
- **Conversation History**: Save and review past conversations

### 2. Daily Prayer Generation
- Generate personalized prayers powered by AI
- **Scripture-Infused**: Prayers informed by relevant Bible verses
- Request prayers for specific topics or needs
- Save prayer history for future reference
- View all saved prayers

### 3. Technical Architecture
- **Clean Architecture**: Separation of concerns with distinct layers
- **Projects**:
  - `AI-Bible-App.Core` - Domain models and interfaces
  - `AI-Bible-App.Infrastructure` - AI service integration, RAG, and data access
  - `AI-Bible-App.Console` - Console application entry point
  - `AI-Bible-App.Tests` - Unit tests
- **AI Integration**: Local Phi-4 model via Ollama (Microsoft Semantic Kernel)
- **RAG System**: Vector embeddings with semantic search
- **Data Persistence**: JSON file-based storage for chat and prayer history

## Prerequisites

- .NET 8.0 SDK or later
- **Ollama** - for running local AI models
- **8-16 GB RAM** recommended

## Setup Instructions

### 1. Install Ollama

**Windows:**
Download from [ollama.ai](https://ollama.ai/download)

**macOS:**
```bash
brew install ollama
```

**Linux:**
```bash
curl -fsSL https://ollama.ai/install.sh | sh
```

### 2. Download Required Models

```bash
# Chat model (required)
ollama pull phi4

# Embedding model for RAG (required)
ollama pull nomic-embed-text
```

### 3. Clone the Repository
```bash
git clone https://github.com/DJMcClellan1966/AI-Bible-app.git
cd AI-Bible-app
```

### 4. Configuration

The app is pre-configured for local Ollama in [appsettings.json](src/AI-Bible-App.Console/appsettings.json).

```bash
cd src/AI-Bible-App.Console
```

Create `appsettings.local.json`:
```json
{
  "OpenAI": {
    "ApiKey": "your-azure-openai-api-key",
    "Endpoint": "https://your-resource-name.openai.azure.com/",
    "DeploymentName": "gpt-4"
  }
}
```

**Option B: Edit `appsettings.json` directly (not recommended for production):**

Update the values in `src/AI-Bible-App.Console/appsettings.json`:
```json
{
  "OpenAI": {
    "ApiKey": "your-azure-openai-api-key",
    "Endpoint": "https://your-resource-name.openai.azure.com/",
    "DeploymentName": "gpt-4"
  }
}
```

**Configuration Options:**
- `ApiKey`: Your Azure OpenAI API key
- `Endpoint`: Your Azure OpenAI endpoint URL
- `DeploymentName`: The name of your deployed model (e.g., "gpt-4", "gpt-35-turbo")

### 3. Build the Solution

```bash
dotnet build
```

### 4. Run the Application

```bash
cd src/AI-Bible-App.Console
dotnet run
```

Or from the root directory:
```bash
dotnet run --project src/AI-Bible-App.Console/AI-Bible-App.Console.csproj
```

## How to Use the App

### Main Menu Options

1. **Chat with a Biblical Character**
   - Select a character (David or Paul)
   - Start a conversation by typing your message
   - Type `save` to save the conversation session
   - Type `exit` to end the conversation

2. **Generate Daily Prayer**
   - Enter a specific topic or press Enter for a general daily prayer
   - Choose to save the prayer to your history

3. **View Prayer History**
   - Browse all your saved prayers with topics and timestamps

4. **View Chat History**
   - See all saved chat sessions with character names and message counts

### Example Conversation Starters

**With David:**
- "Tell me about your experience facing Goliath"
- "How did you handle being pursued by King Saul?"
- "What inspired you to write the psalms?"
- "Can you share wisdom about leadership?"

**With Paul:**
- "Tell me about your conversion on the Damascus road"
- "What was your experience planting churches?"
- "Can you explain grace and faith?"
- "What motivated you during your missionary journeys?"

## How to Add New Biblical Characters

### Step 1: Update the Character Repository

Edit `src/AI-Bible-App.Infrastructure/Repositories/InMemoryCharacterRepository.cs` and add a new character to the `_characters` list:

```csharp
new BiblicalCharacter
{
    Id = "moses",
    Name = "Moses",
    Title = "Prophet, Lawgiver, Leader of the Exodus",
    Description = "Led the Israelites out of Egyptian slavery and received the Ten Commandments",
    Era = "circa 1400 BC",
    BiblicalReferences = new List<string> 
    { 
        "Exodus",
        "Leviticus",
        "Numbers",
        "Deuteronomy"
    },
    SystemPrompt = @"You are Moses from the Bible. You led the Israelites out of Egypt...
    
[Add detailed character prompt here describing their personality, speaking style, and perspective]",
    Attributes = new Dictionary<string, string>
    {
        { "Personality", "Humble, Faithful, Leader" },
        { "KnownFor", "Ten Commandments, Exodus, Burning Bush" },
        { "KeyVirtues", "Obedience, Humility, Intercession" }
    }
}
```

### Step 2: Craft the System Prompt

The system prompt is crucial for character authenticity. Include:
- Character's background and biblical context
- Speaking style and personality traits
- Key experiences and teachings
- Biblical references they might mention
- Their perspective on God and faith

### Step 3: Test the Character

Build and run the application to test the new character's responses.

## Running Tests

```bash
dotnet test
```

This will run all unit tests in the `AI-Bible-App.Tests` project.

## Project Structure

```
AI-Bible-App/
├── src/
│   ├── AI-Bible-App.Core/           # Domain models and interfaces
│   │   ├── Models/
│   │   │   ├── BiblicalCharacter.cs
│   │   │   ├── ChatMessage.cs
│   │   │   ├── ChatSession.cs
│   │   │   └── Prayer.cs
│   │   └── Interfaces/
│   │       ├── IAIService.cs
│   │       ├── ICharacterRepository.cs
│   │       ├── IChatRepository.cs
│   │       └── IPrayerRepository.cs
│   │
│   ├── AI-Bible-App.Infrastructure/  # Implementation layer
│   │   ├── Services/
│   │   │   └── OpenAIService.cs
│   │   └── Repositories/
│   │       ├── InMemoryCharacterRepository.cs
│   │       ├── JsonChatRepository.cs
│   │       └── JsonPrayerRepository.cs
│   │
│   └── AI-Bible-App.Console/        # Application entry point
│       ├── Program.cs
│       ├── BibleApp.cs
│       └── appsettings.json
│
└── tests/
    └── AI-Bible-App.Tests/          # Unit tests
        ├── Models/
        └── Repositories/
```

## Data Storage

The application stores data locally in JSON files:

- **Chat History**: `data/chat_sessions.json`
- **Prayer History**: `data/prayers.json`

These files are created automatically when you save conversations or prayers.

## Error Handling

The application includes comprehensive error handling:
- API connection errors are logged and displayed to users
- Invalid inputs are validated
- Rate limiting is respected
- All errors are logged for debugging

## Logging

The application uses Microsoft.Extensions.Logging for structured logging:
- Console logging is enabled by default
- Log level can be configured in `appsettings.json`
- Errors are logged with full stack traces

## API Configuration Notes

### Azure OpenAI
- Requires an Azure subscription
- Create an Azure OpenAI resource
- Deploy a model (GPT-4 or GPT-3.5-Turbo recommended)
- Use the endpoint and API key in configuration

### Rate Limits
- Be mindful of API rate limits
- The application limits conversation history to the last 10 messages to manage token usage
- Consider implementing retry logic for production use

## Future Enhancements

### 📋 Improvement Recommendations

#### High Priority (Quick Wins)
- [ ] **1. Full Bible Reader** - Add a complete Bible reading mode with chapter navigation
- [ ] **2. Search Functionality** - Search across conversations and Bible text
- [ ] **3. Export/Share** - Export conversations as PDF or share to social media
- [ ] **4. Daily Verse** - Home screen widget with daily verse and character insight
- [ ] **5. More Characters** - Add Ruth, Esther, Solomon, Peter, John, Mary, etc.

#### Medium Priority (Enhanced Features)
- [ ] **6. Reading Plans** - Guided study plans (21-day journey through Psalms with David)
- [ ] **7. Bookmarks** - Save favorite verses and conversations
- [ ] **8. Offline Indicator** - Show when using cached responses vs. live AI
- [ ] **9. Font Size Settings** - Accessibility settings for text size
- [ ] **10. Conversation Topics** - Pre-built discussion starters for each character

#### Lower Priority (Advanced)
- [ ] **11. Cloud Sync** - Sync conversations and reflections across devices
- [ ] **12. Original Languages** - Show Hebrew/Greek with transliteration
- [ ] **13. Cross-References** - Link related verses automatically
- [ ] **14. Commentary Integration** - Add scholarly commentary access
- [ ] **15. Group Study Mode** - Share conversations in group settings

#### Technical Improvements
- [x] **16. Fix TTS** ✅ - Re-implemented with Windows Speech API for Windows platform
- [x] **17. Streaming Responses** ✅ - Already implemented with `IAsyncEnumerable<string>`
- [x] **18. Better Error Recovery** ✅ - Added resilience helper with retry logic and user-friendly messages
- [x] **19. Performance Improvements** ✅ - Lazy loading Bible data, indexed lookups, LRU cache

### Recent Technical Updates

1. **Windows TTS Service** - Platform-specific implementation using `Windows.Media.SpeechSynthesis` with proper voice selection and text cleaning
2. **Resilience Helper** - Exponential backoff retry logic with user-friendly error messages
3. **Bible Lookup Performance** - Lazy loading, O(1) book index lookup, LRU passage cache

---

Potential features for future development:
- Web UI (Blazor)
- Desktop UI (WPF/MAUI)
- Bible verse lookup integration
- Multi-language support
- Audio prayers (text-to-speech)
- Prayer request sharing
- More biblical characters (Moses, Peter, Mary, etc.)
- Conversation themes and guided discussions
- Export conversations to PDF

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

## License

This project is licensed under the MIT License.

## Acknowledgments

- Biblical character personalities and teachings are based on biblical texts
- AI responses are generated using Azure OpenAI
- This is an educational and spiritual growth tool
