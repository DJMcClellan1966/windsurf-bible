using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Logging;

namespace AI_Bible_App.Infrastructure.Repositories;

/// <summary>
/// JSON file-based implementation of prayer repository with encryption.
/// Inherits common functionality from JsonRepositoryBase.
/// </summary>
public class JsonPrayerRepository : JsonRepositoryBase<Prayer>, IPrayerRepository
{
    public JsonPrayerRepository(
        ILogger<JsonPrayerRepository> logger,
        IEncryptionService? encryptionService = null,
        IFileSecurityService? fileSecurityService = null,
        string dataDirectory = "data")
        : base(logger, "prayers.json", encryptionService, fileSecurityService, dataDirectory)
    {
    }

    protected override string GetEntityId(Prayer entity) => entity.Id;
    protected override string EntityTypeName => "prayer";

    public Task<Prayer> GetPrayerAsync(string prayerId)
        => GetByIdAsync(prayerId, $"Prayer {prayerId} not found");

    public Task<List<Prayer>> GetAllPrayersAsync()
        => LoadAllAsync();

    public Task SavePrayerAsync(Prayer prayer)
        => UpsertAsync(prayer, prayer.Id);

    public Task DeletePrayerAsync(string prayerId)
        => DeleteByIdAsync(prayerId);

    public async Task<List<Prayer>> GetPrayersByTopicAsync(string topic)
    {
        var prayers = await LoadAllAsync();
        return prayers.Where(p => p.Topic.Contains(topic, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
