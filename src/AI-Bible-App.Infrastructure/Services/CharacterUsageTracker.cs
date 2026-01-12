using AI_Bible_App.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;

namespace AI_Bible_App.Infrastructure.Services;

public class CharacterUsageTracker : ICharacterUsageTracker
{
    private readonly ILogger<CharacterUsageTracker> _logger;
    private readonly string _dataPath;
    private readonly Dictionary<string, CharacterUsageStats> _usageCache = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public CharacterUsageTracker(ILogger<CharacterUsageTracker> logger)
    {
        _logger = logger;
        _dataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AIBibleApp",
            "Research",
            "usage-stats.json"
        );
        
        _ = LoadUsageStatsAsync();
    }

    public async Task RecordConversationAsync(string characterId, int messageCount, TimeSpan duration)
    {
        await _lock.WaitAsync();
        try
        {
            if (!_usageCache.TryGetValue(characterId, out var stats))
            {
                stats = new CharacterUsageStats
                {
                    CharacterId = characterId,
                    CharacterName = FormatCharacterName(characterId),
                    FirstUsed = DateTime.UtcNow
                };
                _usageCache[characterId] = stats;
            }

            stats.TotalConversations++;
            stats.TotalMessages += messageCount;
            stats.TotalDuration += duration;
            stats.LastUsed = DateTime.UtcNow;
            
            // Calculate popularity score: recent usage weighted higher
            var daysSinceLastUse = (DateTime.UtcNow - stats.LastUsed).TotalDays;
            var recencyWeight = Math.Max(0, 1 - (daysSinceLastUse / 30)); // Decay over 30 days
            stats.PopularityScore = (stats.TotalConversations * recencyWeight) + 
                                   (stats.TotalMessages * 0.1 * recencyWeight);

            await SaveUsageStatsAsync();
            
            _logger.LogInformation(
                "Recorded conversation for {Character}: {Messages} messages, {Duration}s, popularity={Score:F2}",
                characterId, messageCount, duration.TotalSeconds, stats.PopularityScore
            );
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<CharacterUsageStats>> GetTopCharactersAsync(int count = 10)
    {
        await _lock.WaitAsync();
        try
        {
            return _usageCache.Values
                .OrderByDescending(s => s.PopularityScore)
                .Take(count)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<CharacterUsageStats> GetCharacterStatsAsync(string characterId)
    {
        await _lock.WaitAsync();
        try
        {
            if (_usageCache.TryGetValue(characterId, out var stats))
                return stats;
            
            // Return empty stats if not found
            return new CharacterUsageStats
            {
                CharacterId = characterId,
                CharacterName = FormatCharacterName(characterId),
                FirstUsed = DateTime.UtcNow
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<string>> GetCharactersNeedingResearchAsync()
    {
        await _lock.WaitAsync();
        try
        {
            // Characters needing research: popular but low knowledge base entries
            return _usageCache.Values
                .Where(s => s.NeedsResearch || 
                           (s.TotalConversations > 10 && s.KnowledgeBaseEntries < 50))
                .OrderByDescending(s => s.PopularityScore)
                .Select(s => s.CharacterId)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task LoadUsageStatsAsync()
    {
        try
        {
            if (!File.Exists(_dataPath))
            {
                _logger.LogInformation("No existing usage stats found, starting fresh");
                return;
            }

            var json = await File.ReadAllTextAsync(_dataPath);
            var stats = JsonSerializer.Deserialize<List<CharacterUsageStats>>(json);
            
            if (stats != null)
            {
                foreach (var stat in stats)
                {
                    _usageCache[stat.CharacterId] = stat;
                }
                _logger.LogInformation("Loaded usage stats for {Count} characters", stats.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load usage stats from {Path}", _dataPath);
        }
    }

    private async Task SaveUsageStatsAsync()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dataPath)!);
            
            var json = JsonSerializer.Serialize(_usageCache.Values.ToList(), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(_dataPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save usage stats to {Path}", _dataPath);
        }
    }

    private static string FormatCharacterName(string characterId)
    {
        // Convert "moses" to "Moses", "mary_magdalene" to "Mary Magdalene"
        return string.Join(" ", characterId.Split('_')
            .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
    }
}
