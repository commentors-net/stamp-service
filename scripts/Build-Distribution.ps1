# Build and Package Script for StampService
# This script builds the solution and creates a distribution package

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ".\dist"
)

$ErrorActionPreference = "Stop"

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "     Secure Stamp Service - Build Script      " -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean
Write-Host "[1/5] Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path $OutputPath) {
    Remove-Item -Path $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

dotnet clean -c $Configuration
Write-Host "  Cleaned" -ForegroundColor Green

# Step 2: Restore
Write-Host ""
Write-Host "[2/5] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
Write-Host "  Restored" -ForegroundColor Green

# Step 3: Build
Write-Host ""
Write-Host "[3/5] Building solution..." -ForegroundColor Yellow
dotnet build -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "  Build successful" -ForegroundColor Green

# Step 4: Run tests
Write-Host ""
Write-Host "[4/5] Running tests..." -ForegroundColor Yellow
dotnet test -c $Configuration --no-build --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "WARNING: Some tests failed" -ForegroundColor Yellow
    $Continue = Read-Host "Continue with packaging? (y/n)"
    if ($Continue -ne 'y') {
        exit 1
    }
} else {
    Write-Host "  All tests passed" -ForegroundColor Green
}

# Step 5: Publish
Write-Host ""
Write-Host "[5/5] Publishing projects..." -ForegroundColor Yellow

# Publish StampService
Write-Host "  Publishing StampService..." -ForegroundColor Cyan
$ServicePublishPath = Join-Path $OutputPath "StampService"
dotnet publish .\src\StampService\StampService.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -o $ServicePublishPath `
    /p:PublishSingleFile=false
Write-Host "    Done" -ForegroundColor Green

# Publish AdminCLI
Write-Host "  Publishing AdminCLI..." -ForegroundColor Cyan
$AdminPublishPath = Join-Path $OutputPath "AdminCLI"
dotnet publish .\src\StampService.AdminCLI\StampService.AdminCLI.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -o $AdminPublishPath `
    /p:PublishSingleFile=false
Write-Host "    Done" -ForegroundColor Green

# Copy scripts
Write-Host "  Copying installation scripts..." -ForegroundColor Cyan
$ScriptsPath = Join-Path $OutputPath "Scripts"
New-Item -ItemType Directory -Path $ScriptsPath -Force | Out-Null
Copy-Item -Path ".\scripts\*.ps1" -Destination $ScriptsPath -Force
Write-Host "    Done" -ForegroundColor Green

# Copy documentation
Write-Host "  Copying documentation..." -ForegroundColor Cyan
Copy-Item -Path ".\README.md" -Destination $OutputPath -Force
Write-Host "    Done" -ForegroundColor Green

# Create distribution README
$DistReadme = @"
# Secure Stamp Service - Distribution Package

## Contents

- **StampService/** - Windows Service executable and dependencies
- **AdminCLI/** - Administration command-line tool
- **Scripts/** - Installation and uninstallation PowerShell scripts
- **README.md** - Full documentation

## Quick Start

### Installation

1. Open PowerShell as Administrator
2. Navigate to the Scripts directory
3. Run: ``.\Install-StampService.ps1``

The installer will:
- Copy files to C:\Program Files\StampService
- Create data directory at C:\ProgramData\StampService
- Install and start the Windows Service

### Verify Installation

```powershell
# Check service status
Get-Service SecureStampService

# Test with AdminCLI
cd ..\AdminCLI
.\StampService.AdminCLI.exe status
.\StampService.AdminCLI.exe test-stamp
```

### Create Backup Shares

```powershell
cd ..\AdminCLI
.\StampService.AdminCLI.exe create-shares --total 5 --threshold 3 --output C:\Shares
```

Distribute the shares to trusted custodians for safekeeping.

### Uninstallation

```powershell
cd ..\Scripts
.\Uninstall-StampService.ps1
# Add -RemoveData to also delete all service data
```

## Client Integration

To integrate with your applications, add a reference to:
- **StampService.ClientLib.dll** (in StampService directory)

Example usage:

```csharp
using StampService.ClientLib;
using StampService.Core.Models;

var client = new StampServiceClient();

var request = new SignRequest
{
    Operation = "mint",
    RequesterId = "MyApp",
    Payload = new Dictionary<string, object>
    {
        ["recipient"] = "user123",
        ["amount"] = 1000
    }
};

var response = await client.SignAsync(request);
Console.WriteLine($"Signature: {response.Signature}");
```

## Security Notes

- The service runs under LocalSystem by default
- Master key is encrypted with Windows DPAPI
- All operations are logged to C:\ProgramData\StampService\Logs\
- Backup shares should be stored offline in secure locations
- Require threshold shares (e.g., 3 of 5) for key recovery

## Support

For issues and questions, refer to the main README.md file.

## Version

Build Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Configuration: $Configuration

"@

$DistReadme | Out-File -FilePath (Join-Path $OutputPath "DISTRIBUTION-README.txt") -Encoding UTF8
Write-Host "    Done" -ForegroundColor Green

# Create version info
$VersionInfo = @{
    BuildDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Configuration = $Configuration
    GitCommit = (git rev-parse --short HEAD 2>$null)
    GitBranch = (git rev-parse --abbrev-ref HEAD 2>$null)
}
$VersionInfo | ConvertTo-Json | Out-File -FilePath (Join-Path $OutputPath "version.json")

# Create ZIP package
Write-Host ""
Write-Host "Creating distribution package..." -ForegroundColor Yellow
$ZipPath = "StampService-Distribution-$(Get-Date -Format 'yyyyMMdd-HHmmss').zip"
Compress-Archive -Path "$OutputPath\*" -DestinationPath $ZipPath -Force
Write-Host "  Package created: $ZipPath" -ForegroundColor Green

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "            Build Complete!                    " -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output directory: $OutputPath" -ForegroundColor White
Write-Host "Distribution package: $ZipPath" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Test the installation on a clean machine" -ForegroundColor White
Write-Host "  2. Distribute the ZIP file to users" -ForegroundColor White
Write-Host "  3. Users should extract and run Scripts\Install-StampService.ps1" -ForegroundColor White
Write-Host ""
