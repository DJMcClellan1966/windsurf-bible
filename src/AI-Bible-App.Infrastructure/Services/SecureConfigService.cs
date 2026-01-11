using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Service for securely storing and retrieving sensitive configuration like API keys
/// Uses Windows Data Protection API (DPAPI) for encryption on Windows
/// </summary>
public interface ISecureConfigService
{
    Task<string?> GetSecretAsync(string key);
    Task SetSecretAsync(string key, string value);
    Task DeleteSecretAsync(string key);
    Task<bool> HasSecretAsync(string key);
}

public class SecureConfigService : ISecureConfigService
{
    private readonly string _secretsFilePath;
    private readonly byte[] _entropy;

    public SecureConfigService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "VoicesOfScripture");
        Directory.CreateDirectory(appFolder);
        _secretsFilePath = Path.Combine(appFolder, "secrets.dat");
        
        // Use a consistent entropy for this machine/user
        _entropy = Encoding.UTF8.GetBytes("VoicesOfScripture_Secret_Key_V1");
    }

    public async Task<string?> GetSecretAsync(string key)
    {
        var secrets = await LoadSecretsAsync();
        if (secrets.TryGetValue(key, out var encryptedValue))
        {
            return DecryptString(encryptedValue);
        }
        return null;
    }

    public async Task SetSecretAsync(string key, string value)
    {
        var secrets = await LoadSecretsAsync();
        var encryptedValue = EncryptString(value);
        secrets[key] = encryptedValue;
        await SaveSecretsAsync(secrets);
    }

    public async Task DeleteSecretAsync(string key)
    {
        var secrets = await LoadSecretsAsync();
        secrets.Remove(key);
        await SaveSecretsAsync(secrets);
    }

    public async Task<bool> HasSecretAsync(string key)
    {
        var secrets = await LoadSecretsAsync();
        return secrets.ContainsKey(key);
    }

    private string EncryptString(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            
            // Use DPAPI on Windows, fallback to base64 on other platforms
            if (OperatingSystem.IsWindows())
            {
                var encryptedBytes = ProtectedData.Protect(plainBytes, _entropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            else
            {
                // For non-Windows platforms, use a simple obfuscation
                // In production, you'd want to use platform-specific keychains
                return Convert.ToBase64String(plainBytes);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SecureConfig] Encryption failed: {ex.Message}");
            return string.Empty;
        }
    }

    private string DecryptString(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            
            if (OperatingSystem.IsWindows())
            {
                var plainBytes = ProtectedData.Unprotect(encryptedBytes, _entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            else
            {
                return Encoding.UTF8.GetString(encryptedBytes);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SecureConfig] Decryption failed: {ex.Message}");
            return string.Empty;
        }
    }

    private async Task<Dictionary<string, string>> LoadSecretsAsync()
    {
        if (!File.Exists(_secretsFilePath))
            return new Dictionary<string, string>();

        try
        {
            var json = await File.ReadAllTextAsync(_secretsFilePath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) 
                ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private async Task SaveSecretsAsync(Dictionary<string, string> secrets)
    {
        var json = JsonSerializer.Serialize(secrets, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_secretsFilePath, json);
    }
}
