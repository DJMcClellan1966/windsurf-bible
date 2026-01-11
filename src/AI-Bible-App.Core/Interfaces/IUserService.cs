using AI_Bible_App.Core.Models;

namespace AI_Bible_App.Core.Interfaces;

/// <summary>
/// Service for managing the current user session and user-related operations
/// </summary>
public interface IUserService
{
    /// <summary>
    /// The currently logged in user (null if no user selected)
    /// </summary>
    AppUser? CurrentUser { get; }
    
    /// <summary>
    /// Event fired when the current user changes
    /// </summary>
    event EventHandler<AppUser?>? CurrentUserChanged;
    
    /// <summary>
    /// Get all available users
    /// </summary>
    Task<List<AppUser>> GetAllUsersAsync();
    
    /// <summary>
    /// Create a new user
    /// </summary>
    Task<AppUser> CreateUserAsync(string name, string? avatarEmoji = null);
    
    /// <summary>
    /// Switch to a different user
    /// </summary>
    Task SwitchUserAsync(string userId);
    
    /// <summary>
    /// Update the current user's profile
    /// </summary>
    Task UpdateCurrentUserAsync(Action<AppUser> updateAction);
    
    /// <summary>
    /// Delete a user (cannot delete current user)
    /// </summary>
    Task DeleteUserAsync(string userId);
    
    /// <summary>
    /// Log out the current user
    /// </summary>
    Task LogoutAsync();
    
    /// <summary>
    /// Try to auto-login the last active user
    /// </summary>
    Task<bool> TryAutoLoginAsync();
    
    /// <summary>
    /// Get the data directory for the current user
    /// </summary>
    string GetUserDataDirectory();
    
    /// <summary>
    /// Set or update the PIN for a user
    /// </summary>
    Task SetUserPinAsync(string userId, string? pin);
    
    /// <summary>
    /// Verify if the provided PIN matches the user's PIN
    /// </summary>
    Task<bool> VerifyPinAsync(string userId, string pin);
    
    /// <summary>
    /// Remove PIN protection from a user
    /// </summary>
    Task RemovePinAsync(string userId);
}
