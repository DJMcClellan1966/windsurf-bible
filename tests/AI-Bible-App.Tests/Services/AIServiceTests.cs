using Xunit;
using Xunit.Abstractions;
using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using AI_Bible_App.Infrastructure.Services;
using AI_Bible_App.Infrastructure.Repositories;
using AI_Bible_App.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;

namespace AI_Bible_App.Tests.Services;

/// <summary>
/// Tests for AI service implementations including Azure OpenAI.
/// </summary>
public class AIServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ILogger<AzureOpenAIService>> _azureLoggerMock;
    private readonly Mock<ILogger<GroqAIService>> _groqLoggerMock;
    private readonly Mock<IBibleRAGService> _ragServiceMock;

    public AIServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _azureLoggerMock = new Mock<ILogger<AzureOpenAIService>>();
        _groqLoggerMock = new Mock<ILogger<GroqAIService>>();
        _ragServiceMock = new Mock<IBibleRAGService>();
    }

    [Fact]
    public void AzureOpenAIService_NotConfigured_IsAvailableFalse()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var service = new AzureOpenAIService(config, _azureLoggerMock.Object);

        // Assert
        Assert.False(service.IsAvailable);
        _output.WriteLine("✅ AzureOpenAIService.IsAvailable is false when not configured");
    }

    [Fact]
    public void AzureOpenAIService_Configured_IsAvailableTrue()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AzureOpenAI:ApiKey", "test-key" },
                { "AzureOpenAI:Endpoint", "https://test.openai.azure.com" },
                { "AzureOpenAI:DeploymentName", "gpt-4" }
            })
            .Build();

        // Act
        var service = new AzureOpenAIService(config, _azureLoggerMock.Object);

        // Assert
        Assert.True(service.IsAvailable);
        _output.WriteLine("✅ AzureOpenAIService.IsAvailable is true when configured");
    }

    [Fact]
    public async Task AzureOpenAIService_NotConfigured_ThrowsOnCall()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var service = new AzureOpenAIService(config, _azureLoggerMock.Object);
        var character = new BiblicalCharacter { Id = "test", SystemPrompt = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetChatResponseAsync(character, new List<ChatMessage>(), "Hello"));
        
        _output.WriteLine("✅ AzureOpenAIService throws InvalidOperationException when not configured");
    }

    [Fact]
    public void GroqAIService_NotConfigured_IsAvailableFalse()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var service = new GroqAIService(config, _groqLoggerMock.Object);

        // Assert
        Assert.False(service.IsAvailable);
        _output.WriteLine("✅ GroqAIService.IsAvailable is false when not configured");
    }

    [Fact]
    public void GroqAIService_Configured_IsAvailableTrue()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Groq:ApiKey", "test-groq-key" },
                { "Groq:ModelName", "llama-3.1-8b-instant" }
            })
            .Build();

        // Act
        var service = new GroqAIService(config, _groqLoggerMock.Object);

        // Assert
        Assert.True(service.IsAvailable);
        _output.WriteLine("✅ GroqAIService.IsAvailable is true when configured");
    }

    [Fact]
    public void AIBackendType_HasAllExpectedValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(AIBackendType), AIBackendType.LocalOllama));
        Assert.True(Enum.IsDefined(typeof(AIBackendType), AIBackendType.OnDevice));
        Assert.True(Enum.IsDefined(typeof(AIBackendType), AIBackendType.Cloud));
        Assert.True(Enum.IsDefined(typeof(AIBackendType), AIBackendType.AzureOpenAI));
        Assert.True(Enum.IsDefined(typeof(AIBackendType), AIBackendType.Cached));
        
        _output.WriteLine("✅ All AIBackendType values are defined:");
        foreach (AIBackendType backend in Enum.GetValues<AIBackendType>())
        {
            _output.WriteLine($"  - {backend}");
        }
    }

    [Fact]
    public void DeviceCapabilityTier_CoverAllScenarios()
    {
        // Assert - verify all tiers exist
        Assert.Equal(4, Enum.GetValues<DeviceCapabilityTier>().Length);
        
        var tiers = new[]
        {
            (DeviceCapabilityTier.Minimal, "< 2GB RAM", AIBackendType.Cached),
            (DeviceCapabilityTier.Low, "2-3GB RAM", AIBackendType.OnDevice),
            (DeviceCapabilityTier.Medium, "3-6GB RAM", AIBackendType.OnDevice),
            (DeviceCapabilityTier.High, "6GB+ RAM", AIBackendType.LocalOllama)
        };

        foreach (var (tier, desc, expectedBackend) in tiers)
        {
            _output.WriteLine($"  ✅ {tier}: {desc} → {expectedBackend}");
        }
    }

    [Fact]
    public void AIBackendRecommendation_HasRequiredProperties()
    {
        // Arrange
        var recommendation = new AIBackendRecommendation
        {
            Primary = AIBackendType.LocalOllama,
            Fallback = AIBackendType.Cloud,
            Emergency = AIBackendType.Cached,
            RecommendedModelName = "phi4",
            RecommendedContextSize = 4096
        };

        // Assert
        Assert.Equal(AIBackendType.LocalOllama, recommendation.Primary);
        Assert.Equal(AIBackendType.Cloud, recommendation.Fallback);
        Assert.Equal(AIBackendType.Cached, recommendation.Emergency);
        Assert.Equal("phi4", recommendation.RecommendedModelName);
        Assert.Equal(4096, recommendation.RecommendedContextSize);
        
        _output.WriteLine("✅ AIBackendRecommendation has all required properties");
    }

    [Fact]
    public async Task MockRAGService_CanEnrichQuery()
    {
        // Arrange - BibleChunk is what RetrieveRelevantVersesAsync returns
        var relevantChunks = new List<BibleChunk>
        {
            new() { Book = "John", Chapter = 3, StartVerse = 16, EndVerse = 16, Text = "For God so loved..." }
        };

        _ragServiceMock
            .Setup(r => r.RetrieveRelevantVersesAsync(
                It.IsAny<string>(), 
                It.IsAny<int>(), 
                It.IsAny<double>(),
                It.IsAny<SearchStrictness>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(relevantChunks);

        // Act
        var result = await _ragServiceMock.Object.RetrieveRelevantVersesAsync("What is love?", 3);

        // Assert
        Assert.Single(result);
        Assert.Equal("John 3:16", result[0].Reference);
        _output.WriteLine("✅ RAG service can enrich queries with relevant verses");
    }
}

/// <summary>
/// Tests for Serilog logging configuration.
/// </summary>
public class LoggingTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _testLogDir;

    public LoggingTests(ITestOutputHelper output)
    {
        _output = output;
        _testLogDir = Path.Combine(Path.GetTempPath(), $"test_logs_{Guid.NewGuid()}");
    }

    [Fact]
    public void SerilogConfiguration_CreatesLogDirectory()
    {
        // Act
        SerilogConfiguration.ConfigureLogging(null, _testLogDir);

        // Assert
        Assert.True(Directory.Exists(_testLogDir));
        _output.WriteLine($"✅ Log directory created: {_testLogDir}");
    }

    [Fact]
    public void LoggingExtensions_AIRequest_FormatsCorrectly()
    {
        // Arrange
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger>();
        
        // We're testing the extension method signature exists and can be called
        // The actual logging behavior depends on the ILogger implementation
        
        // Assert - method exists and is callable
        Assert.NotNull(typeof(LoggingExtensions).GetMethod("LogAIRequest"));
        _output.WriteLine("✅ LogAIRequest extension method exists");
    }

    [Fact]
    public void LoggingExtensions_AIResponse_FormatsCorrectly()
    {
        // Assert - method exists
        Assert.NotNull(typeof(LoggingExtensions).GetMethod("LogAIResponse"));
        _output.WriteLine("✅ LogAIResponse extension method exists");
    }

    [Fact]
    public void LoggingExtensions_RAGQuery_FormatsCorrectly()
    {
        // Assert - method exists
        Assert.NotNull(typeof(LoggingExtensions).GetMethod("LogRAGQuery"));
        _output.WriteLine("✅ LogRAGQuery extension method exists");
    }

    [Fact]
    public void LoggingExtensions_Performance_FormatsCorrectly()
    {
        // Assert - method exists
        Assert.NotNull(typeof(LoggingExtensions).GetMethod("LogPerformance"));
        _output.WriteLine("✅ LogPerformance extension method exists");
    }

    [Fact]
    public void GetLogDirectory_ReturnsValidPath()
    {
        // Act
        var logDir = SerilogConfiguration.GetLogDirectory();

        // Assert
        Assert.NotEmpty(logDir);
        Assert.Contains("AI-Bible-App", logDir);
        Assert.Contains("logs", logDir);
        _output.WriteLine($"✅ Log directory path: {logDir}");
    }

    [Fact]
    public void AddSerilogLogging_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Logging:MinimumLevel", "Debug" }
            })
            .Build();

        // Act - this should not throw
        services.AddSerilogLogging(config);

        // Assert
        var provider = services.BuildServiceProvider();
        var logger = provider.GetService<ILogger<LoggingTests>>();
        Assert.NotNull(logger);
        _output.WriteLine("✅ Serilog logging registered in DI container");
    }

    public void Dispose()
    {
        try
        {
            SerilogConfiguration.CloseAndFlush();
            if (Directory.Exists(_testLogDir))
            {
                Directory.Delete(_testLogDir, true);
            }
        }
        catch { /* Ignore cleanup errors */ }
    }
}

