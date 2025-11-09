# Client Integration Guide - Using StampService

## Overview

This guide shows you how to integrate Secure Stamp Service into your applications using the `StampService.ClientLib` NuGet package.

## Prerequisites

### Service Side
- ? StampService installed and running on a machine
- ? Service accessible via Named Pipes (local machine)
- ? Backup shares created and stored securely

### Client Side
- ? .NET 8+ application
- ? Windows OS (for Named Pipes support)
- ? NuGet package source configured (if using NuGet)
- ? `StampService.ClientLib` package installed

## Installation

### Option 1: NuGet Package (Recommended)

#### Step 1: Configure NuGet Source

```powershell
# Local folder (for development/testing)
dotnet nuget add source D:\Path\To\nupkgs --name StampServiceLocal

# Network share (for team distribution)
dotnet nuget add source \\server\share\nuget --name CompanyPackages

# Azure Artifacts (for enterprise)
dotnet nuget add source https://pkgs.dev.azure.com/ORG/_packaging/FEED/nuget/v3/index.json --name AzureArtifacts

# GitHub Packages
dotnet nuget add source https://nuget.pkg.github.com/YOUR-ORG/index.json --name GitHubPackages
```

#### Step 2: Install Package

```powershell
# In your project directory
dotnet add package StampService.ClientLib --source StampServiceLocal

# Or specify version
dotnet add package StampService.ClientLib --version 1.0.0
```

#### Step 3: Verify Installation

```powershell
# List installed packages
dotnet list package

# Should show:
# StampService.ClientLib    1.0.0
# StampService.Core         1.0.0 (transitive dependency)
```

### Option 2: Direct DLL Reference

If not using NuGet, reference the DLL directly:

```xml
<!-- In your .csproj file -->
<ItemGroup>
  <Reference Include="StampService.ClientLib">
    <HintPath>C:\Program Files\StampService\StampService.ClientLib.dll</HintPath>
  </Reference>
  <Reference Include="StampService.Core">
    <HintPath>C:\Program Files\StampService\StampService.Core.dll</HintPath>
  </Reference>
</ItemGroup>
```

**Note**: The installer now copies AdminCLI and all necessary DLLs to `C:\Program Files\StampService\` for convenience.

## Basic Usage

### Simple Console Application

```csharp
using StampService.ClientLib;
using StampService.Core.Models;

class Program
{
    static async Task Main(string[] args)
    {
        // Create client (connects via Named Pipe)
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
        Console.WriteLine($"Algorithm: {response.Algorithm}");
        Console.WriteLine($"Timestamp: {response.Timestamp}");
     
     // Verify signature locally (optional)
        bool isValid = client.VerifySignature(response);
        Console.WriteLine($"Signature Valid: {isValid}");
    }
}
```

**Output:**
```
Service Running: True
Key Present: True
Signature: R3JlYXQgam9iIQ==...
Algorithm: Ed25519
Timestamp: 2025-01-07 14:30:45
Signature Valid: True
```

## Integration Patterns

### Pattern 1: ASP.NET Core Web API

#### Dependency Injection Setup

```csharp
// Program.cs
using StampService.ClientLib;

var builder = WebApplication.CreateBuilder(args);

// Register as singleton (reuses connection)
builder.Services.AddSingleton<StampServiceClient>();

// Or with custom configuration
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
  var pipeName = config.GetValue<string>("StampService:PipeName") ?? "StampServicePipe";
  var timeout = config.GetValue<int>("StampService:TimeoutMs", 5000);
    
    return new StampServiceClient(pipeName, timeout);
});

builder.Services.AddControllers();
var app = builder.Build();

