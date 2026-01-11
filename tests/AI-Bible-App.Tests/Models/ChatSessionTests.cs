using AI_Bible_App.Core.Models;

namespace AI_Bible_App.Tests.Models;

public class ChatSessionTests
{
    [Fact]
    public void ChatSession_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var session = new ChatSession();

        // Assert
        Assert.NotNull(session.Id);
        Assert.NotNull(session.Messages);
        Assert.Empty(session.Messages);
        Assert.NotEqual(default(DateTime), session.StartedAt);
        Assert.Null(session.EndedAt);
    }

    [Fact]
    public void ChatSession_ShouldAddMessages()
    {
        // Arrange
        var session = new ChatSession { CharacterId = "david" };
        var message1 = new ChatMessage { Role = "user", Content = "Hello" };
        var message2 = new ChatMessage { Role = "assistant", Content = "Greetings" };

        // Act
        session.Messages.Add(message1);
        session.Messages.Add(message2);

        // Assert
        Assert.Equal(2, session.Messages.Count);
        Assert.Equal("david", session.CharacterId);
    }
}