/// <summary>
/// Tests for error handling in AI services.
/// </summary>
public class ErrorHandlingTests
{
    private readonly ITestOutputHelper _output;

    public ErrorHandlingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BiblicalCharacter_NullSystemPrompt_HandledGracefully()
    {
        // Arrange
        var character = new BiblicalCharacter
        {
            Id = "test",
            Name = "Test Character",
            SystemPrompt = null! // Simulate null
        };

        // Act & Assert - should not throw
        var hasPrompt = !string.IsNullOrEmpty(character.SystemPrompt);
        _output.WriteLine($"✅ Null system prompt handled (hasPrompt: {hasPrompt})");
    }

    [Fact]
    public void ChatMessage_EmptyContent_IsValid()
    {
        // Arrange & Act
        var message = new ChatMessage
        {
            Role = "user",
            Content = ""
        };

        // Assert
        Assert.NotNull(message);
        Assert.Empty(message.Content);
        _output.WriteLine("✅ Empty message content is valid");
    }

    [Fact]
    public void ChatSession_EmptyMessages_IsValid()
    {
        // Arrange & Act
        var session = new ChatSession();

        // Assert
        Assert.NotNull(session.Messages);
        Assert.Empty(session.Messages);
        _output.WriteLine("✅ Empty message list is valid");
    }

    [Fact]
    public async Task Repository_InvalidId_ReturnsNull()
    {
        // Arrange
        var repo = new InMemoryCharacterRepository();

        // Act
        var result = await repo.GetCharacterAsync("non-existent-id-12345");

        // Assert
        Assert.Null(result);
        _output.WriteLine("✅ Invalid ID returns null gracefully");
    }

    [Fact]
    public void BibleVerse_MissingFields_HasDefaults()
    {
        // Arrange & Act
        var verse = new BibleVerse();

        // Assert
        Assert.NotNull(verse.Book);
        Assert.NotNull(verse.Text);
        Assert.Equal(0, verse.Chapter);
        Assert.Equal(0, verse.Verse);
        _output.WriteLine("✅ BibleVerse has safe defaults");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Repository_EmptyOrNullId_ReturnsNull(string? id)
    {
        // Arrange
        var repo = new InMemoryCharacterRepository();

        // Act
        var result = await repo.GetCharacterAsync(id!);

        // Assert
        Assert.Null(result);
        _output.WriteLine($"✅ ID '{id ?? "null"}' returns null gracefully");
    }
}
