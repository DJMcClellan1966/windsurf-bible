using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Azure OpenAI cloud AI service for users with better hardware/internet.
/// Provides GPT-4 quality responses with enterprise-grade reliability.
/// Configure in appsettings.json with your Azure OpenAI credentials.
/// </summary>
public class AzureOpenAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _endpoint;
    private readonly string _deploymentName;
    private readonly string _apiVersion;
    private readonly ILogger<AzureOpenAIService> _logger;
    private readonly IBibleRAGService? _ragService;

    public AzureOpenAIService(
        IConfiguration configuration, 
        ILogger<AzureOpenAIService> logger,
        IBibleRAGService? ragService = null)
    {
        _logger = logger;
        _ragService = ragService;
        
        _apiKey = configuration["AzureOpenAI:ApiKey"] ?? "";
        _endpoint = configuration["AzureOpenAI:Endpoint"] ?? "";
        _deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4";
        _apiVersion = configuration["AzureOpenAI:ApiVersion"] ?? "2024-02-01";
        
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(3)
        };
        
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
        }
        
        _logger.LogInformation(
            "AzureOpenAIService initialized with deployment: {Deployment} at {Endpoint}", 
            _deploymentName, 
            string.IsNullOrEmpty(_endpoint) ? "(not configured)" : _endpoint);
    }

    public bool IsAvailable => !string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_endpoint);

    public async Task<string> GetChatResponseAsync(
        BiblicalCharacter character,
        List<ChatMessage> conversationHistory,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Azure OpenAI not configured. Set AzureOpenAI:ApiKey and AzureOpenAI:Endpoint in appsettings.json");

        try
        {
            // Optionally enrich with RAG context
            var enrichedMessage = userMessage;
            if (_ragService != null)
            {
                try
                {
                    var ragContext = await _ragService.RetrieveRelevantVersesAsync(userMessage, 3);
                    if (ragContext.Any())
                    {
                        var verseContext = string.Join("\n", ragContext.Select(c => $"- {c.Reference}: \"{c.Text}\""));
                        enrichedMessage = $"[Relevant Scripture Context:\n{verseContext}]\n\nUser question: {userMessage}";
                        _logger.LogDebug("RAG enriched query with {Count} verses", ragContext.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "RAG enrichment failed, continuing without context");
                }
            }

            var messages = new List<AzureOpenAIMessage>
            {
                new() { Role = "system", Content = character.SystemPrompt }
            };

            // Add conversation history (last 10 messages for context)
            foreach (var msg in conversationHistory.TakeLast(10))
            {
                messages.Add(new AzureOpenAIMessage
                {
                    Role = msg.Role == "assistant" ? "assistant" : "user",
                    Content = msg.Content
                });
            }

            messages.Add(new AzureOpenAIMessage { Role = "user", Content = enrichedMessage });

            var request = new AzureOpenAIRequest
            {
                Messages = messages,
                MaxTokens = 1500,
                Temperature = 0.7,
                TopP = 0.95,
                FrequencyPenalty = 0.3,
                PresencePenalty = 0.3
            };

            var url = $"{_endpoint.TrimEnd('/')}/openai/deployments/{_deploymentName}/chat/completions?api-version={_apiVersion}";
            
            _logger.LogDebug("Sending request to Azure OpenAI: {Url}", url);
            
            var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Azure OpenAI error {StatusCode}: {Content}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Azure OpenAI returned {response.StatusCode}: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<AzureOpenAIResponse>(cancellationToken: cancellationToken);
            var content = result?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response received";
            
            _logger.LogDebug("Received response from Azure OpenAI ({Tokens} tokens)", result?.Usage?.TotalTokens ?? 0);
            return content;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Azure OpenAI request cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure OpenAI API error");
            throw;
        }
    }

    public async IAsyncEnumerable<string> StreamChatResponseAsync(
        BiblicalCharacter character,
        List<ChatMessage> conversationHistory,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Azure OpenAI not configured");

        // Build messages same as non-streaming
        var messages = new List<AzureOpenAIMessage>
        {
            new() { Role = "system", Content = character.SystemPrompt }
        };

        foreach (var msg in conversationHistory.TakeLast(10))
        {
            messages.Add(new AzureOpenAIMessage
            {
                Role = msg.Role == "assistant" ? "assistant" : "user",
                Content = msg.Content
            });
        }

        messages.Add(new AzureOpenAIMessage { Role = "user", Content = userMessage });

        var request = new AzureOpenAIRequest
        {
            Messages = messages,
            MaxTokens = 1500,
            Temperature = 0.7,
            Stream = true
        };

        var url = $"{_endpoint.TrimEnd('/')}/openai/deployments/{_deploymentName}/chat/completions?api-version={_apiVersion}";

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = jsonContent };

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                continue;

            var data = line[6..]; // Remove "data: " prefix
            
            if (data == "[DONE]")
                break;

            // Parse chunk outside try-catch, handle errors gracefully
            string? content = null;
            var parseSuccess = false;
            
            try
            {
                var chunk = JsonSerializer.Deserialize<AzureOpenAIStreamResponse>(data);
                content = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
                parseSuccess = true;
            }
            catch (JsonException)
            {
                // Skip malformed chunks - parseSuccess stays false
            }
            
            if (parseSuccess && !string.IsNullOrEmpty(content))
            {
                yield return content;
            }
        }
    }

    public async Task<string> GeneratePrayerAsync(
        string topic,
        string style,
        BiblicalCharacter? character = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Azure OpenAI not configured");

        var systemPrompt = character?.SystemPrompt ?? 
            "You are a compassionate spiritual guide who helps people connect with God through prayer. " +
            "Generate heartfelt, authentic prayers that speak to the human condition.";

        var userPrompt = $"Please generate a {style} prayer about: {topic}. " +
            "Make it personal, heartfelt, and approximately 150-200 words.";

        var messages = new List<AzureOpenAIMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = userPrompt }
        };

        var request = new AzureOpenAIRequest
        {
            Messages = messages,
            MaxTokens = 500,
            Temperature = 0.8
        };

        var url = $"{_endpoint.TrimEnd('/')}/openai/deployments/{_deploymentName}/chat/completions?api-version={_apiVersion}";
        
        var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AzureOpenAIResponse>(cancellationToken: cancellationToken);
        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "Unable to generate prayer";
    }

    public async Task<string> GeneratePrayerAsync(string topic, CancellationToken cancellationToken = default)
    {
        return await GeneratePrayerAsync(topic, "Traditional", null, cancellationToken);
    }

    public async Task<string> GeneratePersonalizedPrayerAsync(PrayerOptions options, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Azure OpenAI not configured");

        var traditionDescription = options.Tradition switch
        {
            PrayerTradition.General => "traditional and reverent",
            PrayerTradition.Traditional => "formal, reverent, King James style",
            PrayerTradition.Contemporary => "modern and conversational",
            PrayerTradition.Contemplative => "contemplative and meditative",
            PrayerTradition.Liturgical => "structured liturgical style",
            _ => "traditional"
        };

        var moodDescription = options.Mood switch
        {
            PrayerMood.Grateful => "expressing deep gratitude",
            PrayerMood.Anxious => "bringing worries and finding peace",
            PrayerMood.Joyful => "full of joy and celebration",
            PrayerMood.Grieving => "pouring out grief and sorrow",
            PrayerMood.Hopeful => "looking forward with hope",
            PrayerMood.Overwhelmed => "seeking rest from burdens",
            PrayerMood.Peaceful => "resting in God's peace",
            PrayerMood.Confused => "seeking clarity and wisdom",
            PrayerMood.Fearful => "finding courage in faith",
            PrayerMood.Lonely => "seeking God's presence and comfort",
            PrayerMood.Angry => "honestly expressing frustration to God",
            PrayerMood.Content => "in peaceful contentment",
            PrayerMood.Seeking => "earnestly seeking guidance",
            _ => "peaceful"
        };

        var userPrompt = $"Generate a {traditionDescription} prayer that is {moodDescription}. " +
            $"Topic: {options.Topic}. " +
            (options.LifeCircumstances != null ? $"Life circumstances: {options.LifeCircumstances}. " : "") +
            (options.PrayingFor != null ? $"Praying for: {options.PrayingFor}. " : "") +
            (options.Intentions.Any() ? $"Intentions: {string.Join(", ", options.Intentions)}. " : "") +
            (options.IncludeScripture ? "Please include relevant Scripture references. " : "") +
            "Make it personal and approximately 150-250 words.";

        var messages = new List<AzureOpenAIMessage>
        {
            new() { Role = "system", Content = "You are a compassionate spiritual guide who crafts heartfelt prayers." },
            new() { Role = "user", Content = userPrompt }
        };

        var request = new AzureOpenAIRequest
        {
            Messages = messages,
            MaxTokens = 600,
            Temperature = 0.8
        };

        var url = $"{_endpoint.TrimEnd('/')}/openai/deployments/{_deploymentName}/chat/completions?api-version={_apiVersion}";
        var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AzureOpenAIResponse>(cancellationToken: cancellationToken);
        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "Unable to generate prayer";
    }

    public async Task<string> GenerateDevotionalAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Azure OpenAI not configured");

        var userPrompt = $"Generate a daily devotional for {date:MMMM d, yyyy}. Include:\n" +
            "1. A relevant Bible verse\n" +
            "2. A brief reflection (2-3 paragraphs)\n" +
            "3. A prayer for the day\n" +
            "4. A practical application point\n" +
            "Make it encouraging and spiritually nourishing.";

        var messages = new List<AzureOpenAIMessage>
        {
            new() { Role = "system", Content = "You are a wise biblical teacher who creates inspiring daily devotionals." },
            new() { Role = "user", Content = userPrompt }
        };

        var request = new AzureOpenAIRequest
        {
            Messages = messages,
            MaxTokens = 800,
            Temperature = 0.7
        };

        var url = $"{_endpoint.TrimEnd('/')}/openai/deployments/{_deploymentName}/chat/completions?api-version={_apiVersion}";
        var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AzureOpenAIResponse>(cancellationToken: cancellationToken);
        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "Unable to generate devotional";
    }

    #region Request/Response Models

    private class AzureOpenAIRequest
    {
        [JsonPropertyName("messages")]
        public List<AzureOpenAIMessage> Messages { get; set; } = new();

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 1024;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("top_p")]
        public double TopP { get; set; } = 0.95;

        [JsonPropertyName("frequency_penalty")]
        public double FrequencyPenalty { get; set; } = 0;

        [JsonPropertyName("presence_penalty")]
        public double PresencePenalty { get; set; } = 0;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;
    }

    private class AzureOpenAIMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }

    private class AzureOpenAIResponse
    {
        [JsonPropertyName("choices")]
        public List<AzureOpenAIChoice>? Choices { get; set; }

        [JsonPropertyName("usage")]
        public AzureOpenAIUsage? Usage { get; set; }
    }

    private class AzureOpenAIChoice
    {
        [JsonPropertyName("message")]
        public AzureOpenAIMessage? Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    private class AzureOpenAIUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    private class AzureOpenAIStreamResponse
    {
        [JsonPropertyName("choices")]
        public List<AzureOpenAIStreamChoice>? Choices { get; set; }
    }

    private class AzureOpenAIStreamChoice
    {
        [JsonPropertyName("delta")]
        public AzureOpenAIDelta? Delta { get; set; }
    }

    private class AzureOpenAIDelta
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    #endregion
}
