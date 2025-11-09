using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using StampService.Core.Interfaces;

namespace StampService.Core;

/// <summary>
/// Manages secure storage of arbitrary secrets (key-value pairs) using Windows Registry and DPAPI
/// </summary>
public class SecretManager : IDisposable
{
    private readonly IAuditLogger _auditLogger;
    private readonly string _registryKeyPath;
    private const string SECRETS_SUBKEY = "Secrets";
    private readonly object _secretLock = new();

    public SecretManager(IAuditLogger auditLogger)
    {
_auditLogger = auditLogger;
        _registryKeyPath = @"SOFTWARE\StampService\" + SECRETS_SUBKEY;
    }

    /// <summary>
    /// Store a secret (encrypted with DPAPI)
  /// </summary>
    public void StoreSecret(string name, string value, Dictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Secret name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(value))
   throw new ArgumentException("Secret value cannot be empty", nameof(value));

  lock (_secretLock)
        {
        try
 {
 // Create secret data structure
    var secretData = new
      {
  Value = value,
              CreatedAt = DateTime.UtcNow,
         Metadata = metadata ?? new Dictionary<string, string>()
        };

        var jsonData = System.Text.Json.JsonSerializer.Serialize(secretData);
      var dataBytes = Encoding.UTF8.GetBytes(jsonData);

   // Encrypt with DPAPI (LocalMachine scope)
           var encryptedData = ProtectedData.Protect(dataBytes, null, DataProtectionScope.LocalMachine);

       // Store in Registry
   using (var key = Registry.LocalMachine.CreateSubKey(_registryKeyPath, true))
           {
        if (key == null)
          throw new InvalidOperationException("Failed to create registry key");

        key.SetValue(name, encryptedData, RegistryValueKind.Binary);
 key.Flush();
             }

                _auditLogger.LogSecurityEvent("SecretStored", $"Secret '{name}' stored securely");
    }
catch (Exception ex)
    {
             _auditLogger.LogSecurityEvent("SecretStoreFailed", $"Failed to store secret '{name}': {ex.Message}");
        throw;
         }
        }
    }

    /// <summary>
    /// Retrieve a secret (decrypted)
    /// </summary>
  public (string value, DateTime createdAt, Dictionary<string, string> metadata)? RetrieveSecret(string name)
  {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Secret name cannot be empty", nameof(name));

lock (_secretLock)
     {
 try
            {
   using (var key = Registry.LocalMachine.OpenSubKey(_registryKeyPath, false))
                {
  if (key == null)
                return null;

              var encryptedData = key.GetValue(name) as byte[];
     if (encryptedData == null || encryptedData.Length == 0)
        return null;

        // Decrypt with DPAPI
     var decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.LocalMachine);
         var jsonData = Encoding.UTF8.GetString(decryptedData);

     // Deserialize
           var secretData = System.Text.Json.JsonSerializer.Deserialize<SecretData>(jsonData);
   if (secretData == null)
           return null;

     _auditLogger.LogSecurityEvent("SecretRetrieved", $"Secret '{name}' retrieved");

             return (secretData.Value, secretData.CreatedAt, secretData.Metadata);
    }
     }
            catch (Exception ex)
  {
                _auditLogger.LogSecurityEvent("SecretRetrieveFailed", $"Failed to retrieve secret '{name}': {ex.Message}");
 throw;
     }
        }
    }

    /// <summary>
    /// List all stored secret names
    /// </summary>
    public List<string> ListSecrets()
    {
 lock (_secretLock)
        {
    try
    {
       using (var key = Registry.LocalMachine.OpenSubKey(_registryKeyPath, false))
   {
       if (key == null)
            return new List<string>();

          var names = key.GetValueNames().ToList();
               _auditLogger.LogSecurityEvent("SecretsListed", $"Listed {names.Count} secrets");
       return names;
            }
            }
     catch (Exception ex)
    {
          _auditLogger.LogSecurityEvent("SecretListFailed", $"Failed to list secrets: {ex.Message}");
      throw;
       }
        }
}

    /// <summary>
    /// Delete a secret
    /// </summary>
    public bool DeleteSecret(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Secret name cannot be empty", nameof(name));

  lock (_secretLock)
        {
    try
  {
         using (var key = Registry.LocalMachine.OpenSubKey(_registryKeyPath, true))
{
        if (key == null)
                  return false;

     // Check if exists
            if (key.GetValue(name) == null)
       return false;

  // Overwrite with random data first (defense in depth)
         var random = new byte[1024];
     RandomNumberGenerator.Fill(random);
   key.SetValue(name, random, RegistryValueKind.Binary);
          key.Flush();
        Array.Clear(random, 0, random.Length);

          // Delete the value
           key.DeleteValue(name, false);

      _auditLogger.LogSecurityEvent("SecretDeleted", $"Secret '{name}' deleted");
   return true;
   }
            }
     catch (Exception ex)
            {
            _auditLogger.LogSecurityEvent("SecretDeleteFailed", $"Failed to delete secret '{name}': {ex.Message}");
    throw;
      }
      }
    }

    /// <summary>
    /// Check if a secret exists
    /// </summary>
    public bool SecretExists(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
  return false;

        lock (_secretLock)
        {
 try
            {
  using (var key = Registry.LocalMachine.OpenSubKey(_registryKeyPath, false))
   {
         if (key == null)
   return false;

       return key.GetValue(name) != null;
                }
          }
   catch
    {
     return false;
 }
        }
  }

    /// <summary>
    /// Delete all secrets
    /// </summary>
    public void DeleteAllSecrets(bool confirmDeletion = false)
    {
        if (!confirmDeletion)
        throw new InvalidOperationException("All secrets deletion must be explicitly confirmed");

        lock (_secretLock)
        {
          try
     {
       using (var key = Registry.LocalMachine.OpenSubKey(_registryKeyPath, true))
    {
         if (key == null)
  return;

         var names = key.GetValueNames();
       foreach (var name in names)
         {
 // Overwrite with random data
      var random = new byte[1024];
     RandomNumberGenerator.Fill(random);
       key.SetValue(name, random, RegistryValueKind.Binary);
            Array.Clear(random, 0, random.Length);

         // Delete
   key.DeleteValue(name, false);
 }

 key.Flush();
       }

                _auditLogger.LogSecurityEvent("AllSecretsDeleted", $"All secrets deleted (CRITICAL)");
   }
            catch (Exception ex)
         {
          _auditLogger.LogSecurityEvent("AllSecretsDeleteFailed", $"Failed to delete all secrets: {ex.Message}");
       throw;
  }
     }
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    private class SecretData
    {
     public string Value { get; set; } = string.Empty;
      public DateTime CreatedAt { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
