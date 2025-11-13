# Build Complete Distribution Package
# Creates a professional distribution package with installer and ZIP

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [switch]$IncludeAdminGUI = $true
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Stamp Service - Complete Distribution" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration: $Configuration" -ForegroundColor White
Write-Host "Version: $Version" -ForegroundColor White
Write-Host "Include AdminGUI: $IncludeAdminGUI" -ForegroundColor White
Write-Host ""

# Get directories
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$DistDir = Join-Path $RootDir "dist"

# Create distribution directory
if (Test-Path $DistDir) {
    Write-Host "Cleaning previous distribution..." -ForegroundColor Cyan
    Remove-Item -Path $DistDir -Recurse -Force
}
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
Write-Host "? Distribution directory created" -ForegroundColor Green
Write-Host ""

# Build StampService
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building StampService..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ServiceProject = Join-Path $RootDir "src\StampService\StampService.csproj"
& dotnet publish $ServiceProject -c $Configuration -o (Join-Path $DistDir "StampService") -p:Version=$Version

if ($LASTEXITCODE -ne 0) {
    Write-Host "? StampService build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "? StampService built successfully" -ForegroundColor Green
Write-Host ""

# Build AdminCLI
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building AdminCLI..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$AdminCLIProject = Join-Path $RootDir "src\StampService.AdminCLI\StampService.AdminCLI.csproj"
& dotnet publish $AdminCLIProject -c $Configuration -o (Join-Path $DistDir "AdminCLI") -p:Version=$Version

if ($LASTEXITCODE -ne 0) {
    Write-Host "? AdminCLI build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "? AdminCLI built successfully" -ForegroundColor Green
Write-Host ""

# Build AdminGUI (if requested)
if ($IncludeAdminGUI) {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Building AdminGUI..." -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    $AdminGUIProject = Join-Path $RootDir "src\StampService.AdminGUI\StampService.AdminGUI.csproj"
    & dotnet publish $AdminGUIProject -c $Configuration -o (Join-Path $DistDir "AdminGUI") -p:Version=$Version

    if ($LASTEXITCODE -ne 0) {
        Write-Host "? AdminGUI build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "? AdminGUI built successfully" -ForegroundColor Green
    Write-Host ""
}

# Copy scripts
Write-Host "Copying scripts..." -ForegroundColor Cyan
$ScriptsDestDir = Join-Path $DistDir "Scripts"
New-Item -ItemType Directory -Path $ScriptsDestDir -Force | Out-Null

$ScriptFiles = @(
    "Install-StampService.ps1",
    "Uninstall-StampService.ps1",
    "Complete-Uninstaller.ps1"
)

foreach ($Script in $ScriptFiles) {
    $SourcePath = Join-Path $ScriptDir $Script
    if (Test-Path $SourcePath) {
        Copy-Item -Path $SourcePath -Destination $ScriptsDestDir -Force
        Write-Host "? Copied: $Script" -ForegroundColor Green
    }
    else {
        Write-Host "  ??  Skipped: $Script (not found)" -ForegroundColor Yellow
    }
}
Write-Host "? Scripts copied" -ForegroundColor Green
Write-Host ""

# Copy documentation
Write-Host "Copying documentation..." -ForegroundColor Cyan
$DocsDestDir = Join-Path $DistDir "Documentation"
New-Item -ItemType Directory -Path $DocsDestDir -Force | Out-Null

$DocFiles = @(
    "README.md",
    "Resources\QUICKSTART.md",
    "Resources\DISTRIBUTION.md",
    "Resources\CLIENT-INTEGRATION.md",
    "Resources\USER-INSTALLATION-GUIDE.md"
)

if ($IncludeAdminGUI) {
    $DocFiles += @(
        "Resources\ADMINGUI-COMPLETE-SUMMARY.md",
        "Resources\POLISH-QUICK-REF.md"
    )
}

foreach ($Doc in $DocFiles) {
    $SourcePath = Join-Path $RootDir $Doc
    if (Test-Path $SourcePath) {
  $FileName = Split-Path -Leaf $SourcePath
    Copy-Item -Path $SourcePath -Destination (Join-Path $DocsDestDir $FileName) -Force
    }
}
Write-Host "? Documentation copied" -ForegroundColor Green
Write-Host ""

# Create version info
Write-Host "Creating version info..." -ForegroundColor Cyan
$VersionInfo = @{
    Version = $Version
    Configuration = $Configuration
    BuildDate = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
 Components = @{
        StampService = $true
        AdminCLI = $true
     AdminGUI = $IncludeAdminGUI
    }
} | ConvertTo-Json -Depth 10

$VersionInfo | Set-Content (Join-Path $DistDir "version.json")
Write-Host "? Version info created" -ForegroundColor Green
Write-Host ""

# Create ZIP package
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Creating ZIP distribution..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$ZipName = "StampService-Distribution-$Version-$Timestamp.zip"
$ZipPath = Join-Path $RootDir $ZipName

Compress-Archive -Path "$DistDir\*" -DestinationPath $ZipPath -CompressionLevel Optimal -Force

$ZipSize = [math]::Round(((Get-Item $ZipPath).Length / 1MB), 2)
Write-Host "? ZIP created: $ZipName" -ForegroundColor Green
Write-Host "? Size: $ZipSize MB" -ForegroundColor Green
Write-Host ""

# Build AdminGUI installer (if requested and Inno Setup available)
if ($IncludeAdminGUI) {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Building Unified Installer..." -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

 $InnoSetupPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    if (Test-Path $InnoSetupPath) {
      $InstallerScript = Join-Path $ScriptDir "Complete-Installer.iss"
        if (Test-Path $InstallerScript) {
            try {
      Write-Host "Updating version in installer script..." -ForegroundColor Cyan
    $ScriptContent = Get-Content $InstallerScript -Raw
    $ScriptContent = $ScriptContent -replace '#define MyAppVersion ".*"', "#define MyAppVersion `"$Version`""
    Set-Content -Path $InstallerScript -Value $ScriptContent -NoNewline
    
                Write-Host "Compiling installer with Inno Setup..." -ForegroundColor Cyan
     Write-Host "  Compiler: $InnoSetupPath" -ForegroundColor Gray
            Write-Host "  Script: $InstallerScript" -ForegroundColor Gray
          Write-Host ""
    
  $InnoArgs = @("/Q", $InstallerScript)
             & $InnoSetupPath @InnoArgs
        
      if ($LASTEXITCODE -eq 0) {
          Write-Host "? Unified installer created successfully!" -ForegroundColor Green
          }
    else {
        Write-Host "??  Installer build returned code: $LASTEXITCODE" -ForegroundColor Yellow
            }
            }
 catch {
       Write-Host "??  Unified installer build failed (non-critical): $($_.Exception.Message)" -ForegroundColor Yellow
         }
        }
        else {
            Write-Host "??  Installer script not found: $InstallerScript" -ForegroundColor Yellow
        }
    }
    else {
   Write-Host "??  Inno Setup not installed - Skipping unified installer" -ForegroundColor Gray
        Write-Host "   Install from: https://jrsoftware.org/isdl.php" -ForegroundColor Gray
    }
 Write-Host ""
}

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Distribution Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Package Details:" -ForegroundColor Cyan
Write-Host "  Version: $Version" -ForegroundColor White
Write-Host "  Configuration: $Configuration" -ForegroundColor White
Write-Host "  Build Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White
Write-Host ""

Write-Host "Distribution Files:" -ForegroundColor Cyan
Write-Host "  ZIP Package:" -ForegroundColor White
Write-Host "    $ZipName" -ForegroundColor Gray
Write-Host "    Size: $ZipSize MB" -ForegroundColor Gray
Write-Host ""

if ($IncludeAdminGUI) {
    $InstallerPath = Join-Path $RootDir "StampService-Complete-Setup-$Version.exe"
    if (Test-Path $InstallerPath) {
        $InstallerSize = [math]::Round(((Get-Item $InstallerPath).Length / 1MB), 2)
        Write-Host "  Unified Installer:" -ForegroundColor White
Write-Host "    StampService-Complete-Setup-$Version.exe" -ForegroundColor Gray
     Write-Host "    Size: $InstallerSize MB" -ForegroundColor Gray
      Write-Host "    Includes: Service + AdminCLI + AdminGUI" -ForegroundColor Gray
        Write-Host ""
    }
}

Write-Host "Components:" -ForegroundColor Cyan
Write-Host "  ? StampService (Windows Service)" -ForegroundColor White
Write-Host "  ? AdminCLI (Command-line tool)" -ForegroundColor White
if ($IncludeAdminGUI) {
    Write-Host "  ? AdminGUI (Desktop application)" -ForegroundColor White
}
Write-Host "  ? Installation scripts" -ForegroundColor White
Write-Host "  ? Documentation" -ForegroundColor White
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Test installation on clean Windows machine" -ForegroundColor Yellow
Write-Host "  2. Verify all components work correctly" -ForegroundColor Yellow
Write-Host "  3. Test backup/recovery process" -ForegroundColor Yellow
if ($IncludeAdminGUI) {
    Write-Host "  4. Test AdminGUI installer and all features" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "Distribution Locations:" -ForegroundColor Cyan
Write-Host "  Build artifacts: $DistDir" -ForegroundColor White
Write-Host "  ZIP package: $ZipPath" -ForegroundColor White
if ($IncludeAdminGUI -and (Test-Path (Join-Path $RootDir "StampService-Complete-Setup-$Version.exe"))) {
    Write-Host "  Unified installer: $(Join-Path $RootDir "StampService-Complete-Setup-$Version.exe")" -ForegroundColor White
}
Write-Host ""

Write-Host "? Build completed successfully!" -ForegroundColor Green
Write-Host ""
