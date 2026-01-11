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
            var characterName = characterNames.GetValueOrDefault(session.CharacterId, session.CharacterId);
            
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

                var trainingEntry = new
                {
                    // Instruction format for fine-tuning
                    instruction = BuildInstruction(session.CharacterId, characterName),
                    input = userMsg.Content,
                    output = assistantMsg.Content,
                    
                    // Metadata for filtering/analysis
                    metadata = new
                    {
                        character_id = session.CharacterId,
                        character_name = characterName,
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

    private string BuildInstruction(string characterId, string? characterName)
    {
        var name = characterName ?? characterId;
        return $"You are {name}, a biblical figure. Respond to the user's message in character, " +
               $"drawing from your life experiences and wisdom as recorded in the Bible. " +
               $"Stay true to your personality and historical context.";
    }
}
