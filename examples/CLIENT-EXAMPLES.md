# Example Client Application

This directory contains example applications demonstrating how to integrate with the Secure Stamp Service.

## Example 1: Simple Token Minting

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

## Example 2: Batch Operations

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

## Example 3: Health Monitoring

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
                Console.WriteLine("??  WARNING: Service not healthy!");
                // Send alert
            }
            
            // Request test stamp
            var testResponse = await client.TestStampAsync();
            bool isValid = client.VerifySignature(testResponse);
            
            if (!isValid)
            {
                Console.WriteLine("??  WARNING: Signature verification failed!");
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

## Example 4: Error Handling

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
```

## Example 5: Multi-Signature Verification

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
            Console.WriteLine($"??  Transaction {tx.Id} has invalid signature!");
        }
    }
    
    return results;
}
```

## Example 6: Freeze/Burn Operations

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

## Integration Checklist

- [ ] Install StampService on a secure server
- [ ] Create and distribute backup shares
- [ ] Add StampService.ClientLib reference to your project
- [ ] Implement sign requests for your operations
- [ ] Store signatures with your transaction records
- [ ] Implement signature verification for audit trails
- [ ] Set up health monitoring
- [ ] Configure retry logic for resilience
- [ ] Test recovery process with backup shares
- [ ] Document your integration for your team

## Performance Considerations

- Each signature operation takes ~1-2ms (Ed25519)
- Named Pipes are local-only (very fast, ~0.1ms overhead)
- Consider batching if signing thousands of operations per second
- Service is thread-safe and can handle concurrent requests
- Monitor service logs for performance issues

## Security Notes

- Signatures are deterministic for the same payload
- Include nonces/timestamps to prevent replay attacks
- The service never exposes the private key
- All operations are logged in audit logs
- Consider encrypting signatures in your database
- Rotate shares periodically (create new key + shares)
