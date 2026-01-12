using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace AI_Bible_App.Infrastructure.Services;

public class WebScrapingService
{
    private readonly ILogger<WebScrapingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly HashSet<string> _whitelistedDomains;

    public WebScrapingService(
        ILogger<WebScrapingService> logger,
        IConfiguration configuration,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        // Add user agent to avoid being blocked
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AI-Bible-App/1.0 (Biblical Research)");

        // Load whitelisted sources from config
        var sourcesSection = configuration.GetSection("AutonomousResearch:WhitelistedSources");
        var sources = new List<string>();
        foreach (var child in sourcesSection.GetChildren())
        {
            var value = child.Value;
            if (!string.IsNullOrEmpty(value))
                sources.Add(value);
        }
        _whitelistedDomains = new HashSet<string>(sources, StringComparer.OrdinalIgnoreCase);
        
        _logger.LogInformation("Initialized with {Count} whitelisted domains", _whitelistedDomains.Count);
    }

    public async Task<ScrapedContent?> ScrapeUrlAsync(string url)
    {
        try
        {
            // Validate URL is whitelisted
            if (!IsWhitelisted(url))
            {
                _logger.LogWarning("Rejected non-whitelisted URL: {Url}", url);
                return null;
            }

            _logger.LogInformation("Scraping: {Url}", url);
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch {Url}: {Status}", url, response.StatusCode);
                return null;
            }

            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Extract main content
            var content = ExtractMainContent(doc, url);
            
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("No content extracted from {Url}", url);
                return null;
            }

            return new ScrapedContent
            {
                Url = url,
                Domain = GetDomain(url),
                Title = ExtractTitle(doc),
                Content = content,
                ScrapedAt = DateTime.UtcNow,
                WordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping {Url}", url);
            return null;
        }
    }

    public async Task<List<ScrapedContent>> ScrapeMultipleAsync(List<string> urls)
    {
        var tasks = urls.Select(url => ScrapeUrlAsync(url));
        var results = await Task.WhenAll(tasks);
        return results.Where(r => r != null).ToList()!;
    }

    private bool IsWhitelisted(string url)
    {
        try
        {
            var uri = new Uri(url);
            var domain = uri.Host.ToLowerInvariant();
            
            // Remove www. prefix for matching
            if (domain.StartsWith("www."))
                domain = domain.Substring(4);
            
            return _whitelistedDomains.Any(whitelisted => 
                domain.Equals(whitelisted, StringComparison.OrdinalIgnoreCase) ||
                domain.EndsWith("." + whitelisted, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    private string GetDomain(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            return "unknown";
        }
    }

    private string ExtractTitle(HtmlDocument doc)
    {
        var titleNode = doc.DocumentNode.SelectSingleNode("//title") ??
                       doc.DocumentNode.SelectSingleNode("//h1");
        return titleNode?.InnerText.Trim() ?? "Untitled";
    }

    private string ExtractMainContent(HtmlDocument doc, string url)
    {
        // Try different content selectors based on common patterns
        var contentSelectors = new[]
        {
            "//article",
            "//main",
            "//*[@class='content']",
            "//*[@id='content']",
            "//*[@class='article-content']",
            "//*[@class='entry-content']",
            "//body"
        };

        foreach (var selector in contentSelectors)
        {
            var node = doc.DocumentNode.SelectSingleNode(selector);
            if (node != null)
            {
                // Remove script and style tags
                foreach (var script in node.Descendants("script").ToList())
                    script.Remove();
                foreach (var style in node.Descendants("style").ToList())
                    style.Remove();
                
                var text = node.InnerText;
                
                // Clean up whitespace
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
                text = text.Trim();
                
                if (text.Length > 200) // Minimum content threshold
                {
                    _logger.LogDebug("Extracted {Length} chars using selector: {Selector}", 
                        text.Length, selector);
                    return text;
                }
            }
        }

        return string.Empty;
    }

    public SourceTier GetSourceTier(string url)
    {
        var domain = GetDomain(url).ToLowerInvariant();
        
        // Tier 1: Highest trust - Biblical scholarship sites
        if (domain.Contains("biblehub.com") || 
            domain.Contains("blueletterbible.org") ||
            domain.Contains("biblegateway.com"))
            return SourceTier.Tier1;
        
        // Tier 2: Academic and archaeological sources
        if (domain.Contains("biblicalarchaeology.org") ||
            domain.Contains("worldhistory.org") ||
            domain.Contains("edu") ||
            domain.Contains("britannica.com"))
            return SourceTier.Tier2;
        
        // Tier 3: General sources (lowest trust)
        return SourceTier.Tier3;
    }
}

public class ScrapedContent
{
    public required string Url { get; set; }
    public required string Domain { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public DateTime ScrapedAt { get; set; }
    public int WordCount { get; set; }
}

public enum SourceTier
{
    Tier1 = 1,  // Bible Hub, Blue Letter Bible - highest trust
    Tier2 = 2,  // Academic, archaeological sites
    Tier3 = 3   // Wikipedia, general sources - lowest trust
}
