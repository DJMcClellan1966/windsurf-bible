using AI_Bible_App.Core.Models;

namespace AI_Bible_App.Core.Interfaces;

/// <summary>
/// Repository for managing biblical characters
/// </summary>
public interface ICharacterRepository
{
    Task<BiblicalCharacter?> GetCharacterAsync(string characterId);
    Task<List<BiblicalCharacter>> GetAllCharactersAsync();
}
