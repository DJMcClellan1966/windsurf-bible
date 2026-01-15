using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using System.Text.Json;

namespace AI_Bible_App.Maui.Services;

/// <summary>
/// Service for exporting chat conversations to JSONL format for LLM fine-tuning
/// </summary>
public interface ITrainingDataExporter
{
    /// <summary>
    /// Export all rated conversations to JSONL format
    /// </summary>
    Task<string> ExportToJsonlAsync(bool onlyPositiveRatings = false);

    Task<string> ExportPreferencePairsToJsonlAsync();
    
    /// <summary>
    /// Get statistics about available training data
    /// </summary>
    Task<TrainingDataStats> GetStatsAsync();
}

public class TrainingDataStats
{
    public int TotalSessions { get; set; }
    public int TotalMessages { get; set; }
    public int RatedMessages { get; set; }
    public int PositiveRatings { get; set; }
    public int NegativeRatings { get; set; }
    public int MessagesWithFeedback { get; set; }
}

public class TrainingDataExporter : ITrainingDataExporter
{
    private readonly IChatRepository _chatRepository;
    private readonly ICharacterRepository _characterRepository;
    private readonly string _exportPath;

    public TrainingDataExporter(IChatRepository chatRepository, ICharacterRepository characterRepository)
    {
        _chatRepository = chatRepository;
        _characterRepository = characterRepository;
        _exportPath = Path.Combine(FileSystem.AppDataDirectory, "exports");
        Directory.CreateDirectory(_exportPath);
    }

    public async Task<TrainingDataStats> GetStatsAsync()
    {
        var sessions = await _chatRepository.GetAllSessionsAsync();
        var stats = new TrainingDataStats();
        
        foreach (var session in sessions)
        {
            stats.TotalSessions++;
            foreach (var message in session.Messages)
            {
                stats.TotalMessages++;
                if (message.Rating != 0)
                {
                    stats.RatedMessages++;
                    if (message.Rating > 0) stats.PositiveRatings++;
                    else stats.NegativeRatings++;
                }
                if (!string.IsNullOrEmpty(message.Feedback))
                {
                    stats.MessagesWithFeedback++;
                }
            }
        }
        
        return stats;
    }

    public async Task<string> ExportToJsonlAsync(bool onlyPositiveRatings = false)
    {
        var sessions = await _chatRepository.GetAllSessionsAsync();
        var characters = await _characterRepository.GetAllCharactersAsync();
        var characterNames = characters.ToDictionary(c => c.Id, c => c.Name);
        
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var filename = onlyPositiveRatings 
            ? $"training_positive_{timestamp}.jsonl" 
            : $"training_all_{timestamp}.jsonl";
        var filePath = Path.Combine(_exportPath, filename);

        using var writer = new StreamWriter(filePath);
        var exportedCount = 0;

        foreach (var session in sessions)
        {
            // Build conversation pairs (user message -> assistant response)
            for (int i = 0; i < session.Messages.Count - 1; i++)
            {
                var userMsg = session.Messages[i];
                var assistantMsg = session.Messages[i + 1];

                // Only export user -> assistant pairs
                if (userMsg.Role != "user" || assistantMsg.Role != "assistant")
                    continue;

                // Filter by rating if requested
                if (onlyPositiveRatings && assistantMsg.Rating != 1)
                    continue;

                // Skip unrated messages if exporting all (still want some quality signal)
                // Include all rated messages
                if (!onlyPositiveRatings && assistantMsg.Rating == 0)
                    continue;

                var assistantCharacterId = !string.IsNullOrWhiteSpace(assistantMsg.CharacterId)
                    ? assistantMsg.CharacterId
                    : session.CharacterId;
                var assistantCharacterName = characterNames.GetValueOrDefault(assistantCharacterId, assistantCharacterId);

                var trainingEntry = new
                {
                    // Instruction format for fine-tuning
                    instruction = BuildInstruction(assistantCharacterId, assistantCharacterName),
                    input = userMsg.Content,
                    output = assistantMsg.Content,
                    
                    // Metadata for filtering/analysis
                    metadata = new
                    {
                        character_id = assistantCharacterId,
                        character_name = assistantCharacterName,
                        session_type = session.SessionType.ToString(),
                        user_id = session.UserId,
                        rating = assistantMsg.Rating,
                        feedback = assistantMsg.Feedback,
                        timestamp = assistantMsg.Timestamp.ToString("o"),
                        message_id = assistantMsg.Id,
                        session_id = session.Id
                    }
                };

                var json = JsonSerializer.Serialize(trainingEntry, new JsonSerializerOptions 
                { 
                    WriteIndented = false 
                });
                await writer.WriteLineAsync(json);
                exportedCount++;
            }
        }

        System.Diagnostics.Debug.WriteLine($"[DEBUG] Exported {exportedCount} training examples to {filePath}");
        return filePath;
    }

