using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AI_Bible_App.Infrastructure.Utilities;

/// <summary>
/// Exports rated conversation data for future model fine-tuning.
/// Generates JSONL format compatible with major fine-tuning tools.
/// </summary>
public class TrainingDataExporter
{
    private readonly IChatRepository _chatRepository;
    private readonly ICharacterRepository _characterRepository;
    private readonly ILogger<TrainingDataExporter> _logger;

    public TrainingDataExporter(
        IChatRepository chatRepository,
        ICharacterRepository characterRepository,
        ILogger<TrainingDataExporter> logger)
    {
        _chatRepository = chatRepository;
        _characterRepository = characterRepository;
        _logger = logger;
    }

    /// <summary>
    /// Export all rated conversations to JSONL format for fine-tuning.
    /// Only includes conversations with at least one rated message.
    /// </summary>
    public async Task<ExportResult> ExportRatedConversationsAsync(string outputPath, ExportOptions? options = null)
    {
        options ??= new ExportOptions();
        _logger.LogInformation("Exporting rated conversations to {OutputPath}", outputPath);

        var sessions = await _chatRepository.GetAllSessionsAsync();
        var characters = await _characterRepository.GetAllCharactersAsync();
        var characterDict = characters.ToDictionary(c => c.Id, c => c);

        var exportedCount = 0;
        var positiveCount = 0;
        var negativeCount = 0;

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var writer = new StreamWriter(outputPath);

        foreach (var session in sessions)
        {
            if (!characterDict.TryGetValue(session.CharacterId, out var character))
                continue;

            // Find all user-assistant message pairs with ratings
            for (int i = 0; i < session.Messages.Count - 1; i++)
            {
                var userMsg = session.Messages[i];
                var assistantMsg = session.Messages[i + 1];

                // Skip if not a user->assistant pair
                if (userMsg.Role != "user" || assistantMsg.Role != "assistant")
                    continue;

                // Skip if no rating
                if (assistantMsg.Rating == 0)
                    continue;

                // Apply rating filter
                if (options.OnlyPositive && assistantMsg.Rating < 0)
                    continue;

                // Build the training example
                var example = new TrainingExample
                {
                    SystemPrompt = BuildSystemPrompt(character),
                    UserMessage = userMsg.Content,
                    AssistantMessage = assistantMsg.Content,
                    Rating = assistantMsg.Rating,
                    CharacterId = character.Id,
                    CharacterName = character.Name,
                    Timestamp = assistantMsg.Timestamp
                };

                // Write in JSONL format
                var json = JsonSerializer.Serialize(example, new JsonSerializerOptions { WriteIndented = false });
                await writer.WriteLineAsync(json);

                exportedCount++;
                if (assistantMsg.Rating > 0) positiveCount++;
                else negativeCount++;
            }
        }

        var result = new ExportResult
        {
            TotalExported = exportedCount,
            PositiveRatings = positiveCount,
            NegativeRatings = negativeCount,
            OutputPath = outputPath
        };

        _logger.LogInformation("Exported {Total} training examples ({Positive} positive, {Negative} negative)",
            exportedCount, positiveCount, negativeCount);

        return result;
    }

