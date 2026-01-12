using AI_Bible_App.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AI_Bible_App.Infrastructure.Services;

public class ResearchScheduler
{
    private readonly ILogger<ResearchScheduler> _logger;
    private readonly ICharacterResearchService _researchService;
    private readonly ICharacterUsageTracker _usageTracker;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);
    private readonly CancellationTokenSource _cancellationSource = new();
    private Task? _schedulerTask;

    public ResearchScheduler(
        ILogger<ResearchScheduler> logger,
        ICharacterResearchService researchService,
        ICharacterUsageTracker usageTracker,
        IConfiguration configuration)
    {
        _logger = logger;
        _researchService = researchService;
        _usageTracker = usageTracker;
        _configuration = configuration;
    }

    public void Start()
    {
        _logger.LogInformation("Research Scheduler starting");
        _schedulerTask = ExecuteAsync(_cancellationSource.Token);
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Research Scheduler stopping");
        _cancellationSource.Cancel();
        if (_schedulerTask != null)
            await _schedulerTask;
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Research Scheduler started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);

                // Check if research is enabled
                var enabled = _configuration["AutonomousResearch:Enabled"] == "true";
                if (!enabled)
                {
                    _logger.LogDebug("Research is disabled, skipping check");
                    continue;
                }

                // Check if we're in the research window
                var now = DateTime.Now;
                var startHour = int.TryParse(_configuration["AutonomousResearch:StartHour"], out var sh) ? sh : 2;
                var endHour = int.TryParse(_configuration["AutonomousResearch:EndHour"], out var eh) ? eh : 6;

                if (!IsInResearchWindow(now, startHour, endHour))
                {
                    _logger.LogDebug("Outside research window ({Start}:00-{End}:00), skipping", 
                        startHour, endHour);
                    continue;
                }

                _logger.LogInformation("In research window, checking for characters needing research...");

                // Get top characters needing research (returns character IDs)
                var topCount = int.TryParse(_configuration["AutonomousResearch:TopCharactersCount"], out var tc) ? tc : 5;
                var characterIdsNeedingResearch = await _usageTracker.GetCharactersNeedingResearchAsync();
                
                if (characterIdsNeedingResearch.Count == 0)
                {
                    _logger.LogInformation("No characters need research at this time");
                    continue;
                }

                // Get active research sessions to avoid duplicates
                var activeSessions = await _researchService.GetActiveResearchAsync();
                var activeCharacterIds = activeSessions.Select(s => s.CharacterId).ToHashSet();

                // Start research for characters not already being researched
                var started = 0;
                foreach (var characterId in characterIdsNeedingResearch.Take(topCount))
                {
                    if (activeCharacterIds.Contains(characterId))
                    {
                        _logger.LogDebug("Skipping {Character} - already being researched", 
                            characterId);
                        continue;
                    }

                    try
                    {
                        // Get character stats for logging
                        var stats = await _usageTracker.GetCharacterStatsAsync(characterId);
                        
                        _logger.LogInformation(
                            "Starting research for {Character} (popularity: {Score:F2}, conversations: {Count})",
                            stats.CharacterName, stats.PopularityScore, stats.TotalConversations);

                        await _researchService.StartResearchAsync(
                            characterId, 
                            ResearchPriority.Normal);
                        
                        started++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to start research for {Character}", 
                            characterId);
                    }
                }

                if (started > 0)
                {
                    _logger.LogInformation("Started research for {Count} character(s)", started);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in research scheduler");
            }
        }

        _logger.LogInformation("Research Scheduler stopped");
    }

    private bool IsInResearchWindow(DateTime now, int startHour, int endHour)
    {
        var currentHour = now.Hour;
        
        // Handle wrap-around (e.g., 22:00 - 6:00)
        if (startHour > endHour)
        {
            return currentHour >= startHour || currentHour < endHour;
        }
        
        // Normal range (e.g., 2:00 - 6:00)
        return currentHour >= startHour && currentHour < endHour;
    }
}
