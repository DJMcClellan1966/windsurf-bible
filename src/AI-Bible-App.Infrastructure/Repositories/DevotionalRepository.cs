using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using System.Text.Json;

namespace AI_Bible_App.Infrastructure.Repositories;

public class DevotionalRepository : IDevotionalRepository
{
    private readonly IAIService _aiService;
    private readonly string _devotionalFilePath;

    public DevotionalRepository(IAIService aiService)
    {
        _aiService = aiService;
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "VoicesOfScripture");
        Directory.CreateDirectory(appFolder);
        _devotionalFilePath = Path.Combine(appFolder, "devotionals.json");
    }

    public async Task<Devotional?> GetDevotionalForDateAsync(DateTime date)
    {
        var devotionals = await LoadDevotionalsAsync();
        var dateOnly = date.Date;
        return devotionals.FirstOrDefault(d => d.Date.Date == dateOnly);
    }

    public async Task<IEnumerable<Devotional>> GetRecentDevotionalsAsync(int count = 7)
    {
        var devotionals = await LoadDevotionalsAsync();
        return devotionals
            .OrderByDescending(d => d.Date)
            .Take(count)
            .ToList();
    }

    public async Task MarkDevotionalAsReadAsync(string devotionalId)
    {
        var devotionals = await LoadDevotionalsAsync();
        var devotional = devotionals.FirstOrDefault(d => d.Id == devotionalId);
        if (devotional != null)
        {
            devotional.IsRead = true;
            await SaveDevotionalsAsync(devotionals);
        }
    }

    public async Task<Devotional> GenerateDevotionalAsync(DateTime date)
    {
        // Check if already exists
        var existing = await GetDevotionalForDateAsync(date);
        if (existing != null)
            return existing;

        // Generate new devotional using dedicated AI method
        var response = await _aiService.GenerateDevotionalAsync(date);
        
        // Parse the JSON response
        var devotional = ParseDevotionalResponse(response, date);
        
        // Save it
        var devotionals = await LoadDevotionalsAsync();
        devotionals.Add(devotional);
        await SaveDevotionalsAsync(devotionals);

        return devotional;
    }

    private Devotional ParseDevotionalResponse(string response, DateTime date)
    {
        try
        {
            // Extract JSON if wrapped in markdown code blocks
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}') + 1;
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart);
                var devotionalData = JsonSerializer.Deserialize<DevotionalData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (devotionalData != null)
                {
                    return new Devotional
                    {
                        Title = devotionalData.Title ?? "Daily Devotional",
                        Scripture = devotionalData.Scripture ?? "",
                        ScriptureReference = devotionalData.ScriptureReference ?? "",
                        Content = devotionalData.Content ?? "",
                        Prayer = devotionalData.Prayer ?? "",
                        Category = devotionalData.Category ?? "Faith",
                        Date = date,
                        IsRead = false
                    };
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DevotionalRepository] Failed to parse response: {ex.Message}");
        }

        // Fallback devotional
        return new Devotional
        {
            Title = "Daily Reflection",
            Scripture = "Trust in the LORD with all your heart and lean not on your own understanding; in all your ways submit to him, and he will make your paths straight.",
            ScriptureReference = "Proverbs 3:5-6",
            Content = "Today, let us remember to trust in God's wisdom and guidance in all aspects of our lives.",
            Prayer = "Lord, help me to trust in You completely and to seek Your will in all I do. Amen.",
            Category = "Faith",
            Date = date,
            IsRead = false
        };
    }

    private class DevotionalData
    {
        public string? Title { get; set; }
        public string? Scripture { get; set; }
        public string? ScriptureReference { get; set; }
        public string? Content { get; set; }
        public string? Prayer { get; set; }
        public string? Category { get; set; }
    }

    private async Task<List<Devotional>> LoadDevotionalsAsync()
    {
        if (!File.Exists(_devotionalFilePath))
            return new List<Devotional>();

        try
        {
            var json = await File.ReadAllTextAsync(_devotionalFilePath);
            return JsonSerializer.Deserialize<List<Devotional>>(json) ?? new List<Devotional>();
        }
        catch
        {
            return new List<Devotional>();
        }
    }

    private async Task SaveDevotionalsAsync(List<Devotional> devotionals)
    {
        var json = JsonSerializer.Serialize(devotionals, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_devotionalFilePath, json);
    }
}
