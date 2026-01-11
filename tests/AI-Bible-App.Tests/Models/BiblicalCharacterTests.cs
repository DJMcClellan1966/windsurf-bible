using AI_Bible_App.Core.Models;

namespace AI_Bible_App.Tests.Models;

public class BiblicalCharacterTests
{
    [Fact]
    public void BiblicalCharacter_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var character = new BiblicalCharacter();

        // Assert
        Assert.NotNull(character.Id);
        Assert.NotNull(character.Name);
        Assert.NotNull(character.BiblicalReferences);
        Assert.NotNull(character.Attributes);
        Assert.Empty(character.BiblicalReferences);
        Assert.Empty(character.Attributes);
    }

    [Fact]
    public void BiblicalCharacter_ShouldStorePropertiesCorrectly()
    {
        // Arrange
        var character = new BiblicalCharacter
        {
            Id = "david",
            Name = "David",
            Title = "King of Israel",
            Era = "1040-970 BC"
        };

        // Assert
        Assert.Equal("david", character.Id);
        Assert.Equal("David", character.Name);
        Assert.Equal("King of Israel", character.Title);
        Assert.Equal("1040-970 BC", character.Era);
    }
}
