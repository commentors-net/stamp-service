# Client Integration Guide - Using StampService NuGet Packages

## Overview

This guide shows you how to integrate Secure Stamp Service into your applications using NuGet packages.

## Prerequisites

### Service Side
- ? StampService installed and running on a machine
- ? Service accessible via Named Pipes (local machine or network)
- ? Backup shares created and stored securely

### Client Side
- ? .NET 8+ application
- ? Windows OS (for Named Pipes support)
- ? NuGet package source configured
- ? StampService.ClientLib package installed

## Installation

### Step 1: Configure NuGet Source

```powershell
# Option A: Local folder (development)
dotnet nuget add source D:\Path\To\nupkgs --name StampServiceLocal

# Option B: Network share (team)
dotnet nuget add source \\server\share\nuget --name CompanyPackages

# Option C: Azure Artifacts (enterprise)
dotnet nuget add source https://pkgs.dev.azure.com/ORG/_packaging/FEED/nuget/v3/index.json --name AzureArtifacts
```

### Step 2: Install Package

```powershell
# In your project directory
dotnet add package StampService.ClientLib --source StampServiceLocal

# Or specify version
dotnet add package StampService.ClientLib --version 1.0.0 --source StampServiceLocal
```

### Step 3: Verify Installation

```powershell
# List installed packages
dotnet list package

# Should show:
# StampService.ClientLib    1.0.0
# StampService.Core         1.0.0 (transitive dependency)
```

## Basic Usage

### Simple Console Application

```csharp
using StampService.ClientLib;
using StampService.Core.Models;

class Program
{
    static async Task Main(string[] args)
    {
        // Create client
    using var client = new StampServiceClient();
        
        // Check service status
     var status = await client.GetStatusAsync();
    Console.WriteLine($"Service Running: {status.IsRunning}");
        Console.WriteLine($"Key Present: {status.KeyPresent}");
        
        // Create sign request
        var request = new SignRequest
    {
      Operation = "transfer",
            RequesterId = "MyApp",
      Payload = new Dictionary<string, object>
       {
         ["from"] = "user123",
            ["to"] = "user456",
         ["amount"] = 1000,
    ["timestamp"] = DateTime.UtcNow
          }
        };
 
        // Sign
    var response = await client.SignAsync(request);
  Console.WriteLine($"Signature: {response.Signature}");
        Console.WriteLine($"Timestamp: {response.Timestamp}");
  
    // Verify (optional)
        bool isValid = client.VerifySignature(response);
        Console.WriteLine($"Signature Valid: {isValid}");
    }
}
```

## Integration Patterns

### Pattern 1: ASP.NET Core Web API

#### Dependency Injection Setup

```csharp
// Program.cs
using StampService.ClientLib;

var builder = WebApplication.CreateBuilder(args);

// Register as singleton (reuses connection pool)
builder.Services.AddSingleton<IStampServiceClient, StampServiceClient>();

// Or register with factory for custom configuration
builder.Services.AddSingleton<IStampServiceClient>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<StampServiceClient>>();
    return new StampServiceClient(
        pipeName: "StampServicePipe",
   timeoutMs: 10000
    );
});

builder.Services.AddControllers();
var app = builder.Build();

app.MapControllers();
app.Run();
```

#### Controller Usage

