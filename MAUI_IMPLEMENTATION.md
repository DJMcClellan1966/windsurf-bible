# AI-Bible-App MAUI - Modern Cross-Platform UI

## Overview

The AI-Bible-App now features a **modern, beautiful user interface** built with **.NET MAUI** (Multi-platform App UI), bringing the AI Bible experience to mobile and desktop platforms with a native look and feel.

## Features

### ✅ Cross-Platform Support
- **Windows** - Native Windows desktop app
- **Android** - Android mobile app (5.0+)
- **iOS** - iPhone and iPad apps (15.0+)
- **macOS** - macOS desktop app via Mac Catalyst (15.0+)

### ✅ Modern MVVM Architecture
- **CommunityToolkit.MVVM** for clean, maintainable code
- **Dependency Injection** for services and ViewModels
- **Shell Navigation** for seamless page transitions
- **Data Binding** for reactive UI updates

### ✅ Beautiful UI/UX
- **Material Design** inspired interface
- **Character Selection** with avatar cards and detailed information
- **Chat Interface** with message bubbles and typing indicators
- **Prayer Generator** with save functionality
- **Responsive Layout** adapts to different screen sizes

### ✅ Same Powerful Backend
- All existing features: local Phi-4 AI, RAG with World English Bible, offline processing
- Reuses Core and Infrastructure layers from console app
- Shared business logic and data access

## Project Structure

```
src/AI-Bible-App.Maui/
├── Views/                  # XAML pages
│   ├── CharacterSelectionPage.xaml
│   ├── ChatPage.xaml
│   └── PrayerPage.xaml
├── ViewModels/            # MVVM ViewModels
│   ├── BaseViewModel.cs
│   ├── CharacterSelectionViewModel.cs
│   ├── ChatViewModel.cs
│   └── PrayerViewModel.cs
├── Services/              # UI services
│   ├── INavigationService.cs
│   └── NavigationService.cs
├── Converters/            # Value converters
│   ├── StringEqualConverter.cs
│   ├── InvertedBoolConverter.cs
│   └── StringNotEmptyConverter.cs
├── Resources/             # Images, fonts, styles
├── App.xaml               # Application resources
├── AppShell.xaml          # Shell navigation setup
└── MauiProgram.cs         # Dependency injection configuration
```

## Screenshots

### Character Selection
- Beautiful card-based layout
- Character avatars with names, roles, and descriptions
- Easy navigation to chat or prayer

### Chat Interface
- User messages (right-aligned, blue bubbles)
- AI responses (left-aligned, white bubbles)
- Typing indicator when AI is generating response
- Clear chat option

### Prayer Generator
- Text editor for prayer requests
- Generate personalized prayers
- Save prayers for later
- View prayer history

## How to Run

### Windows
```bash
cd src/AI-Bible-App.Maui
dotnet build -f net10.0-windows10.0.19041.0
dotnet run -f net10.0-windows10.0.19041.0
```

### Android (requires Android SDK)
```bash
dotnet build -f net10.0-android
dotnet run -f net10.0-android
```

### iOS (requires Mac with Xcode)
```bash
dotnet build -f net10.0-ios
dotnet run -f net10.0-ios
```

### macOS
```bash
dotnet build -f net10.0-maccatalyst
dotnet run -f net10.0-maccatalyst
```

## Configuration

The app uses the same `appsettings.json` as the console app:

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

## Key Technologies

- **.NET 10 / .NET MAUI** - Cross-platform UI framework
- **CommunityToolkit.MVVM 8.4** - MVVM helpers and code generation
- **Microsoft.Extensions.DependencyInjection** - Service container
- **Microsoft.Extensions.Configuration** - Configuration management
- **XAML** - Declarative UI markup
- **C# 13** - Modern C# features

## Architecture Highlights

### Dependency Injection
All services registered in `MauiProgram.cs`:
- Core services (AI, RAG, Repositories)
- ViewModels (transient)
- Pages (transient)
- Navigation service (singleton)