    /// <summary>
    /// Export in Alpaca/Stanford format for instruction fine-tuning
    /// </summary>
    public async Task<ExportResult> ExportAlpacaFormatAsync(string outputPath, ExportOptions? options = null)
    {
        options ??= new ExportOptions { OnlyPositive = true };
        _logger.LogInformation("Exporting in Alpaca format to {OutputPath}", outputPath);

        var sessions = await _chatRepository.GetAllSessionsAsync();
        var characters = await _characterRepository.GetAllCharactersAsync();
        var characterDict = characters.ToDictionary(c => c.Id, c => c);

        var examples = new List<AlpacaExample>();

        foreach (var session in sessions)
        {
            if (!characterDict.TryGetValue(session.CharacterId, out var character))
                continue;

            for (int i = 0; i < session.Messages.Count - 1; i++)
            {
                var userMsg = session.Messages[i];
                var assistantMsg = session.Messages[i + 1];

                if (userMsg.Role != "user" || assistantMsg.Role != "assistant")
                    continue;

                // Only include highly rated responses for Alpaca format
                if (assistantMsg.Rating <= 0)
                    continue;

                examples.Add(new AlpacaExample
                {
                    Instruction = $"You are {character.Name}, {character.Description}. Respond to the user as this biblical character.",
                    Input = userMsg.Content,
                    Output = assistantMsg.Content
                });
            }
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(examples, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json);

        return new ExportResult
        {
            TotalExported = examples.Count,
            PositiveRatings = examples.Count,
            NegativeRatings = 0,
            OutputPath = outputPath
        };
    }

    /// <summary>
    /// Export in DPO (Direct Preference Optimization) format with chosen/rejected pairs
    /// </summary>
    public async Task<ExportResult> ExportDpoFormatAsync(string outputPath)
    {
        _logger.LogInformation("Exporting in DPO format to {OutputPath}", outputPath);

        var sessions = await _chatRepository.GetAllSessionsAsync();
        var characters = await _characterRepository.GetAllCharactersAsync();
        var characterDict = characters.ToDictionary(c => c.Id, c => c);

        // Group messages by prompt to find preference pairs
        var promptGroups = new Dictionary<string, List<(ChatMessage msg, BiblicalCharacter character)>>();

        foreach (var session in sessions)
        {
            if (!characterDict.TryGetValue(session.CharacterId, out var character))
                continue;

            for (int i = 0; i < session.Messages.Count - 1; i++)
            {
                var userMsg = session.Messages[i];
                var assistantMsg = session.Messages[i + 1];

                if (userMsg.Role != "user" || assistantMsg.Role != "assistant")
                    continue;

                if (assistantMsg.Rating == 0)
                    continue;

                var key = $"{character.Id}:{userMsg.Content}";
                if (!promptGroups.ContainsKey(key))
                    promptGroups[key] = new List<(ChatMessage, BiblicalCharacter)>();

                promptGroups[key].Add((assistantMsg, character));
            }
        }

        var dpoExamples = new List<DpoExample>();

        // Find pairs where we have both positive and negative ratings for same prompt
        foreach (var group in promptGroups.Where(g => g.Value.Count >= 2))
        {
            var positive = group.Value.FirstOrDefault(m => m.msg.Rating > 0);
            var negative = group.Value.FirstOrDefault(m => m.msg.Rating < 0);

            if (positive.msg != null && negative.msg != null)
            {
                var prompt = group.Key.Split(':')[1];
                dpoExamples.Add(new DpoExample
                {
                    Prompt = prompt,
                    Chosen = positive.msg.Content,
                    Rejected = negative.msg.Content
                });
            }
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var writer = new StreamWriter(outputPath);
        foreach (var example in dpoExamples)
        {
            var json = JsonSerializer.Serialize(example, new JsonSerializerOptions { WriteIndented = false });
            await writer.WriteLineAsync(json);
        }

        return new ExportResult
        {
            TotalExported = dpoExamples.Count,
            PositiveRatings = dpoExamples.Count,
            NegativeRatings = dpoExamples.Count,
            OutputPath = outputPath
        };
    }

    private string BuildSystemPrompt(BiblicalCharacter character)
    {
        return $"You are {character.Name}, {character.Description}. " +
               $"You lived during {character.Era}. " +
               $"Speak authentically as this biblical character would.";
    }
}

public class ExportOptions
{
    public bool OnlyPositive { get; set; } = false;
    public int MinRating { get; set; } = 0;
}

public class ExportResult
{
    public int TotalExported { get; set; }
    public int PositiveRatings { get; set; }
    public int NegativeRatings { get; set; }
    public string OutputPath { get; set; } = string.Empty;
}

public class TrainingExample
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    public string AssistantMessage { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string CharacterId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class AlpacaExample
{
    public string Instruction { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
}

public class DpoExample
{
    public string Prompt { get; set; } = string.Empty;
    public string Chosen { get; set; } = string.Empty;
    public string Rejected { get; set; } = string.Empty;
}
