# Publish NuGet Packages
# This script publishes packages to various destinations

param(
    [Parameter(Mandatory=$false)]
    [string]$PackagesPath = ".\nupkgs",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet('Local', 'NetworkShare', 'AzureArtifacts', 'GitHub', 'NuGetOrg')]
 [string]$Destination = 'Local',
    
    [Parameter(Mandatory=$false)]
    [string]$Source = "",
    
    [Parameter(Mandatory=$false)]
    [string]$ApiKey = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipSymbols
)

$ErrorActionPreference = "Stop"

Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "   Secure Stamp Service - NuGet Publisher    " -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Verify packages exist
if (-not (Test-Path $PackagesPath)) {
    Write-Host "ERROR: Packages path not found: $PackagesPath" -ForegroundColor Red
    Write-Host "Run Build-NuGetPackages.ps1 first" -ForegroundColor Yellow
    exit 1
}

$packages = Get-ChildItem -Path $PackagesPath -Filter "*.nupkg" | Where-Object { $_.Name -notlike "*.symbols.nupkg" }

if ($packages.Count -eq 0) {
    Write-Host "ERROR: No packages found in $PackagesPath" -ForegroundColor Red
    exit 1
}

Write-Host "Found $($packages.Count) package(s) to publish:" -ForegroundColor Green
foreach ($pkg in $packages) {
    Write-Host "  - $($pkg.Name)" -ForegroundColor White
}
Write-Host ""

