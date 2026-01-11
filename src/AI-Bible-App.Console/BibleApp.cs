using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Logging;

namespace AI_Bible_App.Console;

/// <summary>
/// Main application class for the Bible App console interface
/// </summary>
public class BibleApp
{
    private readonly IAIService _aiService;
    private readonly ICharacterRepository _characterRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IPrayerRepository _prayerRepository;
    private readonly IBibleRAGService? _ragService;
    private readonly ILogger<BibleApp> _logger;
    private ChatSession? _currentSession;

    public BibleApp(
        IAIService aiService,
        ICharacterRepository characterRepository,
        IChatRepository chatRepository,
        IPrayerRepository prayerRepository,
        ILogger<BibleApp> logger,
        IBibleRAGService? ragService = null)
    {
        _aiService = aiService;
        _characterRepository = characterRepository;
        _chatRepository = chatRepository;
        _prayerRepository = prayerRepository;
        _ragService = ragService;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        System.Console.Clear();
        System.Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        System.Console.WriteLine("║          Welcome to the AI Bible App                       ║");
        System.Console.WriteLine("║    Talk with Biblical Characters & Generate Prayers       ║");
        System.Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        System.Console.WriteLine();

        // Initialize RAG service if available
        if (_ragService != null && !_ragService.IsInitialized)
        {
            System.Console.WriteLine("Initializing Scripture search (RAG)...");
            try
            {
                await _ragService.InitializeAsync();
                System.Console.WriteLine("✓ Scripture search initialized successfully");
                System.Console.WriteLine();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize RAG service");
                System.Console.WriteLine("⚠ Scripture search unavailable (running without RAG)");
                System.Console.WriteLine();
            }
        }

        while (true)
        {
            try
            {
                await ShowMainMenuAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred");
                System.Console.WriteLine($"\n❌ Error: {ex.Message}");
                System.Console.WriteLine("Press any key to continue...");
                System.Console.ReadKey();
            }
        }
    }

    private async Task ShowMainMenuAsync()
    {
        System.Console.Clear();
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");
        System.Console.WriteLine("MAIN MENU");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");
        System.Console.WriteLine("1. Chat with a Biblical Character");
        System.Console.WriteLine("2. Generate Daily Prayer");
        System.Console.WriteLine("3. View Prayer History");
        System.Console.WriteLine("4. View Chat History");
        System.Console.WriteLine("5. Exit");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");
        System.Console.Write("Select an option (1-5): ");

        var choice = System.Console.ReadLine();

        switch (choice)
        {
            case "1":
                await ChatWithCharacterAsync();
                break;
            case "2":
                await GeneratePrayerAsync();
                break;
            case "3":
                await ViewPrayerHistoryAsync();
                break;
            case "4":
                await ViewChatHistoryAsync();
                break;
            case "5":
                System.Console.WriteLine("\nThank you for using the AI Bible App. God bless you!");
                Environment.Exit(0);
                break;
            default:
                System.Console.WriteLine("\n❌ Invalid option. Press any key to try again...");
                System.Console.ReadKey();
                break;
        }
    }

    private async Task ChatWithCharacterAsync()
    {
        System.Console.Clear();
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");
        System.Console.WriteLine("SELECT A BIBLICAL CHARACTER");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");

        var characters = await _characterRepository.GetAllCharactersAsync();
        for (int i = 0; i < characters.Count; i++)
        {
            var character = characters[i];
            System.Console.WriteLine($"{i + 1}. {character.Name} - {character.Title}");
            System.Console.WriteLine($"   {character.Description}");
            System.Console.WriteLine($"   Era: {character.Era}");
            System.Console.WriteLine();
        }

        System.Console.Write($"Select a character (1-{characters.Count}): ");
        if (!int.TryParse(System.Console.ReadLine(), out int selection) || 
            selection < 1 || selection > characters.Count)
        {
            System.Console.WriteLine("\n❌ Invalid selection. Press any key to return...");
            System.Console.ReadKey();
            return;
        }

        var selectedCharacter = characters[selection - 1];
        await StartChatSessionAsync(selectedCharacter);
    }

