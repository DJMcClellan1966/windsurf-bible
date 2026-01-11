using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Logging;

namespace AI_Bible_App.Infrastructure.Repositories;

/// <summary>
/// Repository for managing conversation memory and character-user history
/// </summary>
public class ConversationMemoryRepository : JsonRepositoryBase<ConversationMemory>
{
    public ConversationMemoryRepository(
        ILogger<ConversationMemoryRepository> logger,
        string dataDirectory = "data")
        : base(logger, "conversation_memories.json", null, null, dataDirectory)
    {
    }

    protected override string GetEntityId(ConversationMemory entity) => entity.Id;
    protected override string EntityTypeName => "conversation memory";

    public async Task<ConversationMemory?> GetByUserAndCharacterAsync(string userId, string characterId)
    {
        var all = await LoadAllAsync();
        return all.FirstOrDefault(m => m.UserId == userId && m.CharacterId == characterId);
    }

    public async Task<List<ConversationMemory>> GetByUserIdAsync(string userId)
    {
        var all = await LoadAllAsync();
        return all.Where(m => m.UserId == userId).ToList();
    }

    public async Task AddThemeAsync(string userId, string characterId, string theme)
    {
        var memory = await GetByUserAndCharacterAsync(userId, characterId);
        
        if (memory == null)
        {
            memory = new ConversationMemory
            {
                UserId = userId,
                CharacterId = characterId,
                FirstInteraction = DateTime.UtcNow
            };
            memory.ThemeFrequency[theme] = 1;
            await UpsertAsync(memory, memory.Id);
        }
        else
        {
            if (memory.ThemeFrequency.ContainsKey(theme))
                memory.ThemeFrequency[theme]++;
            else
                memory.ThemeFrequency[theme] = 1;
                
            memory.LastInteraction = DateTime.UtcNow;
            await UpsertAsync(memory, memory.Id);
        }
    }

    public async Task AddSummaryAsync(string userId, string characterId, ConversationSummary summary)
    {
        var memory = await GetByUserAndCharacterAsync(userId, characterId);
        
        if (memory != null)
        {
            memory.RecentSummaries.Insert(0, summary);
            
            // Keep only last 10 summaries
            if (memory.RecentSummaries.Count > 10)
            {
                memory.RecentSummaries = memory.RecentSummaries.Take(10).ToList();
            }
            
            memory.LastInteraction = DateTime.UtcNow;
            await UpsertAsync(memory, memory.Id);
        }
    }
}
