using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Logging;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Service for managing the current user session and user-related operations.
/// Handles user switching, profile updates, and data directory management.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;
    private AppUser? _currentUser;

    public AppUser? CurrentUser => _currentUser;
    public event EventHandler<AppUser?>? CurrentUserChanged;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public Task<List<AppUser>> GetAllUsersAsync()
        => _userRepository.GetAllUsersAsync();

    public async Task<AppUser> CreateUserAsync(string name, string? avatarEmoji = null)
    {
        var user = new AppUser
        {
            Name = name.Trim(),
            AvatarEmoji = avatarEmoji ?? GetRandomEmoji(),
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow
        };

        await _userRepository.SaveUserAsync(user);
        _logger.LogInformation("Created new user: {Name} ({Id})", user.Name, user.Id);
        
        // Create user's data directory
        var userDir = GetUserDataDirectoryInternal(user.Id);
        if (!Directory.Exists(userDir))
        {
            Directory.CreateDirectory(userDir);
        }

        return user;
    }

    public async Task SwitchUserAsync(string userId)
    {
        var user = await _userRepository.GetUserAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User {userId} not found");
        }

        _currentUser = user;
        _currentUser.LastActiveAt = DateTime.UtcNow;
        await _userRepository.SaveUserAsync(_currentUser);
        
        _logger.LogInformation("Switched to user: {Name}", user.Name);
        CurrentUserChanged?.Invoke(this, _currentUser);
    }

    public async Task UpdateCurrentUserAsync(Action<AppUser> updateAction)
    {
        if (_currentUser == null)
        {
            throw new InvalidOperationException("No user is currently logged in");
        }

        updateAction(_currentUser);
        _currentUser.LastActiveAt = DateTime.UtcNow;
        await _userRepository.SaveUserAsync(_currentUser);
        
        CurrentUserChanged?.Invoke(this, _currentUser);
    }

    public async Task DeleteUserAsync(string userId)
    {
        if (_currentUser?.Id == userId)
        {
            throw new InvalidOperationException("Cannot delete the currently active user");
        }

        // Delete user data directory
        var userDir = GetUserDataDirectoryInternal(userId);
        if (Directory.Exists(userDir))
        {
            try
            {
                Directory.Delete(userDir, recursive: true);
                _logger.LogInformation("Deleted user data directory: {Path}", userDir);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete user data directory: {Path}", userDir);
            }
        }

        await _userRepository.DeleteUserAsync(userId);
        _logger.LogInformation("Deleted user: {UserId}", userId);
    }

    public Task LogoutAsync()
    {
        _currentUser = null;
        CurrentUserChanged?.Invoke(this, null);
        _logger.LogInformation("User logged out");
        return Task.CompletedTask;
    }

    public async Task<bool> TryAutoLoginAsync()
    {
        var lastUser = await _userRepository.GetLastActiveUserAsync();
        if (lastUser != null)
        {
            _currentUser = lastUser;
            _currentUser.LastActiveAt = DateTime.UtcNow;
            await _userRepository.SaveUserAsync(_currentUser);
            
            _logger.LogInformation("Auto-logged in user: {Name}", lastUser.Name);
            CurrentUserChanged?.Invoke(this, _currentUser);
            return true;
        }
        return false;
    }

    public string GetUserDataDirectory()
    {
        if (_currentUser == null)
        {
            // Return default directory if no user logged in
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AI-Bible-App", "data");
        }
        return GetUserDataDirectoryInternal(_currentUser.Id);
    }

    private static string GetUserDataDirectoryInternal(string userId)
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AI-Bible-App", "users", userId, "data");
    }

    private static string GetRandomEmoji()
    {
        var emojis = new[] { "👤", "😊", "🙂", "😇", "🌟", "💫", "✨", "🙏", "📖", "✝️", "🕊️", "💒" };
        return emojis[Random.Shared.Next(emojis.Length)];
    }

    public async Task SetUserPinAsync(string userId, string? pin)
    {
        var user = await _userRepository.GetUserAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User {userId} not found");
        }

        if (string.IsNullOrEmpty(pin))
        {
            user.PinHash = null;
        }
        else
        {
            // Simple hash for PIN (adequate for local device security)
            user.PinHash = HashPin(pin);
        }

        await _userRepository.SaveUserAsync(user);
        
        // Update current user if this is the current user
        if (_currentUser?.Id == userId)
        {
            _currentUser.PinHash = user.PinHash;
            CurrentUserChanged?.Invoke(this, _currentUser);
        }

        _logger.LogInformation("PIN {Action} for user: {Name}", 
            string.IsNullOrEmpty(pin) ? "removed" : "set", user.Name);
    }

    public async Task<bool> VerifyPinAsync(string userId, string pin)
    {
        var user = await _userRepository.GetUserAsync(userId);
        if (user == null)
        {
            return false;
        }

        // If user has no PIN, any entry is valid (shouldn't happen in normal flow)
        if (string.IsNullOrEmpty(user.PinHash))
        {
            return true;
        }

        return user.PinHash == HashPin(pin);
    }

    public async Task RemovePinAsync(string userId)
    {
        await SetUserPinAsync(userId, null);
    }

    private static string HashPin(string pin)
    {
        // Simple hash using SHA256 - adequate for local device PIN protection
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(pin + "BibleAppSalt");
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
