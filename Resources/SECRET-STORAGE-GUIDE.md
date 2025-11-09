# Secret Storage Feature - Usage Guide

## Overview

StampService now supports **secure key-value storage** for sensitive data like mnemonics, private keys, API keys, etc. All secrets are encrypted with DPAPI and stored in the Windows Registry.

---

## Storage Location

```
HKEY_LOCAL_MACHINE\SOFTWARE\StampService\Secrets\
??? MasterContractKey (REG_BINARY, encrypted)
??? MasterProxyKey (REG_BINARY, encrypted)
??? ... (other secrets)
```

Each secret is:
- ? **Encrypted** with DPAPI (LocalMachine scope)
- ? **Stored** in Windows Registry
- ? **Protected** by Windows security
- ? **Includes metadata** (timestamp, custom metadata)

---

## Client Usage Examples

### **Example 1: Store Mnemonic and Retrieve It**

```csharp
using StampService.ClientLib;
using NBitcoin;
using Nethereum.HdWallet;

// Generate mnemonic
var masterMnemo = new Mnemonic(Wordlist.English, WordCount.Twelve);
var masterWallet = new Wallet(masterMnemo.ToString(), "");
var masterAccount = masterWallet.GetAccount(0);

using var stampClient = new StampServiceClient();

// Store the mnemonic securely
var metadata = new Dictionary<string, string>
{
    ["KeyType"] = "MasterContract",
    ["Address"] = masterAccount.Address,
    ["Network"] = "Mainnet",
    ["GeneratedAt"] = DateTime.UtcNow.ToString("O"),
    ["Application"] = "TokenController",
    ["Version"] = "3.8.0"
};

bool stored = await stampClient.StoreSecretAsync(
    "MasterContractKey",
    masterMnemo.ToString(),
    metadata
);

if (stored)
{
    MessageBox.Show(
        $"? Mnemonic Stored Securely!\n\n" +
        $"Address: {masterAccount.Address}\n" +
        $"Key Name: MasterContractKey\n\n" +
   "The mnemonic is encrypted and stored in the secure key service.",
        "Success",
        MessageBoxButton.OK,
  MessageBoxImage.Information);
}
```

### **Example 2: Retrieve Mnemonic**

```csharp
using var stampClient = new StampServiceClient();

// Retrieve the mnemonic
var result = await stampClient.RetrieveSecretAsync("MasterContractKey");

if (result.HasValue)
{
    string mnemonic = result.Value.value;
    DateTime createdAt = result.Value.createdAt;
  var metadata = result.Value.metadata;

    // Recreate wallet from mnemonic
    var wallet = new Wallet(mnemonic, "");
    var account = wallet.GetAccount(0);

    MessageBox.Show(
        $"? Mnemonic Retrieved!\n\n" +
        $"Address: {account.Address}\n" +
        $"Created: {createdAt}\n" +
        $"Network: {metadata["Network"]}\n\n" +
   "Wallet restored successfully.",
        "Retrieved",
        MessageBoxButton.OK,
        MessageBoxImage.Information);
}
else
{
    MessageBox.Show(
        "? Mnemonic not found!\n\n" +
        "The secret 'MasterContractKey' does not exist.",
 "Not Found",
        MessageBoxButton.Warning,
        MessageBoxImage.Warning);
}
```

### **Example 3: Check If Secret Exists**

```csharp
using var stampClient = new StampServiceClient();

bool exists = await stampClient.SecretExistsAsync("MasterContractKey");

if (exists)
{
    // Secret exists, retrieve it
    var result = await stampClient.RetrieveSecretAsync("MasterContractKey");
    // ... use the secret
}
else
{
    // Secret doesn't exist, generate new one
    var newMnemo = new Mnemonic(Wordlist.English, WordCount.Twelve);
    await stampClient.StoreSecretAsync("MasterContractKey", newMnemo.ToString());
}
```

### **Example 4: List All Secrets**

```csharp
using var stampClient = new StampServiceClient();

List<string> secretNames = await stampClient.ListSecretsAsync();

Console.WriteLine($"Found {secretNames.Count} secrets:");
foreach (var name in secretNames)
{
    Console.WriteLine($"  - {name}");
}

// Output:
// Found 3 secrets:
//   - MasterContractKey
//   - MasterProxyKey
//   - APIKey
```

