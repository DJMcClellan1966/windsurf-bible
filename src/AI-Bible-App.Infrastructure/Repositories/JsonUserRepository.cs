using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Logging;

namespace AI_Bible_App.Infrastructure.Repositories;

/// <summary>
/// JSON file-based repository for user profiles.
/// Stores user data in a central users.json file.
/// </summary>
public class JsonUserRepository : JsonRepositoryBase<AppUser>, IUserRepository
{
    public JsonUserRepository(
        ILogger<JsonUserRepository> logger,
        IEncryptionService? encryptionService = null,
        IFileSecurityService? fileSecurityService = null,
        string dataDirectory = "data")
        : base(logger, "users.json", encryptionService, fileSecurityService, dataDirectory)
    {
    }

    protected override string GetEntityId(AppUser entity) => entity.Id;
    protected override string EntityTypeName => "user";

    public Task<List<AppUser>> GetAllUsersAsync()
        => LoadAllAsync();

    public async Task<AppUser?> GetUserAsync(string userId)
    {
        var users = await LoadAllAsync();
        return users.FirstOrDefault(u => u.Id == userId);
    }

    public Task SaveUserAsync(AppUser user)
    {
        user.LastActiveAt = DateTime.UtcNow;
        return UpsertAsync(user, user.Id);
    }

    public Task DeleteUserAsync(string userId)
        => DeleteByIdAsync(userId);

    public async Task<AppUser?> GetLastActiveUserAsync()
    {
        var users = await LoadAllAsync();
        return users.OrderByDescending(u => u.LastActiveAt).FirstOrDefault();
    }
}
