using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using StampService.Core.Interfaces;
using StampService.Core.Models;

namespace StampService.Core;

/// <summary>
/// Manages cryptographic keys with secure storage using Windows Registry and DPAPI
/// </summary>
public class KeyManager : IDisposable
{
    private readonly ICryptoProvider _cryptoProvider;
    private readonly IAuditLogger _auditLogger;
    private readonly string _registryKeyPath;
    private const string REGISTRY_VALUE_NAME = "MasterKey";
    private byte[]? _privateKey;
    private byte[]? _publicKey;
    private readonly object _keyLock = new();

    public bool HasKey => _privateKey != null && _publicKey != null;
    public string Algorithm => _cryptoProvider.Algorithm;

    public KeyManager(ICryptoProvider cryptoProvider, IAuditLogger auditLogger, string keyStorePath)
    {
        _cryptoProvider = cryptoProvider;
        _auditLogger = auditLogger;
        
        // Convert file path to registry path
        // e.g., "C:\ProgramData\StampService\master.key" -> "SOFTWARE\StampService"
        _registryKeyPath = @"SOFTWARE\StampService";
    }

    /// <summary>
  /// Generate a new key pair and store it securely
    /// </summary>
    public void GenerateKey()
    {
        lock (_keyLock)
 {
            // Generate new key pair
            var (privateKey, publicKey) = _cryptoProvider.GenerateKeyPair();
        
       _privateKey = privateKey;
_publicKey = publicKey;

   // Store encrypted private key using DPAPI in Registry
     SaveKeySecurely();

 _auditLogger.LogSecurityEvent("KeyGeneration", 
             $"New {_cryptoProvider.Algorithm} key pair generated");
        }
    }

    /// <summary>
    /// Load key from secure storage (Windows Registry)
    /// </summary>
    public bool LoadKey()
    {
    lock (_keyLock)
        {
try
       {
       using (var key = Registry.LocalMachine.OpenSubKey(_registryKeyPath, false))
       {
                if (key == null)
          return false;

          var encryptedData = key.GetValue(REGISTRY_VALUE_NAME) as byte[];
          if (encryptedData == null || encryptedData.Length == 0)
       return false;

    var decryptedData = ProtectedData.Unprotect(encryptedData, null, 
            DataProtectionScope.LocalMachine);

         // Format: [privateKeyLength(4)][privateKey][publicKey]
          int privateKeyLength = BitConverter.ToInt32(decryptedData, 0);
       _privateKey = new byte[privateKeyLength];
    Array.Copy(decryptedData, 4, _privateKey, 0, privateKeyLength);

    _publicKey = new byte[decryptedData.Length - 4 - privateKeyLength];
    Array.Copy(decryptedData, 4 + privateKeyLength, _publicKey, 0, _publicKey.Length);

_auditLogger.LogSecurityEvent("KeyLoaded", 
       $"Key loaded from secure storage (Registry)");

        return true;
                }
    }
            catch (Exception ex)
            {
                _auditLogger.LogSecurityEvent("KeyLoadFailed", 
           $"Failed to load key from Registry: {ex.Message}");
   return false;
     }
        }
    }

    /// <summary>
    /// Sign data with the private key
    /// </summary>
    public byte[] Sign(byte[] data)
    {
      lock (_keyLock)
  {
          if (_privateKey == null)
         throw new InvalidOperationException("No private key loaded");

            return _cryptoProvider.Sign(_privateKey, data);
     }
    }

    /// <summary>
    /// Verify signature with the public key
    /// </summary>
    public bool Verify(byte[] data, byte[] signature)
    {
        lock (_keyLock)
        {
            if (_publicKey == null)
            throw new InvalidOperationException("No public key loaded");

   return _cryptoProvider.Verify(_publicKey, data, signature);
        }
    }

    /// <summary>
    /// Get public key in PEM format
    /// </summary>
    public string GetPublicKeyPem()
    {
        lock (_keyLock)
 {
      if (_publicKey == null)
   throw new InvalidOperationException("No public key loaded");

    return _cryptoProvider.ExportPublicKeyPem(_publicKey);
        }
    }

    /// <summary>
    /// Get raw public key bytes
    /// </summary>
    public byte[] GetPublicKey()
    {
  lock (_keyLock)
        {
            if (_publicKey == null)
          throw new InvalidOperationException("No public key loaded");

  return (byte[])_publicKey.Clone();
        }
    }

    /// <summary>
    /// Export private key for SSS (use with extreme caution!)
    /// </summary>
    internal byte[] ExportPrivateKeyForSSS()
    {
        lock (_keyLock)
      {
            if (_privateKey == null)
    throw new InvalidOperationException("No private key loaded");

 _auditLogger.LogSecurityEvent("PrivateKeyExported", 
          "Private key exported for SSS operation (SENSITIVE)");

          return (byte[])_privateKey.Clone();
   }
    }

