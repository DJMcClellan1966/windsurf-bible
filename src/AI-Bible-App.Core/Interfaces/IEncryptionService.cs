namespace AI_Bible_App.Core.Interfaces;

/// <summary>
/// Service for encrypting and decrypting sensitive data
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypt plaintext data
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypt encrypted data
    /// </summary>
    string Decrypt(string cipherText);

    /// <summary>
    /// Check if data is encrypted (starts with encryption marker)
    /// </summary>
    bool IsEncrypted(string data);
}
