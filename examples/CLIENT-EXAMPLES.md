# Example Client Application

This directory contains example applications demonstrating how to integrate with the Secure Stamp Service.

## Table of Contents

1. [Signing Operations](#signing-operations)
2. [Secret Storage (NEW!)](#secret-storage-new)
3. [Health Monitoring](#health-monitoring)
4. [Error Handling](#error-handling)
5. [Integration Checklist](#integration-checklist)

---

## Signing Operations

### Example 1: Simple Token Minting

```csharp
using StampService.ClientLib;
using StampService.Core.Models;

// Initialize client
using var client = new StampServiceClient();

// Create a mint request
var mintRequest = new SignRequest
{
    Operation = "mint",
    RequesterId = "TokenSystem",
    Payload = new Dictionary<string, object>
    {
     ["recipient"] = "user123",
        ["amount"] = 1000,
        ["currency"] = "GOLD",
        ["timestamp"] = DateTime.UtcNow.ToString("O"),
        ["nonce"] = Guid.NewGuid().ToString()
    }
};

// Get signature
var response = await client.SignAsync(mintRequest);

Console.WriteLine($"Mint operation signed!");
Console.WriteLine($"Signature: {response.Signature}");
Console.WriteLine($"Timestamp: {response.Timestamp}");

// Verify signature locally
bool isValid = client.VerifySignature(response);
Console.WriteLine($"Signature valid: {isValid}");
```

### Example 2: Batch Operations

```csharp
using StampService.ClientLib;
using StampService.Core.Models;

using var client = new StampServiceClient();

// Process multiple operations
var operations = new[]
{
    ("mint", "user1", 100),
  ("mint", "user2", 200),
    ("mint", "user3", 150)
};

foreach (var (operation, user, amount) in operations)
{
    var request = new SignRequest
    {
        Operation = operation,
        RequesterId = "BatchProcessor",
        Payload = new Dictionary<string, object>
        {
            ["recipient"] = user,
      ["amount"] = amount,
        ["batch_id"] = "BATCH-001",
            ["timestamp"] = DateTime.UtcNow.ToString("O")
      }
    };

    var response = await client.SignAsync(request);
    Console.WriteLine($"? Signed: {operation} {amount} for {user}");
    
    // Store signature in your database along with the operation
    // The signature proves the operation was approved by the stamp service
}
```

### Example 3: Freeze/Burn Operations

```csharp
using StampService.ClientLib;
using StampService.Core.Models;

// Freeze an account
async Task<SignedResponse> FreezeAccount(string accountId, string reason)
{
    using var client = new StampServiceClient();
    
  var request = new SignRequest
    {
        Operation = "freeze",
 RequesterId = "ComplianceSystem",
        Payload = new Dictionary<string, object>
     {
         ["account_id"] = accountId,
            ["reason"] = reason,
    ["frozen_at"] = DateTime.UtcNow.ToString("O"),
      ["authorized_by"] = Environment.UserName
        }
    };
    
    return await client.SignAsync(request);
}

// Burn tokens
async Task<SignedResponse> BurnTokens(string accountId, decimal amount, string reason)
{
    using var client = new StampServiceClient();
    
    var request = new SignRequest
    {
        Operation = "burn",
    RequesterId = "TokenSystem",
      Payload = new Dictionary<string, object>
        {
   ["account_id"] = accountId,
  ["amount"] = amount,
         ["reason"] = reason,
            ["burned_at"] = DateTime.UtcNow.ToString("O"),
      ["nonce"] = Guid.NewGuid().ToString()
        }
    };
    
    return await client.SignAsync(request);
}
```

---

## Secret Storage (NEW!)

### Example 4: Store and Retrieve Wallet Mnemonic

```csharp
using StampService.ClientLib;
using NBitcoin;
using Nethereum.HdWallet;

// Generate a new wallet mnemonic
var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
var wallet = new Wallet(mnemonic.ToString(), "");
var account = wallet.GetAccount(0);

using var client = new StampServiceClient();

// Store the mnemonic securely
var metadata = new Dictionary<string, string>
{
    ["KeyType"] = "MasterContract",
    ["Address"] = account.Address,
    ["PrivateKey"] = account.PrivateKey,
    ["Network"] = "Mainnet",
    ["GeneratedAt"] = DateTime.UtcNow.ToString("O"),
    ["Application"] = "TokenController",
    ["Version"] = "3.8.0",
    ["Machine"] = Environment.MachineName,
    ["User"] = Environment.UserName
};

bool stored = await client.StoreSecretAsync("MasterContractKey", mnemonic.ToString(), metadata);

if (stored)
{
    Console.WriteLine("? Mnemonic Stored Securely!");
    Console.WriteLine($"Address: {account.Address}");
    Console.WriteLine($"Key Name: MasterContractKey");
}

// Later... retrieve the mnemonic
var result = await client.RetrieveSecretAsync("MasterContractKey");

if (result.HasValue)
{
    string retrievedMnemonic = result.Value.value;
    DateTime createdAt = result.Value.createdAt;
    var retrievedMetadata = result.Value.metadata;

    // Recreate wallet from mnemonic
    var restoredWallet = new Wallet(retrievedMnemonic, "");
    var restoredAccount = restoredWallet.GetAccount(0);

    Console.WriteLine("? Mnemonic Retrieved!");
 Console.WriteLine($"Address: {restoredAccount.Address}");
    Console.WriteLine($"Created: {createdAt}");
    Console.WriteLine($"Network: {retrievedMetadata["Network"]}");
}
```

### Example 5: Check Before Generate Pattern

```csharp
using StampService.ClientLib;
using NBitcoin;
using Nethereum.HdWallet;

async Task<Wallet> GetOrCreateWallet(string keyName)
{
  using var client = new StampServiceClient();

    // Check if wallet already exists
    bool exists = await client.SecretExistsAsync(keyName);

    if (exists)
    {
        // Retrieve existing wallet
        var result = await client.RetrieveSecretAsync(keyName);
        if (result.HasValue)
        {
        Console.WriteLine($"? Retrieved existing wallet: {keyName}");
            return new Wallet(result.Value.value, "");
        }
    }

    // Generate new wallet
    Console.WriteLine($"? Generating new wallet: {keyName}");
    var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
  var wallet = new Wallet(mnemonic.ToString(), "");
    var account = wallet.GetAccount(0);

    // Store for future use
    var metadata = new Dictionary<string, string>
    {
 ["Address"] = account.Address,
        ["GeneratedAt"] = DateTime.UtcNow.ToString("O")
    };

  await client.StoreSecretAsync(keyName, mnemonic.ToString(), metadata);
    Console.WriteLine($"? Wallet stored: {account.Address}");

    return wallet;
}

// Usage
var masterWallet = await GetOrCreateWallet("MasterContractKey");
var proxyWallet = await GetOrCreateWallet("MasterProxyKey");
```

### Example 6: Multiple Key Management

```csharp
using StampService.ClientLib;

async Task StoreMultipleKeys()
{
    using var client = new StampServiceClient();

    // Store different types of keys
    var keys = new Dictionary<string, (string value, Dictionary<string, string> metadata)>
    {
        ["MasterContractKey"] = (
     "twelve word mnemonic phrase here...",
  new Dictionary<string, string>
            {
          ["KeyType"] = "MasterContract",
 ["Network"] = "Mainnet",
          ["Address"] = "0x..."
          }
        ),
    ["MasterProxyKey"] = (
            "another twelve word mnemonic...",
            new Dictionary<string, string>
        {
    ["KeyType"] = "ProxyContract",
          ["Network"] = "Mainnet",
        ["Address"] = "0x..."
     }
        ),
        ["APIKey_Infura"] = (
            "your-infura-api-key-here",
          new Dictionary<string, string>
    {
           ["Provider"] = "Infura",
       ["Purpose"] = "Blockchain RPC"
            }
        ),
        ["APIKey_Etherscan"] = (
            "your-etherscan-api-key",
            new Dictionary<string, string>
            {
     ["Provider"] = "Etherscan",
         ["Purpose"] = "Contract Verification"
            }
        )
    };

    foreach (var (name, (value, metadata)) in keys)
    {
        bool stored = await client.StoreSecretAsync(name, value, metadata);
        Console.WriteLine($"{(stored ? "?" : "?")} Stored: {name}");
    }
}

// Retrieve all keys
async Task<Dictionary<string, string>> RetrieveAllKeys()
{
    using var client = new StampServiceClient();

  // List all secret names
    var secretNames = await client.ListSecretsAsync();
    var secrets = new Dictionary<string, string>();

  foreach (var name in secretNames)
    {
    var result = await client.RetrieveSecretAsync(name);
        if (result.HasValue)
        {
            secrets[name] = result.Value.value;
 }
    }

    return secrets;
}
```

### Example 7: WPF Button - Generate or Retrieve Keys

```csharp
using System.Windows;
using StampService.ClientLib;
using NBitcoin;
using Nethereum.HdWallet;

private async void MnemonicBtn_Click(object sender, RoutedEventArgs e)
{
    try
    {
        using var client = new StampServiceClient();

 // Check service status first
        var status = await client.GetStatusAsync();
        if (!status.IsRunning || !status.KeyPresent)
      {
     MessageBox.Show(
          "? Stamp Service Not Ready!\n\n" +
      "The service is not running or has no signing key.\n" +
         "Please ensure the service is installed and started.",
   "Service Error",
      MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        // Check if keys already exist
      bool masterExists = await client.SecretExistsAsync("MasterContractKey");
      bool proxyExists = await client.SecretExistsAsync("MasterProxyKey");

        if (masterExists && proxyExists)
        {
  var result = MessageBox.Show(
  "?? Keys Already Exist!\n\n" +
         "Master Contract Key and Master Proxy Key are already stored.\n\n" +
          "Do you want to:\n" +
           "• YES - Retrieve existing keys\n" +
 "• NO - Generate new keys (overwrites existing)\n" +
        "• CANCEL - Do nothing",
          "Keys Exist",
            MessageBoxButton.YesNoCancel,
           MessageBoxImage.Question);

       if (result == MessageBoxResult.Yes)
          {
       // Retrieve existing keys
      await RetrieveExistingKeys(client);
         return;
            }
      else if (result == MessageBoxResult.Cancel)
  {
         return;
 }
         // If NO, continue to generate new keys
        }

        // Generate new keys
        await GenerateAndStoreKeys(client);
    }
    catch (Exception ex)
    {
        MessageBox.Show(
            $"? Error: {ex.Message}\n\n" +
      $"Details: {ex.GetType().Name}",
      "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}

private async Task RetrieveExistingKeys(StampServiceClient client)
{
    var masterResult = await client.RetrieveSecretAsync("MasterContractKey");
    var proxyResult = await client.RetrieveSecretAsync("MasterProxyKey");

    if (masterResult.HasValue && proxyResult.HasValue)
    {
        var masterWallet = new Wallet(masterResult.Value.value, "");
        var proxyWallet = new Wallet(proxyResult.Value.value, "");

 var masterAccount = masterWallet.GetAccount(0);
    var proxyAccount = proxyWallet.GetAccount(0);

        MessageBox.Show(
            "? Keys Retrieved Successfully!\n\n" +
    $"Master Contract Address:\n{masterAccount.Address}\n\n" +
    $"Master Proxy Address:\n{proxyAccount.Address}\n\n" +
 $"Master Created: {masterResult.Value.createdAt:yyyy-MM-dd HH:mm:ss}\n" +
            $"Proxy Created: {proxyResult.Value.createdAt:yyyy-MM-dd HH:mm:ss}\n\n" +
            $"Network: {masterResult.Value.metadata.GetValueOrDefault("Network", "Unknown")}",
   "Keys Retrieved",
     MessageBoxButton.OK,
   MessageBoxImage.Information);
    }
}

private async Task GenerateAndStoreKeys(StampServiceClient client)
{
    // Generate Master Contract Key
    var masterMnemo = new Mnemonic(Wordlist.English, WordCount.Twelve);
    var masterWallet = new Wallet(masterMnemo.ToString(), "");
    var masterAccount = masterWallet.GetAccount(0);

    // Generate Master Proxy Key
    var proxyMnemo = new Mnemonic(Wordlist.English, WordCount.Twelve);
    var proxyWallet = new Wallet(proxyMnemo.ToString(), "");
    var proxyAccount = proxyWallet.GetAccount(0);

    // Prepare metadata
    var network = "Mainnet"; // Or get from your ConnectNethereum.CurrentNetwork
    var version = "3.8.0";   // Your app version

    var masterMetadata = new Dictionary<string, string>
    {
        ["KeyType"] = "MasterContract",
        ["Address"] = masterAccount.Address,
        ["PrivateKey"] = masterAccount.PrivateKey,
        ["Network"] = network,
        ["GeneratedAt"] = DateTime.UtcNow.ToString("O"),
   ["Application"] = "TokenController",
     ["Version"] = version,
        ["Machine"] = Environment.MachineName,
        ["User"] = Environment.UserName
    };

    var proxyMetadata = new Dictionary<string, string>
    {
        ["KeyType"] = "MasterProxy",
   ["Address"] = proxyAccount.Address,
   ["PrivateKey"] = proxyAccount.PrivateKey,
        ["Network"] = network,
        ["GeneratedAt"] = DateTime.UtcNow.ToString("O"),
  ["Application"] = "TokenController",
 ["Version"] = version,
        ["Machine"] = Environment.MachineName,
 ["User"] = Environment.UserName
    };

    // Store both keys
    bool masterStored = await client.StoreSecretAsync("MasterContractKey", masterMnemo.ToString(), masterMetadata);
 bool proxyStored = await client.StoreSecretAsync("MasterProxyKey", proxyMnemo.ToString(), proxyMetadata);

    if (masterStored && proxyStored)
    {
        MessageBox.Show(
            "? Keys Generated and Stored Successfully!\n\n" +
    $"Master Contract Address:\n{masterAccount.Address}\n\n" +
      $"Master Proxy Address:\n{proxyAccount.Address}\n\n" +
    "Keys have been securely stored in the Key Management Service.\n" +
         "• Encrypted with DPAPI (LocalMachine scope)\n" +
      "• Stored in Windows Registry\n" +
        "• Only accessible by LocalSystem and Administrators\n\n" +
  "? IMPORTANT:\n" +
            "Make sure the StampService has backup shares stored safely!\n" +
  "If the service master key is lost, these secrets cannot be recovered.",
        "Keys Stored Successfully",
          MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
    else
    {
    MessageBox.Show(
   "? Failed to Store Keys!\n\n" +
     $"Master: {(masterStored ? "? Stored" : "? Failed")}\n" +
            $"Proxy: {(proxyStored ? "? Stored" : "? Failed")}\n\n" +
            "Please check:\n" +
            "• Service is running\n" +
   "• You have administrator privileges\n" +
            "• Service logs for errors",
     "Storage Failed",
    MessageBoxButton.OK,
 MessageBoxImage.Error);
 }
}
```

### Example 8: List and Manage Secrets

```csharp
using StampService.ClientLib;

async Task ManageSecrets()
{
    using var client = new StampServiceClient();

    // List all secrets
    var secrets = await client.ListSecretsAsync();

    Console.WriteLine($"\n?? Found {secrets.Count} secrets:");
    foreach (var name in secrets)
    {
     Console.WriteLine($"  • {name}");
  }

    // Get details for each secret (without showing the value)
    foreach (var name in secrets)
    {
 var result = await client.RetrieveSecretAsync(name);
        if (result.HasValue)
    {
            Console.WriteLine($"\n?? {name}:");
            Console.WriteLine($"   Created: {result.Value.createdAt:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"   Metadata:");
    foreach (var (key, value) in result.Value.metadata)
        {
          Console.WriteLine($"     - {key}: {value}");
        }
        }
    }
}

// Delete old secrets
async Task CleanupOldSecrets(TimeSpan olderThan)
{
    using var client = new StampServiceClient();

    var secrets = await client.ListSecretsAsync();
    var deleted = 0;

    foreach (var name in secrets)
    {
    var result = await client.RetrieveSecretAsync(name);
        if (result.HasValue)
        {
     var age = DateTime.UtcNow - result.Value.createdAt;
            if (age > olderThan)
   {
                bool success = await client.DeleteSecretAsync(name);
       if (success)
    {
        Console.WriteLine($"? Deleted old secret: {name} (age: {age.Days} days)");
 deleted++;
          }
    }
    }
    }

    Console.WriteLine($"\n? Deleted {deleted} old secrets");
}
```

### Example 9: Secure Configuration Management

```csharp
using StampService.ClientLib;

// Store application configuration secrets
async Task StoreAppConfig()
{
    using var client = new StampServiceClient();

    var configs = new Dictionary<string, string>
    {
        ["Database_ConnectionString"] = "Server=...;Database=...;",
        ["API_StripeKey"] = "sk_live_...",
        ["API_TwilioSid"] = "AC...",
        ["API_SendGridKey"] = "SG...",
        ["Encryption_MasterKey"] = "base64-encoded-key..."
    };

    foreach (var (name, value) in configs)
    {
      var metadata = new Dictionary<string, string>
        {
      ["Type"] = "Configuration",
            ["Environment"] = "Production",
            ["UpdatedAt"] = DateTime.UtcNow.ToString("O")
        };

        await client.StoreSecretAsync(name, value, metadata);
        Console.WriteLine($"? Stored config: {name}");
    }
}

// Retrieve configuration at startup
async Task<Dictionary<string, string>> LoadAppConfig()
{
    using var client = new StampServiceClient();

    var config = new Dictionary<string, string>();
    var secretNames = await client.ListSecretsAsync();

 foreach (var name in secretNames.Where(n => n.StartsWith("Database_") || n.StartsWith("API_")))
    {
        var result = await client.RetrieveSecretAsync(name);
        if (result.HasValue)
        {
 config[name] = result.Value.value;
        }
    }

    return config;
}
```

---

## Health Monitoring

### Example 10: Service Health Check

```csharp
using StampService.ClientLib;

async Task MonitorServiceHealth()
{
using var client = new StampServiceClient();
    
    while (true)
    {
        try
        {
      // Get service status
  var status = await client.GetStatusAsync();
   
            if (!status.IsRunning || !status.KeyPresent)
 {
             Console.WriteLine("?  WARNING: Service not healthy!");
        // Send alert
      }
            
      // Request test stamp
            var testResponse = await client.TestStampAsync();
 bool isValid = client.VerifySignature(testResponse);
      
            if (!isValid)
  {
Console.WriteLine("?  WARNING: Signature verification failed!");
   // Send alert
       }
          
    Console.WriteLine($"? Service healthy - Uptime: {TimeSpan.FromSeconds(status.UptimeSeconds)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Service unreachable: {ex.Message}");
            // Send alert
        }
   
        // Check every 5 minutes
        await Task.Delay(TimeSpan.FromMinutes(5));
    }
}
```

---

## Error Handling

### Example 11: Retry Logic

```csharp
using StampService.ClientLib;
using StampService.Core.Models;

async Task<SignedResponse?> SignWithRetry(SignRequest request, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            using var client = new StampServiceClient(timeoutMs: 10000);
return await client.SignAsync(request);
 }
        catch (TimeoutException) when (attempt < maxRetries)
        {
     Console.WriteLine($"Timeout on attempt {attempt}, retrying...");
          await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
        }
 catch (IOException) when (attempt < maxRetries)
        {
    Console.WriteLine($"Connection error on attempt {attempt}, retrying...");
            await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
        }
   catch (Exception ex)
        {
   Console.WriteLine($"Error: {ex.Message}");
            throw;
 }
    }
    
    return null;
}

// Usage
var request = new SignRequest { /* ... */ };
var response = await SignWithRetry(request);
if (response != null)
{
    Console.WriteLine("? Signed successfully");
}
```

### Example 12: Secret Storage with Error Handling

```csharp
using StampService.ClientLib;

async Task<bool> SafeStoreSecret(string name, string value, Dictionary<string, string>? metadata = null)
{
    try
    {
        using var client = new StampServiceClient();
        
        // Check service is available
        var status = await client.GetStatusAsync();
        if (!status.IsRunning)
 {
            Console.WriteLine("? Service not running");
            return false;
        }

        // Store secret
        bool stored = await client.StoreSecretAsync(name, value, metadata);
        
  if (stored)
        {
            Console.WriteLine($"? Secret '{name}' stored successfully");
            return true;
        }
      else
        {
            Console.WriteLine($"? Failed to store secret '{name}'");
  return false;
        }
    }
    catch (TimeoutException)
    {
        Console.WriteLine("? Service timeout");
        return false;
    }
    catch (IOException ex)
    {
        Console.WriteLine($"? Connection error: {ex.Message}");
        return false;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"? Unexpected error: {ex.Message}");
        return false;
    }
}

async Task<string?> SafeRetrieveSecret(string name)
{
 try
    {
        using var client = new StampServiceClient();
        
     // Check if exists first
        bool exists = await client.SecretExistsAsync(name);
        if (!exists)
        {
         Console.WriteLine($"? Secret '{name}' does not exist");
        return null;
      }

        // Retrieve
        var result = await client.RetrieveSecretAsync(name);
    if (result.HasValue)
        {
       Console.WriteLine($"? Secret '{name}' retrieved");
    return result.Value.value;
        }

        return null;
    }
    catch (Exception ex)
    {
   Console.WriteLine($"? Error retrieving '{name}': {ex.Message}");
        return null;
    }
}
```

---

## Multi-Signature Verification

### Example 13: Transaction Verification

```csharp
using StampService.ClientLib;
using StampService.Core.Models;

// Your transaction record
public class Transaction
{
    public string Id { get; set; }
    public string Operation { get; set; }
 public Dictionary<string, object> Data { get; set; }
  public SignedResponse Signature { get; set; }
}

// Verify a stored transaction
bool VerifyStoredTransaction(Transaction transaction)
{
    using var client = new StampServiceClient();
    
    // Verify the signature matches the data
    return client.VerifySignature(transaction.Signature);
}

// Verify multiple transactions
async Task<Dictionary<string, bool>> VerifyBatch(List<Transaction> transactions)
{
    var results = new Dictionary<string, bool>();
    using var client = new StampServiceClient();
    
    foreach (var tx in transactions)
    {
   bool isValid = client.VerifySignature(tx.Signature);
        results[tx.Id] = isValid;
        
        if (!isValid)
        {
            Console.WriteLine($"?  Transaction {tx.Id} has invalid signature!");
     }
 }
    
    return results;
}
```

---

## Integration Checklist

### Initial Setup
- [ ] Install StampService on a secure server
- [ ] Create and distribute backup shares
- [ ] Test service with AdminCLI commands
- [ ] Verify service starts automatically on boot

### Code Integration
- [ ] Add StampService.ClientLib NuGet package reference
- [ ] Implement signing operations for critical actions
- [ ] Implement secret storage for sensitive data (mnemonics, keys, API keys)
- [ ] Store signatures/secrets with your transaction records
- [ ] Implement signature verification for audit trails
- [ ] Add error handling and retry logic

### Security & Monitoring
- [ ] Set up health monitoring
- [ ] Configure logging for client operations
- [ ] Test recovery process with backup shares
- [ ] Document secret naming conventions
- [ ] Create backup/restore procedures

### Testing
- [ ] Test all secret storage operations
- [ ] Test signing operations under load
- [ ] Test error scenarios (service down, timeout)
- [ ] Verify secrets persist across service restarts
- [ ] Test secret retrieval after system reboot

### Documentation
- [ ] Document your integration for your team
- [ ] Create runbook for common operations
- [ ] Document disaster recovery procedures
- [ ] List all secrets and their purposes

---

## Performance Considerations

- Each signature operation takes ~1-2ms (Ed25519)
- Each secret storage operation takes ~2-5ms (Registry + DPAPI)
- Named Pipes are local-only (very fast, ~0.1ms overhead)
- Consider batching if signing thousands of operations per second
- Service is thread-safe and can handle concurrent requests
- Monitor service logs for performance issues

---

## Security Notes

### Signing
- Signatures are deterministic for the same payload
- Include nonces/timestamps to prevent replay attacks
- The service never exposes the private key
- All operations are logged in audit logs
- Consider encrypting signatures in your database
- Rotate shares periodically (create new key + shares)

### Secret Storage
- Secrets are encrypted with DPAPI (LocalMachine scope)
- Only LocalSystem and Administrators can access
- Secrets are stored in Windows Registry (more secure than files)
- Secrets are NOT included in backup shares (only signing key is)
- If you reinstall Windows, DPAPI key changes and secrets are lost
- Plan for secret backup/export if needed
- Use descriptive names for secrets (e.g., `MasterContractKey_Mainnet`)
- Add metadata to track purpose, network, version

---

## Common Patterns

### Pattern 1: Initialize Once, Use Many Times
```csharp
// Bad - creates new client each time
for (int i = 0; i < 100; i++)
{
  using var client = new StampServiceClient();
    await client.SignAsync(request);
}

// Good - reuse client
using var client = new StampServiceClient();
for (int i = 0; i < 100; i++)
{
    await client.SignAsync(request);
}
```

### Pattern 2: Check Before Store
```csharp
// Always check if secret exists before generating new one
if (!await client.SecretExistsAsync("MasterKey"))
{
    // Generate and store
    await client.StoreSecretAsync("MasterKey", newValue, metadata);
}
else
{
    // Retrieve existing
    var result = await client.RetrieveSecretAsync("MasterKey");
}
```

### Pattern 3: Metadata for Tracking
```csharp
// Always include metadata for tracking
var metadata = new Dictionary<string, string>
{
    ["Purpose"] = "Smart Contract Deployment",
    ["Network"] = "Ethereum Mainnet",
    ["Version"] = appVersion,
    ["CreatedBy"] = Environment.UserName,
 ["Machine"] = Environment.MachineName
};
await client.StoreSecretAsync(name, value, metadata);
```

---

## Troubleshooting

### Service Not Available
```csharp
try
{
    var status = await client.GetStatusAsync();
}
catch (IOException)
{
    Console.WriteLine("Service not running. Start with: Start-Service SecureStampService");
}
catch (TimeoutException)
{
    Console.WriteLine("Service not responding. Check service logs.");
}
```

### Secret Not Found
```csharp
var result = await client.RetrieveSecretAsync("MySecret");
if (!result.HasValue)
{
    // Secret doesn't exist
    // Either generate new or restore from backup
}
```

### Signature Verification Failed
```csharp
bool isValid = client.VerifySignature(response);
if (!isValid)
{
    // Either:
    // 1. Data was tampered with
    // 2. Wrong public key
    // 3. Signature encoding issue
}
```

---

## Additional Resources

- **Service Logs**: `C:\ProgramData\StampService\Logs\`
- **Registry Location**: `HKLM:\SOFTWARE\StampService\`
- **Admin Commands**: Run `.\StampService.AdminCLI.exe --help`
- **Recovery Guide**: See `Resources\RECOVERY-GUIDE.md`
- **Secret Storage Guide**: See `Resources\SECRET-STORAGE-GUIDE.md`
