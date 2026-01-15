using AI_Bible_App.Infrastructure.Repositories;

namespace AI_Bible_App.Tests.Repositories;

public class InMemoryCharacterRepositoryTests
{
    [Fact]
    public async Task GetAllCharactersAsync_ShouldReturnAllCharacters()
    {
        // Arrange
        var repository = new InMemoryCharacterRepository();

        // Act
        var characters = await repository.GetAllCharactersAsync();

        // Assert
        Assert.NotNull(characters);
        Assert.True(characters.Count >= 18, $"Expected at least 18 built-in characters, got {characters.Count}");
        Assert.Contains(characters, c => c.Id == "david");
        Assert.Contains(characters, c => c.Id == "paul");
        Assert.Contains(characters, c => c.Id == "moses");
        Assert.Contains(characters, c => c.Id == "mary");
        Assert.Contains(characters, c => c.Id == "peter");
        Assert.Contains(characters, c => c.Id == "skeptic");
        Assert.Contains(characters, c => c.Id == "esther");
        Assert.Contains(characters, c => c.Id == "john");
        Assert.Contains(characters, c => c.Id == "solomon");
        Assert.Contains(characters, c => c.Id == "ruth");
        Assert.Contains(characters, c => c.Id == "deborah");
        Assert.Contains(characters, c => c.Id == "hannah");
    }

    [Fact]
    public async Task GetCharacterAsync_ShouldReturnDavid()
    {
        // Arrange
        var repository = new InMemoryCharacterRepository();

        // Act
        var character = await repository.GetCharacterAsync("david");

        // Assert
        Assert.NotNull(character);
        Assert.Equal("david", character.Id);
        Assert.Equal("David", character.Name);
        Assert.Contains("King", character.Title);
    }

    [Fact]
    public async Task GetCharacterAsync_ShouldReturnPaul()
    {
        // Arrange
        var repository = new InMemoryCharacterRepository();

        // Act
        var character = await repository.GetCharacterAsync("paul");

        // Assert
        Assert.NotNull(character);
        Assert.Equal("paul", character.Id);
        Assert.Equal("Paul (Saul of Tarsus)", character.Name);
    }

    [Fact]
    public async Task GetCharacterAsync_ShouldReturnNullForUnknownId()
    {
        // Arrange
        var repository = new InMemoryCharacterRepository();

        // Act
        var character = await repository.GetCharacterAsync("unknown");

        // Assert
        Assert.Null(character);
    }

    [Fact]
    public async Task GetCharacterAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var repository = new InMemoryCharacterRepository();

        // Act
        var character = await repository.GetCharacterAsync("DAVID");

        // Assert
        Assert.NotNull(character);
        Assert.Equal("david", character.Id);
    }
}
