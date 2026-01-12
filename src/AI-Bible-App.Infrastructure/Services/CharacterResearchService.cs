using AI_Bible_App.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AI_Bible_App.Infrastructure.Services;

public class CharacterResearchService : ICharacterResearchService
{
    private readonly ILogger<CharacterResearchService> _logger;
    private readonly ICharacterUsageTracker _usageTracker;
    private readonly WebScrapingService _scrapingService;
    private readonly ContentValidator _validator;
    private readonly IKnowledgeBaseService _knowledgeBase;
    private readonly IConfiguration _configuration;
    private readonly string _dataPath;
    private readonly Dictionary<string, ResearchSession> _activeSessions = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    // Research topics per character
    private readonly Dictionary<string, List<string>> _researchTopics = new()
    {
        ["moses"] = new()
        {
            "Egyptian New Kingdom slavery practices",
            "Ramesses II and the Exodus",
            "Ancient Egyptian loanwords in Hebrew",
            "Tabernacle construction materials",
            "Bronze Age exodus theories"
        },
        ["david"] = new()
        {
            "United Monarchy archaeological evidence",
            "Philistine warfare tactics",
            "Iron Age shepherd life in Judah",
            "Psalm composition traditions",
            "Tel Dan Stele significance"
        },
        ["ruth"] = new()
        {
            "Judges period Moabite culture",
            "Ancient gleaning laws",
            "Levirate marriage customs",
            "Bethlehem in Bronze Age",
            "Moabite language similarities"
        },
        ["paul"] = new()
        {
            "Roman citizenship privileges",
            "Tarsus in 1st century",
            "Tent-making trade in antiquity",
            "Pharisaic education under Gamaliel",
            "Roman travel routes to Damascus"
        },
        ["peter"] = new()
        {
            "Galilean fishing industry",
            "Aramaic language in Galilee",
            "Capernaum archaeological finds",
            "Jewish-Gentile relations 1st century",
            "Early church leadership structure"
        }
    };

    // URL templates for research
    private readonly Dictionary<string, List<string>> _searchUrls = new()
    {
        ["biblehub.com"] = new() { "https://biblehub.com/commentaries/{character}/{topic}.htm" },
        ["blueletterbible.org"] = new() { "https://www.blueletterbible.org/search/search.cfm?q={topic}" },
        ["biblicalarchaeology.org"] = new() { "https://www.biblicalarchaeology.org/?s={topic}" }
    };

    public CharacterResearchService(
        ILogger<CharacterResearchService> logger,
        ICharacterUsageTracker usageTracker,
        WebScrapingService scrapingService,
        ContentValidator validator,
        IKnowledgeBaseService knowledgeBase,
        IConfiguration configuration)
    {
        _logger = logger;
        _usageTracker = usageTracker;
        _scrapingService = scrapingService;
        _validator = validator;
        _knowledgeBase = knowledgeBase;
        _configuration = configuration;
        
        _dataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AIBibleApp",
            "Research"
        );
        
