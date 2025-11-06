# Build and Package NuGet Packages
# This script builds NuGet packages for private distribution

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ".\nupkgs",
    
    [Parameter(Mandatory=$false)]
    [string]$Version = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$IncludeSymbols,
 
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "   Secure Stamp Service - NuGet Packager     " -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Determine version
if ([string]::IsNullOrEmpty($Version)) {
    # Try to get version from git tag
try {
        $gitTag = git describe --tags --abbrev=0 2>&1
        if ($LASTEXITCODE -eq 0 -and $gitTag -match "^v?(\d+\.\d+\.\d+)") {
      $Version = $Matches[1]
       Write-Host "Using version from git tag: $Version" -ForegroundColor Green
        } else {
            throw "No valid git tag found"
        }
    } catch {
        $Version = "1.0.0"
        Write-Host "No git tag found, using default version: $Version" -ForegroundColor Yellow
    Write-Host "  (Create a tag with: git tag v1.0.0)" -ForegroundColor DarkGray
    }
} else {
    Write-Host "Using specified version: $Version" -ForegroundColor Green
}

# Create output directory
Write-Host ""
Write-Host "[1/4] Preparing output directory..." -ForegroundColor Yellow
if (Test-Path $OutputPath) {
    Remove-Item -Path $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
Write-Host "  Output: $OutputPath" -ForegroundColor Green

# Build solution
if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "[2/4] Building solution..." -ForegroundColor Yellow
  
    # Stop service if running to avoid file locks
    $serviceName = "SecureStampService"
    $service = Get-Service $serviceName -ErrorAction SilentlyContinue
    if ($service -and $service.Status -eq 'Running') {
        Write-Host "  Stopping $serviceName..." -ForegroundColor Yellow
        Stop-Service $serviceName
        Start-Sleep -Seconds 2
}
    
    dotnet clean -c $Configuration
    dotnet restore
    dotnet build -c $Configuration --no-restore /p:Version=$Version
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "  Build successful" -ForegroundColor Green
    
    # Restart service if it was running
    if ($service -and $service.Status -eq 'Stopped') {
        Write-Host "  Starting $serviceName..." -ForegroundColor Yellow
Start-Service $serviceName
    }
} else {
    Write-Host ""
    Write-Host "[2/4] Skipping build (using existing binaries)" -ForegroundColor Yellow
}

# Pack projects
Write-Host ""
Write-Host "[3/4] Creating NuGet packages..." -ForegroundColor Yellow

$projects = @(
    "src\StampService.Core\StampService.Core.csproj",
    "src\StampService.ClientLib\StampService.ClientLib.csproj"
)

$packArgs = @(
    "-c", $Configuration,
    "--no-build",
    "-o", $OutputPath,
    "/p:Version=$Version"
)

if ($IncludeSymbols) {
    $packArgs += @("--include-symbols", "-p:SymbolPackageFormat=snupkg")
}

foreach ($project in $projects) {
    Write-Host "  Packing $project..." -ForegroundColor Cyan
    dotnet pack $project @packArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to pack $project" -ForegroundColor Red
    exit 1
    }
}

Write-Host "  Packages created successfully" -ForegroundColor Green

# List created packages
Write-Host ""
Write-Host "[4/4] Package summary..." -ForegroundColor Yellow

$packages = Get-ChildItem -Path $OutputPath -Filter "*.nupkg"
$totalSize = 0

Write-Host ""
Write-Host "Created packages:" -ForegroundColor White
foreach ($pkg in $packages) {
    $size = [math]::Round($pkg.Length / 1KB, 2)
    $totalSize += $size
    Write-Host "  - $($pkg.Name) ($size KB)" -ForegroundColor White
}
Write-Host ""
Write-Host "Total: $($packages.Count) packages, $([math]::Round($totalSize, 2)) KB" -ForegroundColor White

# Create package info file
$packageInfo = @"
===============================================
  Secure Stamp Service - NuGet Packages
===============================================

Build Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Version: $Version
Configuration: $Configuration

PACKAGES CREATED:

"@

foreach ($pkg in $packages) {
    $size = [math]::Round($pkg.Length / 1KB, 2)
    $packageInfo += "  $($pkg.Name) ($size KB)`n"
}

$packageInfo += @"

===============================================
USAGE OPTIONS:
===============================================

OPTION 1: Local Folder (Simplest for private use)
  
  1. Copy packages to a local or network folder
  2. Add the folder as a NuGet source:
     
     dotnet nuget add source C:\MyNuGetPackages --name LocalPackages
     
  3. Install in your project:
  
     dotnet add package StampService.ClientLib --version $Version

OPTION 2: Network Share (For team use)

  1. Copy packages to network share (e.g., \\server\share\nuget)
  2. Add as source:
  
     dotnet nuget add source \\server\share\nuget --name CompanyPackages
     
  3. Install normally

OPTION 3: Azure Artifacts (For enterprise use)

  1. Create feed in Azure DevOps
  2. Push packages:
     
     dotnet nuget push StampService.Core.$Version.nupkg \
       --source "AzureArtifacts" \
       --api-key az
       
  3. Configure authentication in NuGet.config
  4. Install normally

OPTION 4: GitHub Packages (For GitHub users)

  1. Configure GitHub PAT in NuGet.config
  2. Push packages:
  
     dotnet nuget push StampService.Core.$Version.nupkg \
       --source "https://nuget.pkg.github.com/YOUR-ORG/index.json" \
       --api-key YOUR-GITHUB-PAT
       
  3. Install normally

===============================================
QUICK START (LOCAL USE):
===============================================

1. Add local NuGet source:

   dotnet nuget add source $((Get-Item $OutputPath).FullName) --name StampServiceLocal

2. In your client project:

   dotnet add package StampService.ClientLib --version $Version --source StampServiceLocal

3. Start coding:

   using StampService.ClientLib;
   
   var client = new StampServiceClient();
   var response = await client.SignAsync(request);

===============================================

See NUGET-PUBLISHING.md for detailed instructions on all options.

"@

$packageInfo | Out-File -FilePath (Join-Path $OutputPath "PACKAGE-INFO.txt") -Encoding UTF8

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "Packaging Complete!                " -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Packages location: $OutputPath" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Review PACKAGE-INFO.txt for usage options" -ForegroundColor White
Write-Host "  2. Choose distribution method (local/network/Azure/GitHub)" -ForegroundColor White
Write-Host "  3. Add packages to your client projects" -ForegroundColor White
Write-Host ""
Write-Host "Quick start (local use):" -ForegroundColor Yellow
Write-Host "  dotnet nuget add source $((Get-Item $OutputPath).FullName) --name StampServiceLocal" -ForegroundColor White
Write-Host "  dotnet add package StampService.ClientLib --version $Version --source StampServiceLocal" -ForegroundColor White
Write-Host ""

# Open output folder
Start-Sleep -Seconds 1
if (Test-Path $OutputPath) {
    explorer.exe $OutputPath
}
