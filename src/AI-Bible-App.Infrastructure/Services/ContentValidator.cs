using AI_Bible_App.Core.Services;
using AI_Bible_App.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AI_Bible_App.Infrastructure.Services;

public class ContentValidator
{
    private readonly ILogger<ContentValidator> _logger;
    private readonly LocalAIService _aiService;
    private readonly WebScrapingService _scrapingService;

    public ContentValidator(
        ILogger<ContentValidator> logger,
        LocalAIService aiService,
        WebScrapingService scrapingService)
    {
        _logger = logger;
        _aiService = aiService;
        _scrapingService = scrapingService;
    }

    public async Task<ValidationResult> ValidateFindingAsync(
        string characterId,
        string topic,
        List<ScrapedContent> sources)
    {
        var result = new ValidationResult
        {
            IsValid = false,
            Sources = sources.Select(s => s.Url).ToList()
        };

        // Step 1: Check minimum source count
        if (sources.Count < 2)
        {
            result.RejectReason = "Insufficient sources (need at least 2)";
            _logger.LogWarning("Rejected finding for {Character}/{Topic}: only {Count} source(s)", 
                characterId, topic, sources.Count);
            return result;
        }

        // Step 2: Cross-reference content
        var commonClaims = ExtractCommonClaims(sources);
        if (commonClaims.Count == 0)
        {
            result.RejectReason = "No agreement between sources";
            _logger.LogWarning("Rejected finding for {Character}/{Topic}: sources don't agree", 
                characterId, topic);
            return result;
        }

        // Step 3: Check for controversy markers
        var controversyDetected = DetectControversy(sources);
        if (controversyDetected)
        {
            result.RequiresHumanReview = true;
            result.ReviewReason = "Controversial content detected - sources disagree on key facts";
            _logger.LogInformation("Flagged finding for {Character}/{Topic} for human review: controversy", 
                characterId, topic);
        }

        // Step 4: AI validation for anachronisms
        var aiValidation = await ValidateWithAIAsync(characterId, topic, commonClaims, sources);
        if (!aiValidation.IsValid)
        {
            result.RejectReason = aiValidation.RejectReason;
            result.RequiresHumanReview = aiValidation.RequiresReview;
            result.ReviewReason = aiValidation.ReviewReason;
            return result;
        }

        // Step 5: Calculate confidence level
        result.Confidence = CalculateConfidenceLevel(sources);
        result.IsValid = true;
        result.ValidatedClaims = commonClaims;

        _logger.LogInformation(
            "Validated finding for {Character}/{Topic}: {Confidence} confidence, {Claims} claims from {Sources} sources",
            characterId, topic, result.Confidence, commonClaims.Count, sources.Count);

        return result;
    }

    private List<string> ExtractCommonClaims(List<ScrapedContent> sources)
    {
        // Simple approach: look for sentences that appear in multiple sources
        // In production, this would use NLP for semantic similarity
        var commonClaims = new List<string>();

        if (sources.Count < 2)
            return commonClaims;

        // Extract sentences from each source
        var sourceSentences = sources.Select(s => 
            s.Content.Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Select(sent => sent.Trim())
                .Where(sent => sent.Length > 30 && sent.Length < 300)
                .ToList()
        ).ToList();

        // Find claims that appear in at least 2 sources (simple word overlap)
        foreach (var sent1 in sourceSentences[0])
        {
            var words1 = sent1.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var otherSentences in sourceSentences.Skip(1))
            {
                foreach (var sent2 in otherSentences)
                {
                    var words2 = sent2.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var overlap = words1.Intersect(words2).Count();
                    
                    // If 60%+ word overlap, consider it a common claim
                    if (overlap >= Math.Min(words1.Length, words2.Length) * 0.6)
                    {
                        if (!commonClaims.Any(c => c.ToLowerInvariant().Contains(sent1.ToLowerInvariant())))
                        {
                            commonClaims.Add(sent1);
                            break;
                        }
                    }
                }
            }
        }