```csharp
// Controllers/SignatureController.cs
using Microsoft.AspNetCore.Mvc;
using StampService.ClientLib;
using StampService.Core.Models;

[ApiController]
[Route("api/[controller]")]
public class SignatureController : ControllerBase
{
    private readonly IStampServiceClient _stampClient;
    private readonly ILogger<SignatureController> _logger;
    
  public SignatureController(
        IStampServiceClient stampClient,
        ILogger<SignatureController> logger)
    {
        _stampClient = stampClient;
        _logger = logger;
    }
    
    [HttpPost("sign")]
    public async Task<IActionResult> Sign([FromBody] SignatureRequestDto request)
    {
try
        {
   var signRequest = new SignRequest
            {
 Operation = request.Operation,
         RequesterId = User.Identity?.Name ?? "Anonymous",
    Payload = request.Payload
 };
            
var response = await _stampClient.SignAsync(signRequest);
        
  _logger.LogInformation(
                "Signed operation {Operation} for user {User}",
    request.Operation,
  User.Identity?.Name
  );
            
      return Ok(new
            {
          Signature = response.Signature,
            Timestamp = response.Timestamp,
                SignerId = response.SignerId
            });
   }
        catch (Exception ex)
        {
     _logger.LogError(ex, "Failed to sign request");
       return StatusCode(500, "Signing failed");
        }
    }
    
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
     try
        {
         var status = await _stampClient.GetStatusAsync();
     return Ok(status);
     }
        catch (Exception ex)
        {
      _logger.LogError(ex, "Failed to get service status");
   return StatusCode(500, "Service unavailable");
        }
  }
    
    [HttpPost("test")]
    public async Task<IActionResult> TestStamp()
    {
        try
        {
         var response = await _stampClient.TestStampAsync();
      var isValid = _stampClient.VerifySignature(response);
   
          return Ok(new
            {
      Response = response,
       IsValid = isValid
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test stamp failed");
      return StatusCode(500, "Test failed");
        }
    }
}

public class SignatureRequestDto
{
    public string Operation { get; set; } = string.Empty;
    public Dictionary<string, object> Payload { get; set; } = new();
}
```

### Pattern 2: Background Worker Service

```csharp
// Worker.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StampService.ClientLib;
using StampService.Core.Models;

public class SigningWorker : BackgroundService
{
    private readonly IStampServiceClient _stampClient;
    private readonly ILogger<SigningWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);
    
    public SigningWorker(
        IStampServiceClient stampClient,
        ILogger<SigningWorker> logger)
  {
        _stampClient = stampClient;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
    _logger.LogInformation("Signing Worker started");
        
     while (!stoppingToken.IsCancellationRequested)
        {
            try
        {
   // Check service health
   var status = await _stampClient.GetStatusAsync();
                
                if (!status.IsRunning || !status.KeyPresent)
  {
         _logger.LogWarning("StampService not ready");
 await Task.Delay(_interval, stoppingToken);
         continue;
      }
                
   // Process signing queue (example)
    await ProcessPendingSignatures(stoppingToken);
            }
         catch (Exception ex)
      {
  _logger.LogError(ex, "Error in signing worker");
          }
   
      await Task.Delay(_interval, stoppingToken);
        }
  
        _logger.LogInformation("Signing Worker stopped");
    }
    
    private async Task ProcessPendingSignatures(CancellationToken cancellationToken)
    {
        // Your business logic here
        // Example: Get pending items from database, sign them, save results
        
  var pendingItems = await GetPendingItemsFromDatabase();
  
        foreach (var item in pendingItems)
        {
      if (cancellationToken.IsCancellationRequested)
     break;
 
            try
          {
       var request = new SignRequest
    {
 Operation = item.OperationType,
          RequesterId = "SigningWorker",
         Payload = item.Data
                };
   
       var response = await _stampClient.SignAsync(request);
     
     await SaveSignatureToDatabase(item.Id, response.Signature);
           
    _logger.LogInformation(
        "Signed item {ItemId}, operation {Operation}",
             item.Id,
item.OperationType
      );
            }
   catch (Exception ex)
     {
         _logger.LogError(ex, "Failed to sign item {ItemId}", item.Id);
    }
        }
  }
    
    private Task<List<PendingItem>> GetPendingItemsFromDatabase()
    {
        // Your database logic
  return Task.FromResult(new List<PendingItem>());
    }
    
    private Task SaveSignatureToDatabase(int itemId, string signature)
  {
        // Your database logic
   return Task.CompletedTask;
    }
}

public class PendingItem
{
    public int Id { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
}

// Program.cs
var builder = Host.CreateApplicationBuilder(args);

// Register StampService client
builder.Services.AddSingleton<IStampServiceClient, StampServiceClient>();

// Register worker
builder.Services.AddHostedService<SigningWorker>();

var host = builder.Build();
host.Run();
```

### Pattern 3: WPF Desktop Application