app.MapControllers();
app.Run();
```

#### appsettings.json

```json
{
  "StampService": {
    "PipeName": "StampServicePipe",
    "TimeoutMs": 5000
  }
}
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
    private readonly StampServiceClient _stampClient;
    private readonly ILogger<SignatureController> _logger;
    
    public SignatureController(
   StampServiceClient stampClient,
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
  SignerId = response.SignerId,
      Algorithm = response.Algorithm
     });
  }
        catch (TimeoutException)
        {
    _logger.LogError("StampService request timed out");
 return StatusCode(504, "Service timeout");
     }
        catch (IOException ex)
  {
   _logger.LogError(ex, "Cannot connect to StampService");
    return StatusCode(503, "Service unavailable");
        }
 catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign request");
      return StatusCode(500, "Signing failed");
        }
    }
    
    [HttpGet("health")]
    public async Task<IActionResult> CheckHealth()
    {
        try
        {
     var status = await _stampClient.GetStatusAsync();
       
       if (!status.IsRunning || !status.KeyPresent)
          return StatusCode(503, "Service not ready");
      
  // Perform test stamp
            var testResponse = await _stampClient.TestStampAsync();
            var isValid = _stampClient.VerifySignature(testResponse);
            
return Ok(new
            {
        Status = "Healthy",
    ServiceRunning = status.IsRunning,
        KeyPresent = status.KeyPresent,
 Uptime = TimeSpan.FromSeconds(status.UptimeSeconds),
        TestSignatureValid = isValid
   });
        }
        catch (Exception ex)
        {
         _logger.LogError(ex, "Health check failed");
        return StatusCode(503, "Service unavailable");
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
    private readonly StampServiceClient _stampClient;
    private readonly ILogger<SigningWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);
 
  public SigningWorker(
  StampServiceClient stampClient,
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
          
         // Process your signing queue
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
    }
}