        return commonClaims.Take(10).ToList(); // Limit to top 10 claims
    }

    private bool DetectControversy(List<ScrapedContent> sources)
    {
        var controversyMarkers = new[]
        {
            "scholars disagree",
            "debated",
            "controversial",
            "disputed",
            "unclear",
            "possibly",
            "may have",
            "might have",
            "some believe",
            "others argue",
            "conflicting accounts"
        };

        foreach (var source in sources)
        {
            var content = source.Content.ToLowerInvariant();
            if (controversyMarkers.Any(marker => content.Contains(marker)))
            {
                _logger.LogDebug("Controversy marker found in {Url}: contains '{Marker}'", 
                    source.Url, controversyMarkers.First(m => content.Contains(m)));
                return true;
            }
        }

        return false;
    }

    private async Task<AIValidationResult> ValidateWithAIAsync(
        string characterId,
        string topic,
        List<string> claims,
        List<ScrapedContent> sources)
    {
        try
        {
            var sourceSummary = string.Join("\n", sources.Select(s => 
                $"- {s.Domain}: {s.Content.Substring(0, Math.Min(500, s.Content.Length))}..."));
            
            var claimsSummary = string.Join("\n", claims.Select((c, i) => $"{i+1}. {c}"));

            var prompt = $@"You are a biblical historian. Validate this research finding:

CHARACTER: {characterId}
TOPIC: {topic}

CLAIMS TO VALIDATE:
{claimsSummary}

SOURCES:
{sourceSummary}

Check for:
1. Anachronisms (modern concepts applied to ancient times)
2. Contradictions with known biblical timeline
3. Implausible claims

Respond in this format:
VALID: yes/no/review
REASON: brief explanation
CONCERNS: list any specific issues";

            var messages = new List<AI_Bible_App.Core.Models.ChatMessage>
            {
                new() { Role = "user", Content = prompt }
            };
            
            var response = _aiService.StreamChatResponseAsync(null, messages, "phi3:mini");
            var fullResponse = string.Empty;
            await foreach (var chunk in response)
            {
                fullResponse += chunk;
            }
            
            var result = new AIValidationResult { IsValid = true };
            
            if (fullResponse.ToLowerInvariant().Contains("valid: no"))
            {
                result.IsValid = false;
                result.RejectReason = "AI validation failed: " + ExtractReason(fullResponse);
                _logger.LogWarning("AI rejected finding for {Character}/{Topic}: {Reason}", 
                    characterId, topic, result.RejectReason);
            }
            else if (fullResponse.ToLowerInvariant().Contains("valid: review"))
            {
                result.IsValid = true;
                result.RequiresReview = true;
                result.ReviewReason = "AI flagged for review: " + ExtractReason(fullResponse);
                _logger.LogInformation("AI flagged finding for {Character}/{Topic} for review", 
                    characterId, topic);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI validation failed for {Character}/{Topic}", characterId, topic);
            
            // On error, require human review as safety measure
            return new AIValidationResult
            {
                IsValid = true,
                RequiresReview = true,
                ReviewReason = "AI validation error - requires manual review"
            };
        }
    }

    private string ExtractReason(string response)
    {
        var match = Regex.Match(response, @"REASON:\s*(.+?)(?:\n|$)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : "See validation details";
    }

    private AI_Bible_App.Core.Services.ConfidenceLevel CalculateConfidenceLevel(List<ScrapedContent> sources)
    {
        var tier1Count = sources.Count(s => _scrapingService.GetSourceTier(s.Url) == SourceTier.Tier1);
        var tier2Count = sources.Count(s => _scrapingService.GetSourceTier(s.Url) == SourceTier.Tier2);
        var totalSources = sources.Count;

        // Very High: 3+ sources with at least 2 Tier 1
        if (totalSources >= 3 && tier1Count >= 2)
            return AI_Bible_App.Core.Services.ConfidenceLevel.VeryHigh;
        
        // High: Multiple Tier 1 sources
        if (tier1Count >= 2)
            return AI_Bible_App.Core.Services.ConfidenceLevel.High;
        
        // Medium: Mix of Tier 1 and Tier 2
        if (tier1Count >= 1 && tier2Count >= 1)
            return AI_Bible_App.Core.Services.ConfidenceLevel.Medium;
        
        // Low: Only Tier 2/3 sources
        return AI_Bible_App.Core.Services.ConfidenceLevel.Low;
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Sources { get; set; } = new();
    public List<string> ValidatedClaims { get; set; } = new();
    public AI_Bible_App.Core.Services.ConfidenceLevel Confidence { get; set; }
    public bool RequiresHumanReview { get; set; }
    public string? ReviewReason { get; set; }
    public string? RejectReason { get; set; }
}

public class AIValidationResult
{
    public bool IsValid { get; set; }
    public bool RequiresReview { get; set; }
    public string? ReviewReason { get; set; }
    public string? RejectReason { get; set; }
}
