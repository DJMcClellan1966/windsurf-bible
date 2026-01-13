using AI_Bible_App.Infrastructure.Repositories;
var repo = new InMemoryCharacterRepository(null);
var chars = await repo.GetAllCharactersAsync();
Console.WriteLine($"Total characters: {chars.Count}");
Console.WriteLine($"Characters with RoundtableEnabled=true:");
foreach (var c in chars.Where(x => x.RoundtableEnabled))
{
    Console.WriteLine($"  - {c.Name}");
}
