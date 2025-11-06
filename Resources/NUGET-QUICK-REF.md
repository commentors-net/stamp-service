# NuGet Packages - Quick Reference

## Quick Start (3 Commands)

```powershell
# 1. Build packages
.\scripts\Build-NuGetPackages.ps1

# 2. Add local source
dotnet nuget add source D:\Jobs\workspace\DiG\Stamp-Service\nupkgs --name StampServiceLocal

# 3. Use in your project
dotnet add package StampService.ClientLib --source StampServiceLocal
```

## All Commands

### Building

```powershell
# Default build
.\scripts\Build-NuGetPackages.ps1

# With version
.\scripts\Build-NuGetPackages.ps1 -Version "1.2.3"

# With symbols
.\scripts\Build-NuGetPackages.ps1 -IncludeSymbols
```

### Publishing

```powershell
# Local folder
.\scripts\Publish-NuGetPackages.ps1 -Destination Local -Source ".\packages"

# Network share
.\scripts\Publish-NuGetPackages.ps1 -Destination NetworkShare -Source "\\server\share\nuget"

# Azure Artifacts
.\scripts\Publish-NuGetPackages.ps1 -Destination AzureArtifacts -Source "URL" -ApiKey "PAT"

# GitHub Packages
.\scripts\Publish-NuGetPackages.ps1 -Destination GitHub -Source "URL" -ApiKey "PAT"
```

### Using Packages

```csharp
// In your application
using StampService.ClientLib;
using StampService.Core.Models;

var client = new StampServiceClient();

var request = new SignRequest
{
    Operation = "your-operation",
    RequesterId = "YourApp",
    Payload = new Dictionary<string, object>
    {
        ["key"] = "value"
    }
};

var response = await client.SignAsync(request);
Console.WriteLine($"Signature: {response.Signature}");
```

## Distribution Options

| Method | Command | Best For |
|--------|---------|----------|
| **Local** | `-Destination Local -Source ".\packages"` | Single developer |
| **Network** | `-Destination NetworkShare -Source "\\server\share"` | Small teams |
| **Azure** | `-Destination AzureArtifacts -Source URL -ApiKey PAT` | Enterprise |
| **GitHub** | `-Destination GitHub -Source URL -ApiKey PAT` | GitHub users |

## Package Sources

```powershell
# Add source
dotnet nuget add source PATH_OR_URL --name NAME

# List sources
dotnet nuget list source

# Remove source
dotnet nuget remove source NAME
```

## Common Issues

### Service is running
```powershell
Stop-Service SecureStampService
.\scripts\Build-NuGetPackages.ps1
Start-Service SecureStampService
```

### Package not found
```powershell
dotnet nuget locals all --clear
dotnet restore
```

### Version conflict
```powershell
# Increment version
.\scripts\Build-NuGetPackages.ps1 -Version "1.0.1"
```

## Files Created

- `nupkgs/StampService.Core.1.0.0.nupkg` - Core package
- `nupkgs/StampService.ClientLib.1.0.0.nupkg` - Client library
- `nupkgs/PACKAGE-INFO.txt` - Usage information

## More Info

See `Resources/NUGET-PUBLISHING.md` for complete documentation.