        Directory.CreateDirectory(_dataPath);
    }

    public async Task<ResearchSession> StartResearchAsync(string characterId, ResearchPriority priority)
    {
        await _lock.WaitAsync();
        try
        {
            // Check if already researching this character
            if (_activeSessions.ContainsKey(characterId))
            {
                _logger.LogWarning("Research already active for {Character}", characterId);
                return _activeSessions[characterId];
            }

            // Get topics for this character
            var topics = GetResearchTopics(characterId);
            if (topics.Count == 0)
            {
                throw new InvalidOperationException($"No research topics defined for {characterId}");
            }

            var session = new ResearchSession
            {
                Id = Guid.NewGuid().ToString(),
                CharacterId = characterId,
                Priority = priority,
                StartedAt = DateTime.UtcNow,
                Status = ResearchStatus.Scraping,
                CurrentTopics = topics
            };

            _activeSessions[characterId] = session;
            _logger.LogInformation("Started research session {SessionId} for {Character} with {TopicCount} topics",
                session.Id, characterId, topics.Count);

            // Start research in background
            _ = Task.Run(() => PerformResearchAsync(session));

            return session;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<ResearchSession>> GetActiveResearchAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return _activeSessions.Values.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<ResearchFinding>> GetPendingFindingsAsync(string? characterId = null)
    {
        var findingsPath = Path.Combine(_dataPath, "findings-pending.json");
        if (!File.Exists(findingsPath))
            return new List<ResearchFinding>();

        try
        {
            var json = await File.ReadAllTextAsync(findingsPath);
            var findings = JsonSerializer.Deserialize<List<ResearchFinding>>(json) ?? new();
            
            var pendingFindings = findings.Where(f => f.ReviewStatus == ReviewStatus.Pending);
            
            if (!string.IsNullOrEmpty(characterId))
            {
                pendingFindings = pendingFindings.Where(f => f.CharacterId == characterId);
            }
            
            return pendingFindings.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load pending findings");
            return new List<ResearchFinding>();
        }
    }

    public async Task<bool> ApproveFindingAsync(string findingId)
    {
        var finding = await GetFindingByIdAsync(findingId);
        if (finding == null)
            return false;

        finding.ReviewStatus = ReviewStatus.Approved;
        finding.ReviewedAt = DateTime.UtcNow;

        await SaveFindingAsync(finding);
        await IntegrateFindingIntoKnowledgeBaseAsync(finding);

        _logger.LogInformation("Approved finding {FindingId} for {Character}", findingId, finding.CharacterId);
        return true;
    }

    public async Task<bool> RejectFindingAsync(string findingId, string reason)
    {
        var finding = await GetFindingByIdAsync(findingId);
        if (finding == null)
            return false;

        finding.ReviewStatus = ReviewStatus.Rejected;
        finding.RejectionReason = reason;
        finding.ReviewedAt = DateTime.UtcNow;

        await SaveFindingAsync(finding);

        _logger.LogInformation("Rejected finding {FindingId} for {Character}: {Reason}", 
            findingId, finding.CharacterId, reason);
        return true;
    }

    public async Task<ResearchStatistics> GetStatisticsAsync()
    {
        var stats = new ResearchStatistics
        {
            ResearchEnabled = _configuration["AutonomousResearch:Enabled"] == "true",
            TotalCharactersResearched = _activeSessions.Count
        };

        // Load all findings
        var findings = await LoadAllFindingsAsync();
        stats.TotalFindingsCollected = findings.Count;
        stats.TotalFindingsApproved = findings.Count(f => f.ReviewStatus == ReviewStatus.Approved);
        stats.TotalFindingsRejected = findings.Count(f => f.ReviewStatus == ReviewStatus.Rejected);
        stats.PendingReviews = findings.Count(f => f.ReviewStatus == ReviewStatus.Pending);

        stats.FindingsByCharacter = findings
            .GroupBy(f => f.CharacterId)
            .ToDictionary(g => g.Key, g => g.Count());

        stats.FindingsByType = findings
            .GroupBy(f => f.Type)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        stats.FindingsByConfidence = findings
            .GroupBy(f => f.Confidence)
            .ToDictionary(g => g.Key, g => g.Count());

        if (stats.TotalFindingsCollected > 0)
        {
            stats.ApprovalRate = (double)stats.TotalFindingsApproved / stats.TotalFindingsCollected;
        }

        return stats;
    }

    public Task SetResearchEnabledAsync(bool enabled)
    {
        // This would update configuration - for now just log
        _logger.LogInformation("Research enabled set to: {Enabled}", enabled);
        return Task.CompletedTask;
    }

    public Task SetResearchScheduleAsync(TimeSpan startTime, TimeSpan endTime)
    {
        _logger.LogInformation("Research schedule set to: {Start} - {End}", startTime, endTime);
        return Task.CompletedTask;
    }

    private async Task PerformResearchAsync(ResearchSession session)
    {
        try
        {
            session.Status = ResearchStatus.Scraping;
            
            foreach (var topic in session.CurrentTopics.Take(5)) // Limit to 5 topics per session
            {
                try
                {
                    _logger.LogInformation("Researching {Character}: {Topic}", session.CharacterId, topic);
                    
                    // Generate search URLs
                    var urls = GenerateSearchUrls(session.CharacterId, topic);
                    
                    // Scrape content
                    var scrapedContent = await _scrapingService.ScrapeMultipleAsync(urls);
                    session.TopicsResearched++;

                    if (scrapedContent.Count < 2)
                    {
                        _logger.LogWarning("Insufficient sources for {Character}/{Topic}: only {Count}",
                            session.CharacterId, topic, scrapedContent.Count);
                        continue;
                    }

                    session.FindingsCollected += scrapedContent.Count;
                    session.Status = ResearchStatus.Validating;

                    // Validate content
                    var validation = await _validator.ValidateFindingAsync(
                        session.CharacterId, topic, scrapedContent);

                    if (validation.IsValid)
                    {
                        session.FindingsValidated++;
                        
                        // Create finding
                        var finding = new ResearchFinding
                        {
                            Id = Guid.NewGuid().ToString(),
                            CharacterId = session.CharacterId,
                            Type = DetermineType(topic),
                            Title = topic,
                            Content = string.Join("\n\n", validation.ValidatedClaims),
                            Sources = validation.Sources,
                            Confidence = validation.Confidence,
                            RequiresReview = validation.RequiresHumanReview,
                            ReviewStatus = validation.RequiresHumanReview ? 
                                ReviewStatus.Pending : ReviewStatus.AutoApproved,
                            DiscoveredAt = DateTime.UtcNow
                        };

                        await SaveFindingAsync(finding);

                        if (!finding.RequiresReview)
                        {
                            session.FindingsApproved++;
                            await IntegrateFindingIntoKnowledgeBaseAsync(finding);
                        }

                        _logger.LogInformation(
                            "Finding created for {Character}/{Topic}: {Confidence}, Review={NeedsReview}",
                            session.CharacterId, topic, finding.Confidence, finding.RequiresReview);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error researching {Character}/{Topic}", 
                        session.CharacterId, topic);
                }
            }

            session.Status = ResearchStatus.Completed;
            _logger.LogInformation(
                "Completed research for {Character}: {Topics} topics, {Findings} findings, {Approved} approved",
                session.CharacterId, session.TopicsResearched, session.FindingsCollected, 
                session.FindingsApproved);
        }
        catch (Exception ex)
        {
            session.Status = ResearchStatus.Failed;
            session.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Research session failed for {Character}", session.CharacterId);
        }
        finally
        {
            await _lock.WaitAsync();
            try
            {
                _activeSessions.Remove(session.CharacterId);
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    private List<string> GetResearchTopics(string characterId)
    {
        return _researchTopics.TryGetValue(characterId.ToLowerInvariant(), out var topics) 
            ? topics 
            : new List<string>();
    }

    private List<string> GenerateSearchUrls(string characterId, string topic)
    {
        // For now, return hardcoded example URLs
        // In production, this would dynamically construct search URLs
        return new List<string>
        {
            $"https://biblehub.com/commentaries/{characterId}/1-1.htm",
            $"https://www.biblicalarchaeology.org/?s={Uri.EscapeDataString(topic)}"
        };
    }

    private FindingType DetermineType(string topic)
    {
        var topicLower = topic.ToLowerInvariant();
        
        if (topicLower.Contains("language") || topicLower.Contains("word") || topicLower.Contains("hebrew"))
            return FindingType.LanguageInsight;
        
        if (topicLower.Contains("culture") || topicLower.Contains("custom") || topicLower.Contains("practice"))
            return FindingType.CulturalInsight;
        
        if (topicLower.Contains("archaeological") || topicLower.Contains("excavation") || topicLower.Contains("artifact"))
            return FindingType.Archaeological;
        
        if (topicLower.Contains("geography") || topicLower.Contains("location") || topicLower.Contains("place"))
            return FindingType.Geographical;
        
        return FindingType.HistoricalContext;
    }

    private async Task SaveFindingAsync(ResearchFinding finding)
    {
        var fileName = finding.ReviewStatus == ReviewStatus.Pending || finding.ReviewStatus == ReviewStatus.AutoApproved
            ? "findings-pending.json"
            : "findings-approved.json";
        
        var filePath = Path.Combine(_dataPath, fileName);
        
        List<ResearchFinding> findings;
        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath);
            findings = JsonSerializer.Deserialize<List<ResearchFinding>>(json) ?? new();
        }
        else
        {
            findings = new List<ResearchFinding>();
        }

        findings.Add(finding);
        
        var updatedJson = JsonSerializer.Serialize(findings, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        await File.WriteAllTextAsync(filePath, updatedJson);
    }

    private async Task<ResearchFinding?> GetFindingByIdAsync(string findingId)
    {
        var findings = await LoadAllFindingsAsync();
        return findings.FirstOrDefault(f => f.Id == findingId);
    }

    private async Task<List<ResearchFinding>> LoadAllFindingsAsync()
    {
        var allFindings = new List<ResearchFinding>();
        
        foreach (var fileName in new[] { "findings-pending.json", "findings-approved.json" })
        {
            var filePath = Path.Combine(_dataPath, fileName);
            if (File.Exists(filePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    var findings = JsonSerializer.Deserialize<List<ResearchFinding>>(json);
                    if (findings != null)
                        allFindings.AddRange(findings);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load {FileName}", fileName);
                }
            }
        }
        
        return allFindings;
    }

    private async Task IntegrateFindingIntoKnowledgeBaseAsync(ResearchFinding finding)
    {
        try
        {
            // TODO: Add entry method to IKnowledgeBaseService
            // For now, just save to findings file - knowledge base integration coming later
            _logger.LogInformation("Finding {FindingId} ready for knowledge base integration for {Character}",
                finding.Id, finding.CharacterId);
            
            // In production, this would call knowledge base service to add the finding
            // await _knowledgeBase.AddHistoricalContextAsync(characterId, finding.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to integrate finding {FindingId} into knowledge base", finding.Id);
        }
    }
}