```csharp
// MainWindow.xaml.cs
using System.Windows;
using StampService.ClientLib;
using StampService.Core.Models;

public partial class MainWindow : Window
{
    private readonly IStampServiceClient _stampClient;
 
    public MainWindow()
    {
        InitializeComponent();
        _stampClient = new StampServiceClient();
   
        Loaded += MainWindow_Loaded;
    }
    
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
  try
  {
         var status = await _stampClient.GetStatusAsync();
 StatusLabel.Content = status.IsRunning ? "Connected" : "Disconnected";
        }
      catch (Exception ex)
        {
          MessageBox.Show($"Failed to connect to service: {ex.Message}");
        }
    }
  
    private async void SignButton_Click(object sender, RoutedEventArgs e)
    {
   try
        {
var request = new SignRequest
            {
         Operation = "sign-document",
        RequesterId = Environment.UserName,
          Payload = new Dictionary<string, object>
                {
            ["documentId"] = DocumentIdTextBox.Text,
          ["hash"] = DocumentHashTextBox.Text,
            ["timestamp"] = DateTime.UtcNow
                }
            };

            var response = await _stampClient.SignAsync(request);
            
 SignatureTextBox.Text = response.Signature;
   MessageBox.Show("Document signed successfully!");
        }
 catch (Exception ex)
{
            MessageBox.Show($"Signing failed: {ex.Message}");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
     _stampClient?.Dispose();
      base.OnClosed(e);
    }
}
```

### Pattern 4: Minimal API

```csharp
// Program.cs
using StampService.ClientLib;
using StampService.Core.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IStampServiceClient, StampServiceClient>();

var app = builder.Build();

// Sign endpoint
app.MapPost("/sign", async (SignRequest request, IStampServiceClient client) =>
{
    try
    {
        var response = await client.SignAsync(request);
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// Status endpoint
app.MapGet("/status", async (IStampServiceClient client) =>
{
    var status = await client.GetStatusAsync();
    return Results.Ok(status);
});

// Test endpoint
app.MapGet("/test", async (IStampServiceClient client) =>
{
    var response = await client.TestStampAsync();
    var isValid = client.VerifySignature(response);
    return Results.Ok(new { response, isValid });
});

app.Run();
```

## Advanced Usage

### Custom Pipe Name and Timeout

```csharp
// If service uses custom pipe name
var client = new StampServiceClient(
    pipeName: "CustomPipeName",
    timeoutMs: 10000 // 10 seconds
);
```

### Error Handling

```csharp
using StampService.ClientLib;
using System.IO.Pipes;

try
{
    using var client = new StampServiceClient();
    var response = await client.SignAsync(request);
}
catch (TimeoutException)
{
    // Service didn't respond in time
    Console.WriteLine("Request timed out");
}
catch (IOException)
{
    // Named Pipe connection failed
    Console.WriteLine("Cannot connect to service");
}
catch (InvalidOperationException ex)
{
    // Service returned error
    Console.WriteLine($"Service error: {ex.Message}");
}
catch (Exception ex)
{
    // Other errors
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

### Health Checks

```csharp
public class StampServiceHealthCheck : IHealthCheck
{
    private readonly IStampServiceClient _client;
    
    public StampServiceHealthCheck(IStampServiceClient client)
    {
        _client = client;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
     HealthCheckContext context,
     CancellationToken cancellationToken = default)
    {
        try
        {
      var status = await _client.GetStatusAsync();
            
            if (!status.IsRunning)
            {
  return HealthCheckResult.Unhealthy("Service not running");
          }
  
   if (!status.KeyPresent)
            {
           return HealthCheckResult.Degraded("Key not present");
     }
       
     // Perform test stamp
            var testResponse = await _client.TestStampAsync();
      var isValid = _client.VerifySignature(testResponse);
            
            if (!isValid)
        {
         return HealthCheckResult.Unhealthy("Signature verification failed");
            }
       
            return HealthCheckResult.Healthy("Service operational", new Dictionary<string, object>
    {
     ["Uptime"] = TimeSpan.FromSeconds(status.UptimeSeconds),
       ["Algorithm"] = status.Algorithm ?? "Unknown"
     });
   }
        catch (Exception ex)
        {
     return HealthCheckResult.Unhealthy("Cannot connect to service", ex);
        }
    }
}

