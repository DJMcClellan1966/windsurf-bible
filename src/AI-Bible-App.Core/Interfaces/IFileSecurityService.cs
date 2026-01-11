namespace AI_Bible_App.Core.Interfaces;

/// <summary>
/// Service for setting secure file permissions
/// </summary>
public interface IFileSecurityService
{
    /// <summary>
    /// Set restrictive permissions on a file (current user only)
    /// </summary>
    void SetRestrictivePermissions(string filePath);

    /// <summary>
    /// Ensure directory exists with secure permissions
    /// </summary>
    void EnsureSecureDirectory(string directoryPath);
}