### **Example 5: Delete a Secret**

```csharp
using var stampClient = new StampServiceClient();

bool deleted = await stampClient.DeleteSecretAsync("MasterContractKey");

if (deleted)
{
    MessageBox.Show(
        "? Secret Deleted!\n\n" +
        "The secret 'MasterContractKey' has been permanently removed.",
        "Deleted",
        MessageBoxButton.OK,
        MessageBoxImage.Information);
}
```

---

## Your MnemonicBtn_Click Fixed

Here's your original code updated to use the new secret storage:

```csharp
private async void MnemonicBtn_Click(object sender, RoutedEventArgs e)
{
  using var stampClient = new StampServiceClient();
    var status = await stampClient.GetStatusAsync();

    // Check if keys already exist
    bool masterKeyExists = await stampClient.SecretExistsAsync("MasterContractKey");
    bool proxyKeyExists = await stampClient.SecretExistsAsync("MasterProxyKey");

    if (masterKeyExists && proxyKeyExists)
    {
        var result = MessageBox.Show(
         "Keys already exist!\n\n" +
            "Do you want to retrieve existing keys or generate new ones?\n\n" +
            "Click YES to retrieve existing keys\n" +
    "Click NO to generate new keys (will overwrite existing)",
       "Keys Exist",
       MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

   if (result == MessageBoxResult.Yes)
    {
            // Retrieve existing keys
        var masterResult = await stampClient.RetrieveSecretAsync("MasterContractKey");
 var proxyResult = await stampClient.RetrieveSecretAsync("MasterProxyKey");

    if (masterResult.HasValue && proxyResult.HasValue)
          {
        var masterMnemo = masterResult.Value.value;
         var proxyMnemo = proxyResult.Value.value;

        var masterWallet = new Wallet(masterMnemo, "");
           var proxyWallet = new Wallet(proxyMnemo, "");

    MessageBox.Show(
  "? Keys Retrieved Successfully!\n\n" +
         $"Master Contract Address: {masterWallet.GetAccount(0).Address}\n" +
           $"Master Proxy Address: {proxyWallet.GetAccount(0).Address}\n\n" +
             $"Retrieved from secure storage.",
  "Keys Retrieved",
    MessageBoxButton.OK,
     MessageBoxImage.Information);
       }
   return;
        }
        else if (result == MessageBoxResult.Cancel)
        {
          return;
      }
        // If NO, continue to generate new keys
    }

    // Generate Master Contract Key
    var masterMnemo = new NBitcoin.Mnemonic(NBitcoin.Wordlist.English, NBitcoin.WordCount.Twelve);
    var masterWallet = new Wallet(masterMnemo.ToString(), "");
    var masterAccount = masterWallet.GetAccount(0);

    // Generate Master Proxy Key
    var proxyMnemo = new NBitcoin.Mnemonic(NBitcoin.Wordlist.English, NBitcoin.WordCount.Twelve);
    var proxyWallet = new Wallet(proxyMnemo.ToString(), "");
    var proxyAccount = proxyWallet.GetAccount(0);

    // Store Master Contract Key
    var masterMetadata = new Dictionary<string, string>
    {
        ["KeyType"] = "MasterContract",
        ["PrivateKey"] = masterAccount.PrivateKey,
        ["Address"] = masterAccount.Address,
        ["GeneratedAt"] = DateTime.UtcNow.ToString("O"),
        ["Application"] = "TokenController-DeployContract",
        ["Version"] = Version ?? "3.8.0",
        ["Network"] = ConnectNethereum.CurrentNetwork.ToString(),
   ["Machine"] = Environment.MachineName,
        ["User"] = Environment.UserName
    };

    bool masterStored = await stampClient.StoreSecretAsync(
        "MasterContractKey",
        masterMnemo.ToString(),
   masterMetadata
    );

  // Store Master Proxy Key
    var proxyMetadata = new Dictionary<string, string>
    {
        ["KeyType"] = "MasterProxy",
    ["PrivateKey"] = proxyAccount.PrivateKey,
        ["Address"] = proxyAccount.Address,
        ["GeneratedAt"] = DateTime.UtcNow.ToString("O"),
        ["Application"] = "TokenController-DeployContract",
        ["Version"] = Version ?? "3.8.0",
     ["Network"] = ConnectNethereum.CurrentNetwork.ToString(),
        ["Machine"] = Environment.MachineName,
        ["User"] = Environment.UserName
    };

    bool proxyStored = await stampClient.StoreSecretAsync(
        "MasterProxyKey",
    proxyMnemo.ToString(),
        proxyMetadata
    );

    if (masterStored && proxyStored)
    {
 MessageBox.Show(
 "? Keys Generated and Stored Successfully!\n\n" +
 $"Master Contract Address: {masterAccount.Address}\n" +
   $"Master Proxy Address: {proxyAccount.Address}\n\n" +
            "Keys have been securely stored in the Key Management Service.\n" +
            "Encrypted with DPAPI and stored in Windows Registry.\n\n" +
            "? IMPORTANT: Make sure the service has backup shares stored safely.",
            "Keys Stored Successfully",
            MessageBoxButton.OK,
  MessageBoxImage.Information);
    }
    else
    {
        MessageBox.Show(
    "? Failed to store keys!\n\n" +
      "Please check the service is running and try again.",
            "Storage Failed",
   MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
```