// Register in Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<StampServiceHealthCheck>("stamp-service");

app.MapHealthChecks("/health");
```

### Logging Integration

```csharp
public class StampServiceClientWrapper
{
    private readonly IStampServiceClient _client;
    private readonly ILogger<StampServiceClientWrapper> _logger;
    
    public StampServiceClientWrapper(
        IStampServiceClient client,
        ILogger<StampServiceClientWrapper> logger)
    {
   _client = client;
        _logger = logger;
    }
    
 public async Task<SignedResponse> SignWithLoggingAsync(SignRequest request)
    {
 _logger.LogInformation(
     "Signing request - Operation: {Operation}, Requester: {Requester}",
            request.Operation,
  request.RequesterId
        );
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
   
        try
        {
            var response = await _client.SignAsync(request);
    
            stopwatch.Stop();
       
            _logger.LogInformation(
                "Sign completed - Duration: {Duration}ms, Algorithm: {Algorithm}",
   stopwatch.ElapsedMilliseconds,
    response.Algorithm
            );
    
        return response;
        }
        catch (Exception ex)
    {
            stopwatch.Stop();
         
          _logger.LogError(
           ex,
          "Sign failed - Duration: {Duration}ms, Operation: {Operation}",
         stopwatch.ElapsedMilliseconds,
        request.Operation
            );

            throw;
   }
    }
}
```

## Configuration

### appsettings.json

```json
{
  "StampService": {
    "PipeName": "StampServicePipe",
    "TimeoutMs": 5000,
    "RetryCount": 3,
    "RetryDelayMs": 1000
  },
  "Logging": {
    "LogLevel": {
      "StampService": "Information"
    }
  }
}
```

### Configuration Class

```csharp
public class StampServiceOptions
{
    public string PipeName { get; set; } = "StampServicePipe";
  public int TimeoutMs { get; set; } = 5000;
    public int RetryCount { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}

// Program.cs
builder.Services.Configure<StampServiceOptions>(
    builder.Configuration.GetSection("StampService")
);

builder.Services.AddSingleton<IStampServiceClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<StampServiceOptions>>().Value;
    return new StampServiceClient(options.PipeName, options.TimeoutMs);
});
```

## Testing

### Unit Testing with Mock

```csharp
// Mock interface
public interface IStampServiceClient : IDisposable
{
    Task<SignedResponse> SignAsync(SignRequest request);
    Task<SignedResponse> TestStampAsync();
  Task<ServiceStatus> GetStatusAsync();
    bool VerifySignature(SignedResponse signedResponse);
}

// Test
public class MyServiceTests
{
    [Fact]
    public async Task SignDocument_WhenServiceAvailable_ReturnsSignature()
    {
        // Arrange
        var mockClient = new Mock<IStampServiceClient>();
        mockClient
      .Setup(x => x.SignAsync(It.IsAny<SignRequest>()))
            .ReturnsAsync(new SignedResponse
{
    Signature = "test-signature",
          Algorithm = "Ed25519",
  Timestamp = DateTime.UtcNow
            });
      
        var service = new MyService(mockClient.Object);
        
        // Act
        var result = await service.SignDocumentAsync("doc-123");
        
        // Assert
    Assert.NotNull(result);
        Assert.Equal("test-signature", result.Signature);
    }
}
```

## Troubleshooting

### Issue: Cannot Connect to Service

**Check:**
```csharp
// 1. Verify service is running
var psi = new ProcessStartInfo
{
    FileName = "sc.exe",
 Arguments = "query SecureStampService",
    RedirectStandardOutput = true
};
var process = Process.Start(psi);
var output = await process.StandardOutput.ReadToEndAsync();
Console.WriteLine(output);

// 2. Verify pipe name
var client = new StampServiceClient("StampServicePipe"); // Default name

// 3. Check firewall (usually not needed for Named Pipes)
```

### Issue: Timeout Errors

**Solutions:**
```csharp
// Increase timeout
var client = new StampServiceClient(timeoutMs: 30000); // 30 seconds

