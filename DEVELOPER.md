# Developer Guide

## Project Architecture

This project follows **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────────────────────────────────┐
│           Console Application               │
│      (User Interface / Entry Point)         │
└────────────────┬────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────┐
│         Infrastructure Layer                │
│   (OpenAI Service, Repositories, Data)      │
└────────────────┬────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────┐
│            Core Layer                       │
│      (Domain Models, Interfaces)            │
└─────────────────────────────────────────────┘
```

## Building and Testing

### Build the Solution
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Run the Console Application
```bash
cd src/AI-Bible-App.Console
dotnet run
```

## Key Components

### Core Layer (`AI-Bible-App.Core`)

**Models:**
- `BiblicalCharacter` - Represents a biblical figure with personality and context
- `ChatMessage` - Individual message in a conversation
- `ChatSession` - Complete conversation session with message history
- `Prayer` - Generated prayer with metadata

**Interfaces:**
- `IAIService` - AI service abstraction
- `ICharacterRepository` - Character data access
- `IChatRepository` - Chat history persistence
- `IPrayerRepository` - Prayer history persistence

### Infrastructure Layer (`AI-Bible-App.Infrastructure`)

**Services:**
- `OpenAIService` - Azure OpenAI integration
  - Implements chat completion
  - Handles conversation context
  - Generates prayers

**Repositories:**
- `InMemoryCharacterRepository` - Pre-configured biblical characters
- `JsonChatRepository` - JSON file-based chat storage
- `JsonPrayerRepository` - JSON file-based prayer storage

### Application Layer (`AI-Bible-App.Console`)

**Main Components:**
- `Program.cs` - Application startup and DI configuration
- `BibleApp.cs` - Main application logic and UI

## Adding New Biblical Characters

### Step 1: Define Character Profile

Edit `src/AI-Bible-App.Infrastructure/Repositories/InMemoryCharacterRepository.cs`:

```csharp
new BiblicalCharacter
{
    Id = "moses",  // Unique identifier (lowercase)
    Name = "Moses",  // Display name
    Title = "Prophet, Lawgiver, Leader of the Exodus",
    Description = "Led the Israelites out of Egyptian slavery",
    Era = "circa 1400 BC",
    BiblicalReferences = new List<string> 
    { 
        "Exodus",
        "Leviticus",
        "Numbers",
        "Deuteronomy"
    },
    SystemPrompt = @"You are Moses from the Bible...
        [Detailed character instructions]",
    Attributes = new Dictionary<string, string>
    {
        { "Personality", "Humble, Faithful, Leader" },
        { "KnownFor", "Ten Commandments, Exodus" },
        { "KeyVirtues", "Obedience, Humility" }
    }
}
```

### Step 2: Craft System Prompt

The `SystemPrompt` is crucial for character authenticity. Include:

1. **Character Introduction**
   - Who they are
   - Their role in biblical history

2. **Characteristics and Speaking Style**
   - Personality traits
   - How they express themselves
   - Language patterns

3. **Life Experiences**
   - Key events they witnessed
   - Challenges they faced
   - Victories they achieved

4. **Theological Perspective**
   - Their understanding of God
   - Key teachings or revelations
   - Spiritual insights

5. **Guidance for Responses**
   - Speak in first person
   - Reference personal experiences
   - Be encouraging and point to God

### Step 3: Test the Character

1. Build the solution
2. Run the application
3. Select the new character
4. Test various conversation topics
5. Verify character authenticity

## Configuration

### Required Settings

Create `appsettings.local.json` (gitignored):

```json
{
  "OpenAI": {
    "ApiKey": "your-azure-openai-key",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "DeploymentName": "gpt-4"
  }
}
```

### Optional Settings

Adjust logging levels in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "AI_Bible_App": "Debug"
    }
  }
}
```

## Data Storage

### Location
- Chat sessions: `data/chat_sessions.json`
- Prayers: `data/prayers.json`

### Format
Both files store arrays of objects in JSON format.

### Schema

**Chat Session:**
```json
{
  "Id": "guid",
  "CharacterId": "david",
  "StartedAt": "2024-01-01T00:00:00Z",
  "EndedAt": null,
  "Messages": [
    {
      "Id": "guid",
      "Role": "user",
      "Content": "Hello",
      "Timestamp": "2024-01-01T00:00:00Z",
      "CharacterId": "david"
    }
  ]
}
```

**Prayer:**
```json
{
  "Id": "guid",
  "Content": "Dear Lord...",
  "Topic": "Guidance",
  "CreatedAt": "2024-01-01T00:00:00Z",
  "Tags": []
}
```

## Dependency Injection

The application uses Microsoft.Extensions.DependencyInjection:

```csharp
services.AddSingleton<IAIService, OpenAIService>();
services.AddSingleton<ICharacterRepository, InMemoryCharacterRepository>();
services.AddSingleton<IChatRepository, JsonChatRepository>();
services.AddSingleton<IPrayerRepository, JsonPrayerRepository>();
```

## Error Handling

### AI Service Errors
- Connection failures are logged and displayed
- Rate limiting should be handled (not yet implemented)
- Invalid responses are caught and logged

### Data Persistence Errors
- File I/O errors are caught
- Missing directories are created automatically
- Corrupt JSON files will throw exceptions

### User Input Validation
- Empty inputs are ignored
- Commands (exit, save) are case-insensitive

## Testing Strategy

### Unit Tests
Located in `tests/AI-Bible-App.Tests/`:

- **Model Tests**: Verify domain models initialize correctly
- **Repository Tests**: Test data access logic
- **Service Tests**: Would mock AI calls (not yet implemented)

### Running Specific Tests
```bash
# Run all tests
dotnet test

# Run tests for a specific class
dotnet test --filter BiblicalCharacterTests

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Future Enhancements

### High Priority
1. Implement rate limiting and retry logic for OpenAI
2. Add more biblical characters (Moses, Peter, Mary, etc.)
3. Implement conversation analytics
4. Add Bible verse lookup integration

### Medium Priority
1. Create Web UI (Blazor)
2. Add export functionality (PDF, text)
3. Implement user accounts and cloud storage
4. Multi-language support

### Low Priority
1. Audio prayers (text-to-speech)
2. Prayer sharing features
3. Desktop UI (WPF/MAUI)
4. Mobile app

## Troubleshooting

### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet build
```

### Missing Packages
```bash
# Restore packages
dotnet restore
```

### Configuration Issues
- Verify `appsettings.local.json` exists and has valid values
- Check Azure OpenAI endpoint is correct
- Ensure API key has proper permissions

### Runtime Errors
- Check logs in console output
- Verify data directory is writable
- Ensure network connectivity for API calls

## Code Style

### Naming Conventions
- PascalCase for classes, methods, properties
- camelCase for private fields (with underscore prefix: `_field`)
- Use meaningful, descriptive names

### Comments
- XML documentation on public APIs
- Inline comments for complex logic
- Keep comments up-to-date

### Organization
- One class per file
- Group related functionality
- Use regions sparingly

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add/update tests
5. Ensure all tests pass
6. Submit a pull request

## Resources

- [Azure OpenAI Documentation](https://learn.microsoft.com/azure/cognitive-services/openai/)
- [.NET 8 Documentation](https://learn.microsoft.com/dotnet/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
