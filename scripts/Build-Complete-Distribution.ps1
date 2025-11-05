# Build Complete Distribution with Installer
# This script builds the solution and creates a single-file installer

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ".\dist",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipTests,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipInstaller,
    
    [Parameter(Mandatory=$false)]
    [string]$InnoSetupPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)

$ErrorActionPreference = "Stop"

Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "  Secure Stamp Service - Complete Build Script  " -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Step 1: Run the standard build
Write-Host "[1/6] Running standard build process..." -ForegroundColor Yellow
$buildParams = @{
    Configuration = $Configuration
OutputPath = $OutputPath
}
if ($SkipTests) {
    $buildParams['SkipTests'] = $true
}

& "$PSScriptRoot\Build-Distribution.ps1" @buildParams

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build-Distribution.ps1 failed" -ForegroundColor Red
 exit 1
}

Write-Host ""
Write-Host "[2/6] Standard build complete" -ForegroundColor Green

# Step 2: Check if Inno Setup is installed
Write-Host ""
Write-Host "[3/6] Checking for Inno Setup..." -ForegroundColor Yellow

if (-not (Test-Path $InnoSetupPath)) {
    Write-Host "  WARNING: Inno Setup not found at: $InnoSetupPath" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  To create a single-file installer, please:" -ForegroundColor Yellow
    Write-Host "    1. Download Inno Setup from: https://jrsoftware.org/isdl.php" -ForegroundColor White
    Write-Host "    2. Install Inno Setup (default location recommended)" -ForegroundColor White
    Write-Host "    3. Run this script again" -ForegroundColor White
    Write-Host ""
    
    if (-not $SkipInstaller) {
        $response = Read-Host "  Do you want to continue without creating installer? (y/n)"
        if ($response -ne 'y') {
            exit 1
        }
        $SkipInstaller = $true
 }
}

# Step 3: Create LICENSE.txt if it doesn't exist
Write-Host ""
Write-Host "[4/6] Preparing installer resources..." -ForegroundColor Yellow

$licensePath = Join-Path $PSScriptRoot "..\LICENSE.txt"
if (-not (Test-Path $licensePath)) {
    @"
MIT License

Copyright (c) 2025 Your Organization

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
"@ | Out-File -FilePath $licensePath -Encoding UTF8
    Write-Host "  Created LICENSE.txt" -ForegroundColor Green
}

Write-Host "  Resources ready" -ForegroundColor Green

# Step 4: Build installer
if (-not $SkipInstaller -and (Test-Path $InnoSetupPath)) {
    Write-Host ""
    Write-Host "[5/6] Building single-file installer..." -ForegroundColor Yellow
    
    $issFile = Join-Path $PSScriptRoot "StampService-Installer.iss"
 
    & $InnoSetupPath $issFile
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Installer created successfully" -ForegroundColor Green
   
    # Find the created installer
        $installerFile = Get-ChildItem -Path (Split-Path $PSScriptRoot) -Filter "StampService-Setup-*.exe" | 
        Sort-Object LastWriteTime -Descending | 
     Select-Object -First 1
        
        if ($installerFile) {
    $installerSize = [math]::Round($installerFile.Length / 1MB, 2)
            Write-Host "  Installer file: $($installerFile.Name) ($installerSize MB)" -ForegroundColor White
   }
 } else {
        Write-Host "  WARNING: Installer build failed" -ForegroundColor Yellow
}
} else {
    Write-Host ""
    Write-Host "[5/6] Skipping installer creation" -ForegroundColor Yellow
}

# Step 5: Create summary
Write-Host ""
Write-Host "[6/6] Creating build summary..." -ForegroundColor Yellow

$summaryPath = Join-Path (Split-Path $PSScriptRoot) "BUILD-SUMMARY.txt"
$summary = @"
???????????????????????????????????????????????????????????????????
  Secure Stamp Service - Build Summary
???????????????????????????????????????????????????????????????????

Build Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Configuration: $Configuration
Build Script: Build-Complete-Distribution.ps1

DISTRIBUTION PACKAGE:
  Location: $OutputPath
  ZIP File: $(Get-ChildItem -Path (Split-Path $PSScriptRoot) -Filter "StampService-Distribution-*.zip" | Select-Object -Last 1 -ExpandProperty Name)