    /// <summary>
    /// Import private key from SSS recovery
    /// </summary>
    internal void ImportPrivateKeyFromSSS(byte[] privateKey, byte[] publicKey)
    {
        lock (_keyLock)
        {
            _privateKey = (byte[])privateKey.Clone();
 _publicKey = (byte[])publicKey.Clone();

            SaveKeySecurely();

        _auditLogger.LogSecurityEvent("PrivateKeyImported", 
                "Private key imported from SSS recovery (SENSITIVE)");

  // Clear the input arrays
            Array.Clear(privateKey, 0, privateKey.Length);
        }
    }

    /// <summary>
    /// Compute commitment hash for SSS verification
    /// </summary>
    public string ComputeCommitment()
    {
        lock (_keyLock)
        {
            if (_privateKey == null)
         throw new InvalidOperationException("No private key loaded");

            using var sha256 = SHA256.Create();
  var hash = sha256.ComputeHash(_privateKey);
      return Convert.ToBase64String(hash);
        }
    }

    /// <summary>
    /// Delete the key from memory and storage
    /// WARNING: This operation is irreversible! Ensure backup shares exist before calling.
    /// </summary>
/// <param name="confirmDeletion">Must be set to true to confirm deletion</param>
    public void DeleteKey(bool confirmDeletion = false)
    {
        if (!confirmDeletion)
        {
 throw new InvalidOperationException(
  "Key deletion must be explicitly confirmed by setting confirmDeletion parameter to true");
      }

        lock (_keyLock)
        {
            try
            {
     // Clear keys from memory first
         if (_privateKey != null)
      {
     Array.Clear(_privateKey, 0, _privateKey.Length);
         _privateKey = null;
  }

        if (_publicKey != null)
     {
        Array.Clear(_publicKey, 0, _publicKey.Length);
          _publicKey = null;
    }

          // Delete key from Registry
        using (var key = Registry.LocalMachine.OpenSubKey(_registryKeyPath, true))
       {
        if (key != null)
                  {
   // Overwrite with random data first (defense in depth)
         var random = new byte[1024];
        RandomNumberGenerator.Fill(random);
          key.SetValue(REGISTRY_VALUE_NAME, random, RegistryValueKind.Binary);
        key.Flush();
            Array.Clear(random, 0, random.Length);

  // Delete the value
         key.DeleteValue(REGISTRY_VALUE_NAME, false);

             _auditLogger.LogSecurityEvent("KeyDeleted", 
         "Master key permanently deleted from Registry (CRITICAL)");
           }
              else
            {
_auditLogger.LogSecurityEvent("KeyDeleteAttempt", 
             "Key deletion requested but no key found in Registry");
}
  }
       }
            catch (Exception ex)
  {
       _auditLogger.LogSecurityEvent("KeyDeletionFailed", 
             $"Failed to delete key from Registry: {ex.Message}");
        throw new InvalidOperationException("Failed to delete key securely", ex);
          }
        }
    }

    /// <summary>
    /// Check if key exists in Registry
    /// </summary>
    public bool KeyFileExists()
    {
        try
        {
  using (var key = Registry.LocalMachine.OpenSubKey(_registryKeyPath, false))
{
   if (key == null)
        return false;

           var value = key.GetValue(REGISTRY_VALUE_NAME);
        return value != null;
            }
        }
        catch
        {
      return false;
    }
    }

    private void SaveKeySecurely()
    {
        try
        {
    // Combine private and public keys with length prefix
   var combined = new byte[4 + _privateKey!.Length + _publicKey!.Length];
  BitConverter.GetBytes(_privateKey.Length).CopyTo(combined, 0);
            _privateKey.CopyTo(combined, 4);
   _publicKey!.CopyTo(combined, 4 + _privateKey.Length);

            // Encrypt using DPAPI (LocalMachine scope for service)
            var encryptedData = ProtectedData.Protect(combined, null, 
    DataProtectionScope.LocalMachine);

     // Create registry key if it doesn't exist
     using (var key = Registry.LocalMachine.CreateSubKey(_registryKeyPath, true))
  {
  if (key == null)
          throw new InvalidOperationException("Failed to create registry key");

           // Write encrypted data to Registry
key.SetValue(REGISTRY_VALUE_NAME, encryptedData, RegistryValueKind.Binary);
    key.Flush();
            }

     // Clear sensitive data
       Array.Clear(combined, 0, combined.Length);

         _auditLogger.LogSecurityEvent("KeySaved", 
                "Master key saved to Registry with DPAPI encryption");
        }
 catch (Exception ex)
        {
      _auditLogger.LogSecurityEvent("KeySaveFailed", 
    $"Failed to save key to Registry: {ex.Message}");
 throw;
  }
    }

    public void Dispose()
    {
        lock (_keyLock)
        {
    // Securely wipe keys from memory
    if (_privateKey != null)
            {
        Array.Clear(_privateKey, 0, _privateKey.Length);
    _privateKey = null;
        }

 if (_publicKey != null)
            {
     Array.Clear(_publicKey, 0, _publicKey.Length);
  _publicKey = null;
   }
        }
    }
}