// Or retry logic
async Task<SignedResponse> SignWithRetryAsync(SignRequest request, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
         return await client.SignAsync(request);
     }
        catch (TimeoutException) when (i < maxRetries - 1)
        {
            await Task.Delay(1000 * (i + 1)); // Exponential backoff
        }
 }
    throw new TimeoutException("All retry attempts failed");
}
```

### Issue: Service Not Ready

**Check status first:**
```csharp
async Task EnsureServiceReadyAsync()
{
    var maxAttempts = 10;
    for (int i = 0; i < maxAttempts; i++)
    {
        try
        {
            var status = await client.GetStatusAsync();
         if (status.IsRunning && status.KeyPresent)
     return;
        }
      catch { }
        
        await Task.Delay(1000);
    }
    
    throw new InvalidOperationException("Service not ready after waiting");
}
```

## Best Practices

### 1. Use Dependency Injection
```csharp
// Good: Singleton registration
builder.Services.AddSingleton<IStampServiceClient, StampServiceClient>();

// Bad: Creating new instances repeatedly
var client = new StampServiceClient(); // Don't do this in loops
```

### 2. Handle Errors Gracefully
```csharp
try
{
    var response = await client.SignAsync(request);
    // Process response
}
catch (IOException)
{
    // Service unavailable - maybe retry or use cached data
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    // Don't expose internal errors to users
    throw new ApplicationException("Signing failed");
}
```

### 3. Use Health Checks
```csharp
// Check service health before critical operations
var status = await client.GetStatusAsync();
if (!status.KeyPresent)
{
    // Alert administrators
    _logger.LogCritical("StampService has no key!");
}
```

### 4. Cache Public Key
```csharp
// Cache public key to reduce calls
private string? _cachedPublicKey;
private DateTime _cacheExpiry;

async Task<string> GetPublicKeyAsync()
{
    if (_cachedPublicKey != null && DateTime.UtcNow < _cacheExpiry)
        return _cachedPublicKey;
    
    var status = await client.GetStatusAsync();
    _cachedPublicKey = status.PublicKey;
    _cacheExpiry = DateTime.UtcNow.AddHours(1);
  
    return _cachedPublicKey!;
}
```

### 5. Monitor Performance
```csharp
using var activity = Activity.StartActivity("StampService.Sign");
activity?.SetTag("operation", request.Operation);

var stopwatch = Stopwatch.StartNew();
var response = await client.SignAsync(request);
stopwatch.Stop();

activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);

if (stopwatch.ElapsedMilliseconds > 1000)
{
    _logger.LogWarning("Slow signing operation: {Duration}ms", stopwatch.ElapsedMilliseconds);
}
```

## Reference

### StampServiceClient Methods

| Method | Description | Returns |
|--------|-------------|---------|
| `SignAsync(request)` | Sign a request | `Task<SignedResponse>` |
| `TestStampAsync()` | Health check signature | `Task<SignedResponse>` |
| `GetStatusAsync()` | Get service status | `Task<ServiceStatus>` |
| `VerifySignature(response)` | Verify signature locally | `bool` |

### SignRequest Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Operation` | string | Yes | Operation type (mint, burn, etc.) |
| `RequesterId` | string | Yes | Who is requesting the signature |
| `Payload` | Dictionary<string, object> | Yes | Data to sign |

### SignedResponse Properties

| Property | Type | Description |
|----------|------|-------------|
| `Signature` | string | Base64-encoded signature |
| `Algorithm` | string | Algorithm used (Ed25519) |
| `SignedPayload` | string | Base64-encoded payload |
| `SignerId` | string | Service identifier |
| `Timestamp` | DateTime | When signed |
| `PublicKey` | string | Public key PEM |

## Support

- **Documentation**: See [Resources/NUGET-PUBLISHING.md](../NUGET-PUBLISHING.md)
- **Examples**: This guide
- **Issues**: GitHub Issues
- **Logs**: Check service logs at `C:\ProgramData\StampService\Logs\`

---

**Happy Integrating! ??**