SINGLE-FILE INSTALLER:
"@

$installerFile = Get-ChildItem -Path (Split-Path $PSScriptRoot) -Filter "StampService-Setup-*.exe" -ErrorAction SilentlyContinue | 
    Sort-Object LastWriteTime -Descending | 
       Select-Object -First 1

if ($installerFile) {
    $installerSize = [math]::Round($installerFile.Length / 1MB, 2)
    $summary += @"

  Status: ? Created
  File: $($installerFile.Name)
  Size: $installerSize MB
  
  This is a single executable file that can be distributed to end users.
  Non-technical users simply run this file and follow the wizard.

"@
} else {
$summary += @"

  Status: ? Not Created
  Reason: $(if ($SkipInstaller) { "Skipped by user" } else { "Inno Setup not found" })
  
  To create a single-file installer:
    1. Install Inno Setup from https://jrsoftware.org/isdl.php
2. Run this script again

"@
}

$summary += @"

???????????????????????????????????????????????????????????????????
DISTRIBUTION METHODS:
???????????????????????????????????????????????????????????????????

METHOD 1: Single-File Installer (RECOMMENDED for non-technical users)
  
  1. Distribute: StampService-Setup-X.X.X.exe
  2. User runs the .exe file
  3. Installer wizard guides through installation
  4. Service is automatically installed and started
  5. User is prompted to create backup shares

  Advantages:
    ? Single file to distribute
    ? Automatic service installation
    ? Guided post-installation steps
    ? Professional installer experience
    ? Automatic uninstaller

METHOD 2: ZIP Package (For technical users / manual installation)
  
  1. Distribute: StampService-Distribution-YYYYMMDD-HHMMSS.zip
  2. User extracts ZIP file
  3. User opens PowerShell as Administrator
  4. User runs: .\Scripts\Install-StampService.ps1
  5. User manually creates backup shares

  Advantages:
    ? Full control over installation
    ? Can customize installation paths
    ? Can review files before installation
    ? Scriptable deployment

???????????????????????????????????????????????????????????????????
NEXT STEPS:
???????????????????????????????????????????????????????????????????

FOR DEVELOPERS:
  1. Test the installer on a clean VM
  2. Verify service starts correctly
  3. Test AdminCLI commands
  4. Create test backup shares and verify recovery

FOR DISTRIBUTION:
  1. Sign the installer (optional but recommended):
     signtool sign /f certificate.pfx /p password StampService-Setup-X.X.X.exe
  
  2. Create installation guide for end users:
     - Download the installer
     - Run as Administrator
     - Follow the wizard
     - Create backup shares when prompted
  
  3. Distribute the signed installer file to users

???????????????????????????????????????????????????????????????????

Build completed successfully!

"@

$summary | Out-File -FilePath $summaryPath -Encoding UTF8
Write-Host "  Summary saved to BUILD-SUMMARY.txt" -ForegroundColor Green

# Display summary
Write-Host ""
Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "            Build Complete!         " -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

if ($installerFile) {
    Write-Host "? Single-File Installer Created!" -ForegroundColor Green
    Write-Host "  File: $($installerFile.FullName)" -ForegroundColor White
    Write-Host "  Size: $installerSize MB" -ForegroundColor White
    Write-Host ""
Write-Host "  This installer can be distributed to end users." -ForegroundColor White
    Write-Host "  They simply run the .exe and follow the wizard." -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "? ZIP Package Created" -ForegroundColor Yellow
    Write-Host "  See BUILD-SUMMARY.txt for creating a single-file installer" -ForegroundColor White
Write-Host ""
}

Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Review BUILD-SUMMARY.txt for distribution options" -ForegroundColor White
Write-Host "  2. Test installation on a clean machine" -ForegroundColor White
Write-Host "  3. (Optional) Sign the installer with your certificate" -ForegroundColor White
Write-Host "  4. Distribute to end users" -ForegroundColor White
Write-Host ""

# Open Windows Explorer to show output
if ($installerFile) {
    Write-Host "Opening output folder..." -ForegroundColor Cyan
 Start-Sleep -Seconds 2
    explorer.exe "/select,$($installerFile.FullName)"
}
