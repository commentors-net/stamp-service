# Build Complete Distribution Package
# Creates a professional distribution package with installer and ZIP

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [switch]$IncludeAdminGUI = $true
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Aegis Mint - Complete Distribution" -ForegroundColor Cyan
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
Write-Host "Building Aegis Mint Service..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ServiceProject = Join-Path $RootDir "src\StampService\StampService.csproj"
& dotnet publish $ServiceProject -c $Configuration -o (Join-Path $DistDir "StampService") -p:Version=$Version

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Aegis Mint Service build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "? Aegis Mint Service built successfully" -ForegroundColor Green
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
$ZipName = "AegisMint-Distribution-$Version-$Timestamp.zip"
$ZipPath = Join-Path $RootDir $ZipName

Compress-Archive -Path "$DistDir\*" -DestinationPath $ZipPath -CompressionLevel Optimal -Force

$ZipSize = [math]::Round(((Get-Item $ZipPath).Length / 1MB), 2)
Write-Host "? ZIP created: $ZipName" -ForegroundColor Green
Write-Host "? Size: $ZipSize MB" -ForegroundColor Green
Write-Host ""

# Build AdminGUI installer (if requested and Inno Setup available)
if ($IncludeAdminGUI) {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Building Aegis Mint Installer..." -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    $InnoSetupPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    
    # Check if Inno Setup is installed
    if (-not (Test-Path $InnoSetupPath)) {
        Write-Host "? ERROR: Inno Setup 6 not found!" -ForegroundColor Red
        Write-Host "" 
        Write-Host "Expected location: $InnoSetupPath" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Please install Inno Setup 6 from:" -ForegroundColor Yellow
        Write-Host "  https://jrsoftware.org/isdl.php" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "After installation, run this script again to create the installer." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "??  Skipping installer build - ZIP package was created successfully" -ForegroundColor Yellow
    }
    else {
        Write-Host "? Found Inno Setup at: $InnoSetupPath" -ForegroundColor Green
        Write-Host ""
        
        $InstallerScript = Join-Path $ScriptDir "Complete-Installer.iss"
        
        if (-not (Test-Path $InstallerScript)) {
            Write-Host "? ERROR: Installer script not found!" -ForegroundColor Red
            Write-Host "Expected: $InstallerScript" -ForegroundColor Yellow
            Write-Host ""
        }
        else {
            Write-Host "? Found installer script: Complete-Installer.iss" -ForegroundColor Green
            Write-Host ""
            
            try {
                # Update version in installer script
                Write-Host "Updating version in installer script..." -ForegroundColor Cyan
                $ScriptContent = Get-Content $InstallerScript -Raw
                $ScriptContent = $ScriptContent -replace '#define MyAppVersion ".*"', "#define MyAppVersion `"$Version`""
                Set-Content -Path $InstallerScript -Value $ScriptContent -NoNewline
                Write-Host "? Version updated to: $Version" -ForegroundColor Green
                Write-Host ""
                
                # Build installer with Inno Setup (verbose mode for debugging)
                Write-Host "Compiling installer with Inno Setup..." -ForegroundColor Cyan
                Write-Host "  Compiler: $InnoSetupPath" -ForegroundColor Gray
                Write-Host "  Script: $InstallerScript" -ForegroundColor Gray
                Write-Host "  Output: AegisMint-Complete-Setup-$Version.exe" -ForegroundColor Gray
                Write-Host ""
                Write-Host "--- Inno Setup Output ---" -ForegroundColor DarkGray
                
                # Run Inno Setup without /Q flag to see output
                $InnoArgs = @($InstallerScript)
                & $InnoSetupPath @InnoArgs
                
                Write-Host "--- End of Inno Setup Output ---" -ForegroundColor DarkGray
                Write-Host ""
                
                # Check if installer was created
                $ExpectedInstallerPath = Join-Path $RootDir "AegisMint-Complete-Setup-$Version.exe"
                
                if ($LASTEXITCODE -eq 0) {
                    if (Test-Path $ExpectedInstallerPath) {
                        $InstallerSize = [math]::Round(((Get-Item $ExpectedInstallerPath).Length / 1MB), 2)
                        Write-Host "? Aegis Mint installer created successfully!" -ForegroundColor Green
                        Write-Host "  Location: $ExpectedInstallerPath" -ForegroundColor Gray
                        Write-Host "  Size: $InstallerSize MB" -ForegroundColor Gray
                    }
                    else {
                        Write-Host "??  WARNING: Inno Setup completed but installer file not found!" -ForegroundColor Yellow
                        Write-Host "Expected: $ExpectedInstallerPath" -ForegroundColor Gray
                        Write-Host ""
                        Write-Host "Possible issues:" -ForegroundColor Yellow
                        Write-Host "  1. Check OutputDir in Complete-Installer.iss" -ForegroundColor Gray
                        Write-Host "  2. Verify dist folder structure matches [Files] section" -ForegroundColor Gray
                        Write-Host "  3. Check Inno Setup compilation messages above" -ForegroundColor Gray
                    }
                }
                else {
                    Write-Host "? ERROR: Installer build failed!" -ForegroundColor Red
                    Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor Yellow
                    Write-Host ""
                    Write-Host "Please review the Inno Setup output above for error details." -ForegroundColor Yellow
                }
            }
            catch {
                Write-Host "? ERROR: Installer build exception!" -ForegroundColor Red
                Write-Host "Exception: $($_.Exception.Message)" -ForegroundColor Yellow
                Write-Host ""
                Write-Host "Stack trace:" -ForegroundColor Gray
                Write-Host $_.Exception.StackTrace -ForegroundColor DarkGray
            }
        }
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
Write-Host ""
Write-Host "  ? ZIP Package:" -ForegroundColor Green
Write-Host "    $ZipName" -ForegroundColor White
Write-Host "    Location: $ZipPath" -ForegroundColor Gray
Write-Host "    Size: $ZipSize MB" -ForegroundColor Gray
Write-Host ""

if ($IncludeAdminGUI) {
    $InstallerPath = Join-Path $RootDir "AegisMint-Complete-Setup-$Version.exe"
    if (Test-Path $InstallerPath) {
        $InstallerSize = [math]::Round(((Get-Item $InstallerPath).Length / 1MB), 2)
        Write-Host "  ? Aegis Mint Installer (EXE):" -ForegroundColor Green
        Write-Host "    AegisMint-Complete-Setup-$Version.exe" -ForegroundColor White
        Write-Host "    Location: $InstallerPath" -ForegroundColor Gray
        Write-Host "    Size: $InstallerSize MB" -ForegroundColor Gray
        Write-Host "    Includes: Service + AdminCLI + AdminGUI" -ForegroundColor Gray
        Write-Host ""
    }
    else {
        Write-Host "  ? Aegis Mint Installer (EXE):" -ForegroundColor Yellow
        Write-Host "    NOT CREATED - See warnings above" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "    Possible causes:" -ForegroundColor Gray
        Write-Host "      • Inno Setup not installed" -ForegroundColor DarkGray
        Write-Host "      • Inno Setup compilation errors" -ForegroundColor DarkGray
        Write-Host "      • Missing files in dist folder" -ForegroundColor DarkGray
        Write-Host ""
        Write-Host "    The ZIP package is still available and fully functional." -ForegroundColor Cyan
        Write-Host ""
    }
}

Write-Host "Components:" -ForegroundColor Cyan
Write-Host "  ? Aegis Mint Service (Windows Service)" -ForegroundColor White
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
    $InstallerPath = Join-Path $RootDir "AegisMint-Complete-Setup-$Version.exe"
    if (Test-Path $InstallerPath) {
        Write-Host "  4. Test Aegis Mint installer and all features" -ForegroundColor Yellow
    }
    else {
        Write-Host "  4. Install Inno Setup and re-run to create EXE installer (optional)" -ForegroundColor Yellow
    }
}
Write-Host ""

Write-Host "Distribution Locations:" -ForegroundColor Cyan
Write-Host "  Build artifacts: $DistDir" -ForegroundColor White
Write-Host "  ZIP package: $ZipPath" -ForegroundColor White
$InstallerPath = Join-Path $RootDir "AegisMint-Complete-Setup-$Version.exe"
if ($IncludeAdminGUI -and (Test-Path $InstallerPath)) {
    Write-Host "  Aegis Mint installer: $InstallerPath" -ForegroundColor White
}
else {
    Write-Host "  Aegis Mint installer: Not created (use ZIP for manual installation)" -ForegroundColor DarkGray
}
Write-Host ""

Write-Host "? Build completed successfully!" -ForegroundColor Green
Write-Host ""
