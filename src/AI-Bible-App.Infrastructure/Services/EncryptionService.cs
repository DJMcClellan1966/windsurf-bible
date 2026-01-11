using AI_Bible_App.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// AES-256 encryption service for protecting sensitive user data
/// Uses machine-specific key storage for enhanced security (Windows DPAPI)
/// </summary>
[SupportedOSPlatform("windows")]
public class EncryptionService : IEncryptionService
{
    private readonly ILogger<EncryptionService> _logger;
    private readonly byte[] _key;
    private readonly byte[] _iv;
    private const string EncryptionMarker = "ENC:";

    public EncryptionService(ILogger<EncryptionService> logger)
    {
        _logger = logger;
        
        // Generate or retrieve encryption key (stored per-machine using DPAPI)
        _key = GetOrCreateKey();
        _iv = GetOrCreateIV();
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            var encrypted = msEncrypt.ToArray();
            return EncryptionMarker + Convert.ToBase64String(encrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption failed");
            throw new InvalidOperationException("Failed to encrypt data", ex);
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        if (!IsEncrypted(cipherText))
            return cipherText; // Not encrypted, return as-is (backward compatibility)

        try
        {
            // Remove encryption marker
            var encryptedData = cipherText.Substring(EncryptionMarker.Length);
            var buffer = Convert.FromBase64String(encryptedData);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(buffer);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption failed");
            throw new InvalidOperationException("Failed to decrypt data", ex);
        }
    }

    public bool IsEncrypted(string data)
    {
        return !string.IsNullOrEmpty(data) && data.StartsWith(EncryptionMarker);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private byte[] GetOrCreateKey()
    {
        var keyPath = GetSecureStoragePath("encryption.key");
        
        if (File.Exists(keyPath))
        {
            try
            {
                var storedProtectedKey = File.ReadAllBytes(keyPath);
                return System.Security.Cryptography.ProtectedData.Unprotect(
                    storedProtectedKey, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load existing key, generating new one");
            }
        }

        // Generate new key
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        
        // Protect and store key using Windows DPAPI (machine-specific)
        var protectedKey = System.Security.Cryptography.ProtectedData.Protect(
            aes.Key, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        Directory.CreateDirectory(Path.GetDirectoryName(keyPath)!);
        File.WriteAllBytes(keyPath, protectedKey);
        
        _logger.LogInformation("Generated new encryption key");
        return aes.Key;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private byte[] GetOrCreateIV()
    {
        var ivPath = GetSecureStoragePath("encryption.iv");
        
        if (File.Exists(ivPath))
        {
            try
            {
                var storedProtectedIV = File.ReadAllBytes(ivPath);
                return System.Security.Cryptography.ProtectedData.Unprotect(
                    storedProtectedIV, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load existing IV, generating new one");
            }
        }

        // Generate new IV
        using var aes = Aes.Create();
        aes.GenerateIV();
        
        // Protect and store IV
        var protectedIV = System.Security.Cryptography.ProtectedData.Protect(
            aes.IV, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        Directory.CreateDirectory(Path.GetDirectoryName(ivPath)!);
        File.WriteAllBytes(ivPath, protectedIV);
        
        return aes.IV;
    }

    private string GetSecureStoragePath(string filename)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appData, "AIBibleApp", "Security");
        return Path.Combine(appFolder, filename);
    }
}