    private async Task StartChatSessionAsync(BiblicalCharacter character)
    {
        _currentSession = new ChatSession
        {
            CharacterId = character.Id
        };

        System.Console.Clear();
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");
        System.Console.WriteLine($"CHATTING WITH {character.Name.ToUpper()}");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");
        System.Console.WriteLine($"You are now talking with {character.Name}, {character.Title}");
        System.Console.WriteLine("Type 'exit' to end the conversation or 'save' to save this session.");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");
        System.Console.WriteLine();

        while (true)
        {
            System.Console.Write("You: ");
            var userInput = System.Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                _currentSession.EndedAt = DateTime.UtcNow;
                break;
            }

            if (userInput.Equals("save", StringComparison.OrdinalIgnoreCase))
            {
                await _chatRepository.SaveSessionAsync(_currentSession);
                System.Console.WriteLine("\n✓ Chat session saved successfully!");
                System.Console.WriteLine("Press any key to continue...");
                System.Console.ReadKey();
                continue;
            }

            try
            {
                // Add user message to session
                var userMessage = new ChatMessage
                {
                    Role = "user",
                    Content = userInput,
                    CharacterId = character.Id
                };
                _currentSession.Messages.Add(userMessage);

                // Get AI response
                System.Console.Write($"\n{character.Name}: ");
                var response = await _aiService.GetChatResponseAsync(
                    character, 
                    _currentSession.Messages, 
                    userInput);

                System.Console.WriteLine(response);
                System.Console.WriteLine();

                // Add assistant message to session
                var assistantMessage = new ChatMessage
                {
                    Role = "assistant",
                    Content = response,
                    CharacterId = character.Id
                };
                _currentSession.Messages.Add(assistantMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in chat conversation");
                System.Console.WriteLine($"\n❌ Error: {ex.Message}");
                System.Console.WriteLine("Press any key to continue...");
                System.Console.ReadKey();
            }
        }

        System.Console.WriteLine("\nPress any key to return to main menu...");
        System.Console.ReadKey();
    }

    private async Task GeneratePrayerAsync()
    {
        System.Console.Clear();
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");
        System.Console.WriteLine("GENERATE PRAYER");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");
        System.Console.WriteLine("Enter a topic for your prayer (or press Enter for daily prayer):");
        System.Console.Write("Topic: ");
        
        var topic = System.Console.ReadLine() ?? string.Empty;
        
        System.Console.WriteLine("\n⏳ Generating prayer...");

        try
        {
            var prayerContent = await _aiService.GeneratePrayerAsync(topic);
            
            var prayer = new Prayer
            {
                Content = prayerContent,
                Topic = string.IsNullOrEmpty(topic) ? "Daily Prayer" : topic
            };

            System.Console.Clear();
            System.Console.WriteLine("═══════════════════════════════════════════════════════════");
            System.Console.WriteLine($"PRAYER - {prayer.Topic}");
            System.Console.WriteLine("═══════════════════════════════════════════════════════════");
            System.Console.WriteLine();
            System.Console.WriteLine(prayerContent);
            System.Console.WriteLine();
            System.Console.WriteLine("═══════════════════════════════════════════════════════════");
            System.Console.WriteLine();
            System.Console.Write("Would you like to save this prayer? (y/n): ");
            
            var save = System.Console.ReadLine();
            if (save?.Equals("y", StringComparison.OrdinalIgnoreCase) == true)
            {
                await _prayerRepository.SavePrayerAsync(prayer);
                System.Console.WriteLine("\n✓ Prayer saved successfully!");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating prayer");
            System.Console.WriteLine($"\n❌ Error generating prayer: {ex.Message}");
        }

        System.Console.WriteLine("\nPress any key to return to main menu...");
        System.Console.ReadKey();
    }

    private async Task ViewPrayerHistoryAsync()
    {
        System.Console.Clear();
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");
        System.Console.WriteLine("PRAYER HISTORY");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");

        try
        {
            var prayers = await _prayerRepository.GetAllPrayersAsync();
            
            if (prayers.Count == 0)
            {
                System.Console.WriteLine("\nNo saved prayers yet.");
            }
            else
            {
                System.Console.WriteLine($"\nFound {prayers.Count} saved prayer(s):\n");
                
                foreach (var prayer in prayers.OrderByDescending(p => p.CreatedAt))
                {
                    System.Console.WriteLine($"📖 {prayer.Topic}");
                    System.Console.WriteLine($"   Created: {prayer.CreatedAt:yyyy-MM-dd HH:mm}");
                    System.Console.WriteLine($"   {prayer.Content.Substring(0, Math.Min(100, prayer.Content.Length))}...");
                    System.Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error viewing prayer history");
            System.Console.WriteLine($"\n❌ Error: {ex.Message}");
        }

        System.Console.WriteLine("\nPress any key to return to main menu...");
        System.Console.ReadKey();
    }

    private async Task ViewChatHistoryAsync()
    {
        System.Console.Clear();
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");
        System.Console.WriteLine("CHAT HISTORY");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");

        try
        {
            var sessions = await _chatRepository.GetAllSessionsAsync();
            
            if (sessions.Count == 0)
            {
                System.Console.WriteLine("\nNo saved chat sessions yet.");
            }
            else
            {
                System.Console.WriteLine($"\nFound {sessions.Count} saved session(s):\n");
                
                foreach (var session in sessions.OrderByDescending(s => s.StartedAt))
                {
                    var character = await _characterRepository.GetCharacterAsync(session.CharacterId);
                    System.Console.WriteLine($"💬 Chat with {character?.Name ?? "Unknown"}");
                    System.Console.WriteLine($"   Started: {session.StartedAt:yyyy-MM-dd HH:mm}");
                    System.Console.WriteLine($"   Messages: {session.Messages.Count}");
                    System.Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error viewing chat history");
            System.Console.WriteLine($"\n❌ Error: {ex.Message}");
        }

        System.Console.WriteLine("\nPress any key to return to main menu...");
        System.Console.ReadKey();
    }
}