### Navigation
Uses Shell navigation with route registration:
- `characters` → CharacterSelectionPage (root)
- `chat` → ChatPage (with character parameter)
- `prayer` → PrayerPage

### Data Flow
1. User interacts with View (XAML)
2. View binds to ViewModel properties/commands
3. ViewModel calls Core/Infrastructure services
4. Services process data (AI, RAG, storage)
5. ViewModel updates properties
6. View automatically updates via data binding

## Benefits Over Console App

- ✅ **Modern UX** - Intuitive, beautiful interface
- ✅ **Mobile** - Use on phones and tablets
- ✅ **Touch-Friendly** - Optimized for touch input
- ✅ **Native Look** - Adapts to platform (iOS, Android, Windows)
- ✅ **Easier to Use** - No command-line knowledge required
- ✅ **Wider Audience** - Accessible to non-technical users

## Future Enhancements

### Planned Features
1. **Character Avatars** - Custom images for each biblical character
2. **Text-to-Speech** - Hear characters speak their responses
3. **Voice Input** - Speak your questions
4. **Dark Mode** - Eye-friendly dark theme
5. **History View** - Browse past conversations
6. **Export Conversations** - Share as PDF or text
7. **Reading Plans** - Guided Scripture reading
8. **Verse of the Day** - Daily inspirational verse

### Performance Optimizations
- **Lazy Loading** - Load characters/prayers on demand
- **Virtual Scrolling** - Efficient long message lists
- **Image Caching** - Cache avatars and resources
- **Background Sync** - Save data asynchronously

### Platform-Specific Features
- **iOS** - Share extension, widgets, Siri shortcuts
- **Android** - Share intent, home screen widgets
- **Windows** - Jump lists, toast notifications
- **macOS** - Menu bar app, Touch Bar support

## Development Notes

### MVVM Pattern
- **Model** - Core domain models (BiblicalCharacter, Prayer, ChatMessage)
- **View** - XAML pages (CharacterSelectionPage, ChatPage, PrayerPage)
- **ViewModel** - Presentation logic and state (CharacterSelectionViewModel, etc.)

### Commands
- Use `[RelayCommand]` attribute for automatic command generation
- Commands automatically implement ICommand interface
- Support for async commands with `RelayCommandAttribute`

### Observable Properties
- Use `[ObservableProperty]` for automatic property change notification
- Generates OnPropertyChanged calls
- Simplifies MVVM implementation

### Value Converters
- **StringEqualConverter** - Check if string equals a value
- **InvertedBoolConverter** - Invert boolean values
- **StringNotEmptyConverter** - Check if string has content

## Troubleshooting

### Build Errors
- Ensure .NET 10 SDK is installed
- For Android: Install Android SDK via Visual Studio or Android Studio
- For iOS/macOS: Requires Mac with Xcode installed

### Runtime Errors
- Ensure Ollama is running (`http://localhost:11434`)
- Check `appsettings.json` configuration
- Verify models are installed (`ollama list`)

### UI Issues
- Clear bin/obj folders: `dotnet clean`
- Rebuild: `dotnet build`
- Check XAML syntax for binding errors

## Resources

- [.NET MAUI Documentation](https://docs.microsoft.com/dotnet/maui/)
- [CommunityToolkit.MVVM](https://learn.microsoft.com/windows/communitytoolkit/mvvm/introduction)
- [XAML Documentation](https://docs.microsoft.com/xaml/)
- [Ollama](https://ollama.com/)

## Contributing

When adding new features:
1. Create View (XAML) and code-behind
2. Create ViewModel with observable properties and commands
3. Register ViewModel and View in `MauiProgram.cs`
4. Add navigation route in `AppShell.xaml.cs` if needed
5. Update this README

---

**The AI-Bible-App is now a beautiful, modern app ready for mobile and desktop! 🎉**
