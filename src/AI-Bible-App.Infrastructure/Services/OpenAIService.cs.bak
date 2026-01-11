using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Implementation of AI service using Azure OpenAI
/// </summary>
public class OpenAIService : IAIService
{
    private readonly AzureOpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(IConfiguration configuration, ILogger<OpenAIService> logger)
    {
        _logger = logger;
        var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not configured");
        var endpoint = configuration["OpenAI:Endpoint"];
        _deploymentName = configuration["OpenAI:DeploymentName"] ?? "gpt-4";

        if (string.IsNullOrEmpty(endpoint))
        {
            throw new InvalidOperationException("OpenAI Endpoint must be configured for Azure OpenAI");
        }

        // Azure OpenAI
        _client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    public async Task<string> GetChatResponseAsync(
        BiblicalCharacter character, 
        List<Core.Models.ChatMessage> conversationHistory, 
        string userMessage, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var chatClient = _client.GetChatClient(_deploymentName);
            var messages = new List<OpenAI.Chat.ChatMessage>
            {
                new SystemChatMessage(character.SystemPrompt)
            };

            // Add conversation history
            foreach (var msg in conversationHistory.TakeLast(10)) // Limit to last 10 messages
            {
                if (msg.Role == "user")
                    messages.Add(new UserChatMessage(msg.Content));
                else if (msg.Role == "assistant")
                    messages.Add(new AssistantChatMessage(msg.Content));
            }

            // Add current user message
            messages.Add(new UserChatMessage(userMessage));

            var response = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat response from OpenAI");
            throw;
        }
    }

    public async Task<string> GeneratePrayerAsync(string topic, CancellationToken cancellationToken = default)
    {
        try
        {
            var chatClient = _client.GetChatClient(_deploymentName);
            var systemPrompt = @"You are a compassionate prayer writer. Generate heartfelt, biblical prayers that are meaningful and spiritually uplifting. 
Keep prayers concise (2-3 paragraphs), use reverent language, and include relevant scripture themes when appropriate.";

            var userPrompt = string.IsNullOrEmpty(topic) 
                ? "Generate a daily prayer for guidance, strength, and gratitude." 
                : $"Generate a prayer about: {topic}";

            var messages = new List<OpenAI.Chat.ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var response = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating prayer from OpenAI");
            throw;
        }
    }
}