    public async Task<string> ExportPreferencePairsToJsonlAsync()
    {
        var sessions = await _chatRepository.GetAllSessionsAsync();
        var characters = await _characterRepository.GetAllCharactersAsync();
        var characterNames = characters.ToDictionary(c => c.Id, c => c.Name);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var filename = $"training_dpo_pairs_{timestamp}.jsonl";
        var filePath = Path.Combine(_exportPath, filename);

        var groups = new Dictionary<string, List<(ChatMessage Assistant, ChatMessage User, ChatSession Session)>>();

        foreach (var session in sessions)
        {
            for (int i = 0; i < session.Messages.Count - 1; i++)
            {
                var userMsg = session.Messages[i];
                var assistantMsg = session.Messages[i + 1];

                if (userMsg.Role != "user" || assistantMsg.Role != "assistant")
                    continue;

                if (assistantMsg.Rating == 0)
                    continue;

                var assistantCharacterId = !string.IsNullOrWhiteSpace(assistantMsg.CharacterId)
                    ? assistantMsg.CharacterId
                    : session.CharacterId;

                var key = $"{assistantCharacterId}::{userMsg.Content}";
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<(ChatMessage, ChatMessage, ChatSession)>();
                    groups[key] = list;
                }
                list.Add((assistantMsg, userMsg, session));
            }
        }

        using var writer = new StreamWriter(filePath);
        var exportedCount = 0;

        foreach (var kvp in groups)
        {
            var items = kvp.Value;
            var chosen = items
                .Where(x => x.Assistant.Rating > 0)
                .OrderByDescending(x => x.Assistant.Rating)
                .FirstOrDefault();
            var rejected = items
                .Where(x => x.Assistant.Rating < 0)
                .OrderBy(x => x.Assistant.Rating)
                .FirstOrDefault();

            if (chosen.Assistant == null || rejected.Assistant == null)
                continue;

            var assistantCharacterId = !string.IsNullOrWhiteSpace(chosen.Assistant.CharacterId)
                ? chosen.Assistant.CharacterId
                : chosen.Session.CharacterId;
            var assistantCharacterName = characterNames.GetValueOrDefault(assistantCharacterId, assistantCharacterId);

            var entry = new
            {
                prompt = chosen.User.Content,
                chosen = chosen.Assistant.Content,
                rejected = rejected.Assistant.Content,
                metadata = new
                {
                    character_id = assistantCharacterId,
                    character_name = assistantCharacterName,
                    user_id = chosen.Session.UserId,
                    session_type = chosen.Session.SessionType.ToString(),
                    chosen_rating = chosen.Assistant.Rating,
                    rejected_rating = rejected.Assistant.Rating,
                    chosen_feedback = chosen.Assistant.Feedback,
                    rejected_feedback = rejected.Assistant.Feedback,
                    chosen_message_id = chosen.Assistant.Id,
                    rejected_message_id = rejected.Assistant.Id,
                    chosen_session_id = chosen.Session.Id,
                    rejected_session_id = rejected.Session.Id
                }
            };

            var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = false });
            await writer.WriteLineAsync(json);
            exportedCount++;
        }

        System.Diagnostics.Debug.WriteLine($"[DEBUG] Exported {exportedCount} preference pairs to {filePath}");
        return filePath;
    }

    private string BuildInstruction(string characterId, string? characterName)
    {
        var name = characterName ?? characterId;
        return $"You are {name}, a biblical figure. Respond to the user's message in character, " +
               $"drawing from your life experiences and wisdom as recorded in the Bible. " +
               $"Stay true to your personality and historical context.";
    }
}