// Program.cs
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<StampServiceClient>();
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
    private readonly StampServiceClient _stampClient;
    
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
   StatusLabel.Content = status.IsRunning ? "? Connected" : "? Disconnected";
  UptimeLabel.Content = $"Uptime: {TimeSpan.FromSeconds(status.UptimeSeconds):hh\\:mm\\:ss}";
 }
     catch (Exception ex)
        {
            MessageBox.Show($"Failed to connect to service: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            TimestampLabel.Content = $"Signed: {response.Timestamp:yyyy-MM-dd HH:mm:ss}";
            
       MessageBox.Show("Document signed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }
        catch (TimeoutException)
        {
            MessageBox.Show("Service request timed out. Please try again.", "Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (IOException)
        {
            MessageBox.Show("Cannot connect to StampService. Is the service running?", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
     {
            MessageBox.Show($"Signing failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
builder.Services.AddSingleton<StampServiceClient>();

var app = builder.Build();

// Sign endpoint
app.MapPost("/api/sign", async (SignRequest request, StampServiceClient client) =>
{
 try
    {
        var response = await client.SignAsync(request);
   return Results.Ok(response);
    }
    catch (TimeoutException)
    {
     return Results.Problem("Service timeout", statusCode: 504);
    }
    catch (IOException)
    {
        return Results.Problem("Service unavailable", statusCode: 503);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

// Health check endpoint
app.MapGet("/api/health", async (StampServiceClient client) =>
{
    try
    {
var status = await client.GetStatusAsync();
var testResponse = await client.TestStampAsync();
        var isValid = client.VerifySignature(testResponse);
        
    return Results.Ok(new
        {
            Healthy = status.IsRunning && status.KeyPresent && isValid,
          Status = status
     });
    }
 catch
{
        return Results.Problem("Service health check failed", statusCode: 503);
    }
});

app.Run();
```

## Advanced Usage

### Custom Configuration

```csharp
// Custom pipe name and timeout
var client = new StampServiceClient(
    pipeName: "CustomPipeName",  // Must match service configuration
    timeoutMs: 10000  // 10 seconds timeout
);

// appsettings.json
{
  "StampService": {
    "PipeName": "StampServicePipe",
    "TimeoutMs": 10000
  }
}
```

### Retry Logic with Polly

```csharp
using Polly;
using Polly.Retry;

public class ResilientStampServiceClient
{
    private readonly StampServiceClient _client;
    private readonly AsyncRetryPolicy _retryPolicy;
    
    public ResilientStampServiceClient()
    {
  _client = new StampServiceClient();
   
        _retryPolicy = Policy
         .Handle<TimeoutException>()
            .Or<IOException>()
     .WaitAndRetryAsync(
  retryCount: 3,
           sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
 onRetry: (exception, timeSpan, retryCount, context) =>
      {
              Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
          }
         );
    }
    
    public Task<SignedResponse> SignAsync(SignRequest request)
    {
 return _retryPolicy.ExecuteAsync(() => _client.SignAsync(request));
  }
}
```

### Circuit Breaker Pattern

```csharp
using Polly;
using Polly.CircuitBreaker;

public class CircuitBreakerStampServiceClient
{
    private readonly StampServiceClient _client;
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
    
    public CircuitBreakerStampServiceClient()
    {
   _client = new StampServiceClient();
 
  _circuitBreakerPolicy = Policy
.Handle<Exception>()
            .CircuitBreakerAsync(
 handledEventsAllowedBeforeBreaking: 3,
           durationOfBreak: TimeSpan.FromSeconds(30),
    onBreak: (exception, duration) =>
    {
          Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s");
                },
       onReset: () =>
          {
Console.WriteLine("Circuit breaker reset");
          }
     );
    }
    
    public Task<SignedResponse> SignAsync(SignRequest request)
    {
     return _circuitBreakerPolicy.ExecuteAsync(() => _client.SignAsync(request));
    }
}
```

## Error Handling

### Comprehensive Error Handling

```csharp
using System.IO.Pipes;

try
{
    using var client = new StampServiceClient();
    var response = await client.SignAsync(request);
    // Process response
}
catch (TimeoutException)
{
    // Service didn't respond in time
    // Possible causes: Service overloaded, network issues
    logger.LogWarning("StampService request timed out");
    // Consider: Retry, use cached data, or alert user
}
catch (IOException ex)
{
    // Named Pipe connection failed
    // Possible causes: Service not running, wrong pipe name
    logger.LogError(ex, "Cannot connect to StampService");
    // Consider: Check if service is running, verify configuration
}
catch (InvalidOperationException ex)
{
    // Service returned an error
    // Possible causes: Invalid request, service in error state
    logger.LogError(ex, "Service error: {Message}", ex.Message);
    // Consider: Validate request, check service logs
}
catch (Exception ex)
{
    // Unexpected error
    logger.LogError(ex, "Unexpected error calling StampService");
    throw;
}
```

### Production-Ready Error Handler

```csharp
public class StampServiceErrorHandler
{
    private readonly ILogger<StampServiceErrorHandler> _logger;
    
    public async Task<Result<SignedResponse>> TrySignAsync(
        StampServiceClient client,
        SignRequest request)
    {
    try
        {
          var response = await client.SignAsync(request);
  return Result<SignedResponse>.Success(response);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Service timeout for operation {Operation}", request.Operation);
      return Result<SignedResponse>.Failure("Service timeout. Please try again.");
      }
        catch (IOException ex)
        {
     _logger.LogError(ex, "Cannot connect to service");
    return Result<SignedResponse>.Failure("Service unavailable. Please contact support.");
        }
        catch (Exception ex)
   {
     _logger.LogError(ex, "Unexpected error signing request");
          return Result<SignedResponse>.Failure("An error occurred. Please try again.");
        }
    }
}

public class Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    
    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}
```

## Health Checks

### ASP.NET Core Health Check

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class StampServiceHealthCheck : IHealthCheck
{
    private readonly StampServiceClient _client;
    private readonly ILogger<StampServiceHealthCheck> _logger;
    
    public StampServiceHealthCheck(StampServiceClient client, ILogger<StampServiceHealthCheck> logger)
    {
   _client = client;
     _logger = logger;
    }
    
  public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
  {
    // Check service status
            var status = await _client.GetStatusAsync();
     
    if (!status.IsRunning)
            {
         return HealthCheckResult.Unhealthy("Service not running");
    }
        
      if (!status.KeyPresent)
            {
  return HealthCheckResult.Degraded("Key not present - service in keyless state");
            }
            
            // Perform test stamp to verify cryptographic operations
            var testResponse = await _client.TestStampAsync();
    var isValid = _client.VerifySignature(testResponse);
            
        if (!isValid)
            {
 return HealthCheckResult.Unhealthy("Signature verification failed");
            }
     
      return HealthCheckResult.Healthy("Service operational", new Dictionary<string, object>
     {
              ["Uptime"] = TimeSpan.FromSeconds(status.UptimeSeconds).ToString(),
  ["Algorithm"] = status.Algorithm ?? "Unknown",
      ["LastHealthCheck"] = status.LastHealthCheck
            });
        }
  catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Health check timeout");
    return HealthCheckResult.Degraded("Service responding slowly", ex);
  }
        catch (IOException ex)
        {
        _logger.LogError(ex, "Cannot connect to service");
         return HealthCheckResult.Unhealthy("Cannot connect to service", ex);
        }
        catch (Exception ex)
  {
            _logger.LogError(ex, "Health check failed");
  return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }
}

// Program.cs
builder.Services.AddSingleton<StampServiceClient>();
builder.Services.AddHealthChecks()
 .AddCheck<StampServiceHealthCheck>("stamp-service", tags: new[] { "ready", "live" });

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
```

## Testing

### Unit Testing with Mock

First, create an interface for the client:

```csharp
public interface IStampServiceClient : IDisposable
{
    Task<SignedResponse> SignAsync(SignRequest request);
    Task<SignedResponse> TestStampAsync();
    Task<ServiceStatus> GetStatusAsync();
    bool VerifySignature(SignedResponse signedResponse);
}

// Adapter class
public class StampServiceClientAdapter : IStampServiceClient
{
    private readonly StampServiceClient _client;
 
    public StampServiceClientAdapter(StampServiceClient client)
    {
        _client = client;
    }
    
    public Task<SignedResponse> SignAsync(SignRequest request) => _client.SignAsync(request);
    public Task<SignedResponse> TestStampAsync() => _client.TestStampAsync();
    public Task<ServiceStatus> GetStatusAsync() => _client.GetStatusAsync();
    public bool VerifySignature(SignedResponse signedResponse) => _client.VerifySignature(signedResponse);
    public void Dispose() => _client.Dispose();
}
```

Then mock it in tests:

```csharp
using Moq;
using Xunit;

public class DocumentServiceTests
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
     Signature = "test-signature-base64",
         Algorithm = "Ed25519",
     Timestamp = DateTime.UtcNow,
          SignerId = "StampService-v1"
   });
        
        var service = new DocumentService(mockClient.Object);
        
  // Act
  var result = await service.SignDocumentAsync("doc-123", "Test content");
        
      // Assert
        Assert.NotNull(result);
     Assert.Equal("test-signature-base64", result.Signature);
   Assert.Equal("Ed25519", result.Algorithm);
    }
    
    [Fact]
    public async Task SignDocument_WhenServiceUnavailable_ThrowsException()
    {
        // Arrange
        var mockClient = new Mock<IStampServiceClient>();
   mockClient
            .Setup(x => x.SignAsync(It.IsAny<SignRequest>()))
        .ThrowsAsync(new IOException("Service unavailable"));
        
        var service = new DocumentService(mockClient.Object);
        
    // Act & Assert
   await Assert.ThrowsAsync<IOException>(() =>
       service.SignDocumentAsync("doc-123", "Test content"));
    }
}
```

## Best Practices

### 1. Use Dependency Injection ?

```csharp
// Good: Register as singleton
builder.Services.AddSingleton<StampServiceClient>();

// Bad: Creating new instances repeatedly
for (int i = 0; i < 1000; i++)
{
    var client = new StampServiceClient(); // ? Don't do this!
    await client.SignAsync(request);
}
```

### 2. Handle All Error Types ?

```csharp
try
{
    var response = await client.SignAsync(request);
}
catch (TimeoutException)
{
  // Handle timeout - maybe retry
}
catch (IOException)
{
    // Handle connection failure - check service status
}
catch (InvalidOperationException ex)
{
    // Handle service error - log and alert
}
```

### 3. Implement Health Checks ?

```csharp
// Periodic health check
public async Task<bool> IsServiceHealthyAsync()
{
    try
    {
        var status = await _client.GetStatusAsync();
        if (!status.IsRunning || !status.KeyPresent)
        return false;
        
 var testResponse = await _client.TestStampAsync();
        return _client.VerifySignature(testResponse);
    }
    catch
    {
        return false;
    }
}
```

### 4. Use Retry Logic ?

```csharp
// Simple retry
async Task<SignedResponse?> SignWithRetryAsync(SignRequest request, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
       return await _client.SignAsync(request);
        }
        catch (TimeoutException) when (i < maxRetries - 1)
      {
        await Task.Delay(TimeSpan.FromSeconds(2 * (i + 1))); // Exponential backoff
      }
    }
    return null;
}
```

### 5. Monitor Performance ?

```csharp
using System.Diagnostics;

var stopwatch = Stopwatch.StartNew();
var response = await client.SignAsync(request);
stopwatch.Stop();

if (stopwatch.ElapsedMilliseconds > 1000)
{
  _logger.LogWarning("Slow signing operation: {Duration}ms", stopwatch.ElapsedMilliseconds);
}

_telemetry.TrackMetric("StampService.SignDuration", stopwatch.ElapsedMilliseconds);
```

### 6. Cache Public Key (Optional) ?

```csharp
// Cache public key to reduce calls
private string? _cachedPublicKey;
private DateTime _cacheExpiry;

async Task<string> GetPublicKeyAsync()
{
    if (_cachedPublicKey != null && DateTime.UtcNow < _cacheExpiry)
        return _cachedPublicKey;
    
    var status = await _client.GetStatusAsync();
    _cachedPublicKey = status.PublicKey;
    _cacheExpiry = DateTime.UtcNow.AddHours(24); // Cache for 24 hours
    
    return _cachedPublicKey!;
}
```

## Troubleshooting

### Issue: Cannot Connect to Service

**Symptoms:**
- `IOException`: "All pipe instances are busy"
- `TimeoutException`: "The operation was canceled"

**Solutions:**
```powershell
# 1. Verify service is running
Get-Service SecureStampService
# Should show: Status = Running

# 2. Test with AdminCLI
cd "C:\Program Files\StampService"
.\StampService.AdminCLI.exe status

# 3. Check service logs
Get-Content "C:\ProgramData\StampService\Logs\service*.log" -Tail 50

# 4. Verify pipe name matches
# Service: appsettings.json -> PipeName
# Client: new StampServiceClient("PipeName")
```

**Code fix:**
```csharp
// Increase timeout
var client = new StampServiceClient(timeoutMs: 30000); // 30 seconds

// Or check service first
var psi = new ProcessStartInfo
{
    FileName = "sc.exe",
    Arguments = "query SecureStampService",
RedirectStandardOutput = true,
    UseShellExecute = false
};
var process = Process.Start(psi);
var output = await process!.StandardOutput.ReadToEndAsync();
Console.WriteLine(output);
```

### Issue: "ReadMode is not of PipeTransmissionMode.Message"

**This was a bug in StampService.ClientLib version < 1.0.0**

**Solution:**
```powershell
# Update to version 1.0.0 or later
dotnet add package StampService.ClientLib --version 1.0.0

# Or update package
dotnet update package StampService.ClientLib
```

**Fixed in v1.0.0:**
The client now correctly sets `ReadMode = PipeTransmissionMode.Message` to match the server.

### Issue: Intermittent Timeout Errors

**Solutions:**
```csharp
// 1. Increase timeout
var client = new StampServiceClient(timeoutMs: 15000);

// 2. Implement retry with exponential backoff
public async Task<SignedResponse> SignWithBackoffAsync(SignRequest request)
{
    int maxAttempts = 3;
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
  {
         return await _client.SignAsync(request);
        }
        catch (TimeoutException) when (attempt < maxAttempts)
        {
            var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
    await Task.Delay(delay);
        }
    }
throw new TimeoutException("All retry attempts failed");
}
```

## API Reference

### StampServiceClient Class

#### Constructor

```csharp
public StampServiceClient(string pipeName = "StampServicePipe", int timeoutMs = 5000)
```

**Parameters:**
- `pipeName`: Named pipe name (must match service configuration)
- `timeoutMs`: Timeout in milliseconds (default: 5000)

#### Methods

| Method | Description | Returns | Throws |
|--------|-------------|---------|--------|
| `SignAsync(SignRequest)` | Sign a request | `Task<SignedResponse>` | `TimeoutException`, `IOException`, `InvalidOperationException` |
| `TestStampAsync()` | Health check signature | `Task<SignedResponse>` | `TimeoutException`, `IOException` |
| `GetStatusAsync()` | Get service status | `Task<ServiceStatus>` | `TimeoutException`, `IOException` |
| `VerifySignature(SignedResponse)` | Verify signature locally | `bool` | - |
| `Dispose()` | Clean up resources | `void` | - |

### SignRequest Model

```csharp
public class SignRequest
{
  public string Operation { get; set; }          // Required: Operation type
    public string RequesterId { get; set; }        // Required: Who is requesting
    public Dictionary<string, object> Payload { get; set; }  // Required: Data to sign
}
```

**Example:**
```csharp
var request = new SignRequest
{
    Operation = "mint",
    RequesterId = "MyApp",
    Payload = new Dictionary<string, object>
    {
        ["recipient"] = "user123",
        ["amount"] = 1000,
 ["timestamp"] = DateTime.UtcNow
    }
};
```

### SignedResponse Model

```csharp
public class SignedResponse
{
    public string Signature { get; set; }      // Base64-encoded signature
 public string Algorithm { get; set; }      // Algorithm used (Ed25519)
    public string SignedPayload { get; set; }  // Base64-encoded payload
    public string SignerId { get; set; }       // Service identifier
 public DateTime Timestamp { get; set; }    // When signed (UTC)
    public string? PublicKey { get; set; }     // Public key PEM (optional)
}
```

### ServiceStatus Model

```csharp
public class ServiceStatus
{
    public bool IsRunning { get; set; }    // Service running
    public bool KeyPresent { get; set; }          // Key exists
    public long UptimeSeconds { get; set; }  // Uptime in seconds
    public DateTime LastHealthCheck { get; set; } // Last health check
  public string? Algorithm { get; set; }        // Algorithm name
    public string? PublicKey { get; set; }        // Public key PEM
}
```

## Performance Considerations

### Typical Performance

- **Signature Generation**: ~1-2ms (Ed25519)
- **IPC Overhead**: ~0.1ms (Named Pipes)
- **Total Latency**: ~2-5ms (local machine)
- **Throughput**: ~500-1000 signatures/sec (single-threaded)

### Optimization Tips

```csharp
// 1. Reuse client instance (singleton pattern)
services.AddSingleton<StampServiceClient>();

// 2. Batch requests if possible (application-level batching)
var tasks = requests.Select(r => client.SignAsync(r));
var responses = await Task.WhenAll(tasks);

// 3. Use async/await properly (don't block)
var response = await client.SignAsync(request); // ? Good
var response = client.SignAsync(request).Result; // ? Bad (blocks thread)

// 4. Monitor and alert on slow operations
if (stopwatch.ElapsedMilliseconds > 100)
{
    _logger.LogWarning("Slow signature: {Duration}ms", stopwatch.ElapsedMilliseconds);
}
```

## Support

### Documentation
- **Distribution Guide**: [DISTRIBUTION.md](DISTRIBUTION.md)
- **Quick Start**: [QUICKSTART.md](QUICKSTART.md)
- **NuGet Publishing**: [NUGET-PUBLISHING.md](NUGET-PUBLISHING.md)

### Code Examples
- See `examples/CLIENT-EXAMPLES.md` for more examples
- Check this guide for integration patterns

### Troubleshooting
- **Service Logs**: `C:\ProgramData\StampService\Logs\service*.log`
- **Audit Logs**: `C:\ProgramData\StampService\Logs\audit*.log`
- **AdminCLI**: `C:\Program Files\StampService\StampService.AdminCLI.exe status`

### Getting Help
- Check service status: `Get-Service SecureStampService`
- Test service: `cd "C:\Program Files\StampService"; .\StampService.AdminCLI.exe test-stamp`
- Review logs for errors
- Contact your system administrator

---

**?? Happy Integrating!**

For detailed installation and configuration, see [DISTRIBUTION.md](DISTRIBUTION.md).