---

## API Reference

### StampServiceClient Methods

| Method | Description | Returns |
|--------|-------------|---------|
| `StoreSecretAsync(name, value, metadata)` | Store a secret | `bool` (success) |
| `RetrieveSecretAsync(name)` | Retrieve a secret | `(string value, DateTime createdAt, Dict metadata)?` |
| `ListSecretsAsync()` | List all secret names | `List<string>` |
| `DeleteSecretAsync(name)` | Delete a secret | `bool` (success) |
| `SecretExistsAsync(name)` | Check if secret exists | `bool` |

---

## Security Notes

1. ? **Encryption**: All secrets encrypted with DPAPI (LocalMachine scope)
2. ? **Storage**: Registry is more secure than file system
3. ? **Access**: Only LocalSystem and Administrators can access
4. ?? **Backup**: Secrets are NOT included in backup shares (only signing key is)
5. ?? **Recovery**: If you reinstall Windows, secrets will be lost (DPAPI key changes)

---

## Best Practices

### **DO:**
- ? Use descriptive secret names (`MasterContractKey`, not `key1`)
- ? Add metadata for tracking (network, version, timestamp)
- ? Check if secret exists before generating new one
- ? Use different secrets for different networks (mainnet vs testnet)

### **DON'T:**
- ? Store secrets in plain text anywhere else
- ? Log secret values
- ? Hard-code secret names
- ? Share secrets across applications without proper namespacing

---

## Troubleshooting

### "Secret not found"
- Check service is running: `Get-Service SecureStampService`
- Verify secret name is correct (case-sensitive)
- List all secrets to see what exists

### "Access Denied"
- Ensure application runs with appropriate permissions
- Check service is running under LocalSystem account

### "Storage Failed"
- Verify service is running
- Check Registry permissions on `HKLM:\SOFTWARE\StampService`
- Review service logs: `C:\ProgramData\StampService\Logs\`

---

## Migration from Old Code

If you were storing keys in the payload of `SignAsync`:

**Before:**
```csharp
var signRequest = new SignRequest
{
    Operation = "store_keys",  // ? This doesn't actually store anything
    RequesterId = "DeployContract",
    Payload = new Dictionary<string, object>
    {
        ["keys"] = keysToStore  // ? Just signing data, not storing
    }
};
var response = await stampClient.SignAsync(signRequest);
```

**After:**
```csharp
// ? Actually stores the data securely
await stampClient.StoreSecretAsync("MasterContractKey", masterMnemo.ToString(), metadata);

// ? Retrieve when needed
var result = await stampClient.RetrieveSecretAsync("MasterContractKey");
if (result.HasValue)
{
    string mnemonic = result.Value.value;
    // Use the mnemonic
}
```

---

## Summary

? **StampService now provides secure key-value storage!**

- Store mnemonics, keys, API keys securely
- Encrypted with DPAPI
- Stored in Windows Registry
- Simple client API
- Includes metadata support

**Your use case is now fully supported!** ??