# Publish based on destination
switch ($Destination) {
    'Local' {
        Write-Host "Publishing to local folder..." -ForegroundColor Yellow
  
        if ([string]::IsNullOrEmpty($Source)) {
    $Source = ".\packages"
        }
        
        # Create local folder if it doesn't exist
        if (-not (Test-Path $Source)) {
      New-Item -ItemType Directory -Path $Source -Force | Out-Null
        }
        
  # Copy packages
        foreach ($pkg in $packages) {
         Write-Host "  Copying $($pkg.Name)..." -ForegroundColor Cyan
            Copy-Item -Path $pkg.FullName -Destination $Source -Force
     
          # Copy symbols if present
            $symbolPkg = $pkg.FullName -replace '\.nupkg$', '.snupkg'
            if ((Test-Path $symbolPkg) -and -not $SkipSymbols) {
      Copy-Item -Path $symbolPkg -Destination $Source -Force
 }
        }
        
        Write-Host ""
        Write-Host "? Packages published to: $Source" -ForegroundColor Green
    Write-Host ""
        Write-Host "To use these packages:" -ForegroundColor Yellow
        Write-Host "  dotnet nuget add source $((Get-Item $Source).FullName) --name StampServiceLocal" -ForegroundColor White
        Write-Host "  dotnet add package StampService.ClientLib --source StampServiceLocal" -ForegroundColor White
    }
    
    'NetworkShare' {
     Write-Host "Publishing to network share..." -ForegroundColor Yellow
        
     if ([string]::IsNullOrEmpty($Source)) {
          Write-Host "ERROR: Network share path required" -ForegroundColor Red
            Write-Host "Example: .\Publish-NuGetPackages.ps1 -Destination NetworkShare -Source '\\server\share\nuget'" -ForegroundColor Yellow
            exit 1
    }
        
        # Verify network path is accessible
        if (-not (Test-Path $Source)) {
          Write-Host "ERROR: Network share not accessible: $Source" -ForegroundColor Red
            exit 1
  }
     
        # Copy packages
        foreach ($pkg in $packages) {
            Write-Host "  Copying $($pkg.Name)..." -ForegroundColor Cyan
  Copy-Item -Path $pkg.FullName -Destination $Source -Force
            
            # Copy symbols if present
        $symbolPkg = $pkg.FullName -replace '\.nupkg$', '.snupkg'
  if ((Test-Path $symbolPkg) -and -not $SkipSymbols) {
  Copy-Item -Path $symbolPkg -Destination $Source -Force
            }
}
    
   Write-Host ""
  Write-Host "? Packages published to: $Source" -ForegroundColor Green
        Write-Host ""
        Write-Host "Team members can add this source:" -ForegroundColor Yellow
        Write-Host "  dotnet nuget add source $Source --name CompanyPackages" -ForegroundColor White
    }
    
    'AzureArtifacts' {
        Write-Host "Publishing to Azure Artifacts..." -ForegroundColor Yellow
  
  if ([string]::IsNullOrEmpty($Source)) {
            Write-Host "ERROR: Azure Artifacts feed URL required" -ForegroundColor Red
            Write-Host "Example: -Source 'https://pkgs.dev.azure.com/YOUR-ORG/_packaging/YOUR-FEED/nuget/v3/index.json'" -ForegroundColor Yellow
            exit 1
        }

      if ([string]::IsNullOrEmpty($ApiKey)) {
            Write-Host "ERROR: API key (PAT token) required for Azure Artifacts" -ForegroundColor Red
      Write-Host "Example: -ApiKey 'YOUR-PERSONAL-ACCESS-TOKEN'" -ForegroundColor Yellow
            exit 1
   }
     
        # Push packages
        foreach ($pkg in $packages) {
            Write-Host "  Pushing $($pkg.Name)..." -ForegroundColor Cyan
   dotnet nuget push $pkg.FullName --source $Source --api-key $ApiKey
            
    if ($LASTEXITCODE -ne 0) {
           Write-Host "ERROR: Failed to push $($pkg.Name)" -ForegroundColor Red
           exit 1
         }
        }
        
        Write-Host ""
        Write-Host "? Packages published to Azure Artifacts" -ForegroundColor Green
    }
 
  'GitHub' {
        Write-Host "Publishing to GitHub Packages..." -ForegroundColor Yellow
        
        if ([string]::IsNullOrEmpty($Source)) {
    Write-Host "ERROR: GitHub Packages URL required" -ForegroundColor Red
            Write-Host "Example: -Source 'https://nuget.pkg.github.com/YOUR-ORG/index.json'" -ForegroundColor Yellow
            exit 1
      }
        
        if ([string]::IsNullOrEmpty($ApiKey)) {
     Write-Host "ERROR: GitHub PAT token required" -ForegroundColor Red
       Write-Host "Create PAT at: https://github.com/settings/tokens" -ForegroundColor Yellow
      Write-Host "Required scopes: write:packages, read:packages" -ForegroundColor Yellow
  exit 1
        }
        
        # Push packages
        foreach ($pkg in $packages) {
    Write-Host "  Pushing $($pkg.Name)..." -ForegroundColor Cyan
            dotnet nuget push $pkg.FullName --source $Source --api-key $ApiKey
            
      if ($LASTEXITCODE -ne 0) {
       Write-Host "ERROR: Failed to push $($pkg.Name)" -ForegroundColor Red
      exit 1
       }
        }
        
        Write-Host ""
        Write-Host "? Packages published to GitHub Packages" -ForegroundColor Green
    }
    
    'NuGetOrg' {
        Write-Host "Publishing to NuGet.org..." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "WARNING: You are about to publish to the PUBLIC NuGet.org repository!" -ForegroundColor Red
Write-Host "This will make your packages available to everyone." -ForegroundColor Red
        Write-Host ""
      $confirm = Read-Host "Are you sure you want to continue? (type 'YES' to confirm)"
      
        if ($confirm -ne 'YES') {
            Write-Host "Publish cancelled" -ForegroundColor Yellow
          exit 0
  }
        
        if ([string]::IsNullOrEmpty($ApiKey)) {
            Write-Host "ERROR: NuGet.org API key required" -ForegroundColor Red
            Write-Host "Get your API key at: https://www.nuget.org/account/apikeys" -ForegroundColor Yellow
            exit 1
 }
        
        # Push packages
        foreach ($pkg in $packages) {
  Write-Host "  Pushing $($pkg.Name)..." -ForegroundColor Cyan
            dotnet nuget push $pkg.FullName --source "https://api.nuget.org/v3/index.json" --api-key $ApiKey

            if ($LASTEXITCODE -ne 0) {
      Write-Host "ERROR: Failed to push $($pkg.Name)" -ForegroundColor Red
   exit 1
         }
   }
  
        Write-Host ""
        Write-Host "? Packages published to NuGet.org" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "     Publishing Complete!          " -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
