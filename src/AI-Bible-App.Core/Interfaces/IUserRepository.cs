using AI_Bible_App.Core.Models;

namespace AI_Bible_App.Core.Interfaces;

/// <summary>
/// Repository for user profile management
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Get all users
    /// </summary>
    Task<List<AppUser>> GetAllUsersAsync();
    
    /// <summary>
    /// Get a user by ID
    /// </summary>
    Task<AppUser?> GetUserAsync(string userId);
    
    /// <summary>
    /// Get a user by ID (alias for consistency)
    /// </summary>
    Task<AppUser?> GetUserByIdAsync(string userId) => GetUserAsync(userId);
    
    /// <summary>
    /// Update an existing user profile
    /// </summary>
    Task UpdateUserAsync(AppUser user) => SaveUserAsync(user);
    
    /// <summary>
    /// Save or update a user profile
    /// </summary>
    Task SaveUserAsync(AppUser user);
    
    /// <summary>
    /// Delete a user and optionally their data
    /// </summary>
    Task DeleteUserAsync(string userId);
    
    /// <summary>
    /// Get the last active user (for auto-login)
    /// </summary>
    Task<AppUser?> GetLastActiveUserAsync();
}
