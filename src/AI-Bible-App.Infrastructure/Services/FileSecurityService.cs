using AI_Bible_App.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.AccessControl;
using System.Security.Principal;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Service for setting secure file permissions on sensitive data files
/// </summary>
public class FileSecurityService : IFileSecurityService
{
    private readonly ILogger<FileSecurityService> _logger;

    public FileSecurityService(ILogger<FileSecurityService> logger)
    {
        _logger = logger;
    }

    public void SetRestrictivePermissions(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Cannot set permissions on non-existent file: {FilePath}", filePath);
            return;
        }

        try
        {
            // Windows-specific: Use ACLs to restrict access to current user only
            if (OperatingSystem.IsWindows())
            {
                var fileInfo = new FileInfo(filePath);
                var fileSecurity = fileInfo.GetAccessControl();

                // Remove all existing rules
                var rules = fileSecurity.GetAccessRules(true, true, typeof(NTAccount));
                foreach (FileSystemAccessRule rule in rules)
                {
                    fileSecurity.RemoveAccessRule(rule);
                }

                // Add rule for current user only (full control)
                var currentUser = WindowsIdentity.GetCurrent();
                var accessRule = new FileSystemAccessRule(
                    currentUser.User!,
                    FileSystemRights.FullControl,
                    AccessControlType.Allow);

                fileSecurity.AddAccessRule(accessRule);
                fileInfo.SetAccessControl(fileSecurity);

                _logger.LogDebug("Set restrictive permissions on {FilePath}", filePath);
            }
            else
            {
                // Unix-like systems: Use chmod equivalent (600 - user read/write only)
                _logger.LogDebug("Non-Windows platform - using default file permissions");
                // Unix file modes require .NET 6+ and platform-specific implementation
                // Graceful degradation on Windows or older .NET versions
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set restrictive permissions on {FilePath}", filePath);
            // Don't throw - graceful degradation
        }
    }

    public void EnsureSecureDirectory(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                _logger.LogDebug("Created directory: {DirectoryPath}", directoryPath);
            }

            // Set restrictive permissions on directory
            if (OperatingSystem.IsWindows())
            {
                var dirInfo = new DirectoryInfo(directoryPath);
                var dirSecurity = dirInfo.GetAccessControl();

                // Remove inherited rules
                dirSecurity.SetAccessRuleProtection(true, false);

                // Remove all existing rules
                var rules = dirSecurity.GetAccessRules(true, true, typeof(NTAccount));
                foreach (FileSystemAccessRule rule in rules)
                {
                    dirSecurity.RemoveAccessRule(rule);
                }

                // Add rule for current user only
                var currentUser = WindowsIdentity.GetCurrent();
                var accessRule = new FileSystemAccessRule(
                    currentUser.User!,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);

                dirSecurity.AddAccessRule(accessRule);
                dirInfo.SetAccessControl(dirSecurity);

                _logger.LogDebug("Set secure permissions on directory: {DirectoryPath}", directoryPath);
            }
            else
            {
                _logger.LogDebug("Non-Windows platform - using default directory permissions");
                // Unix file modes require .NET 6+ and platform-specific implementation
                // Graceful degradation on Windows or older .NET versions
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set secure directory permissions on {DirectoryPath}", directoryPath);
            // Don't throw - graceful degradation
        }
    }
}
