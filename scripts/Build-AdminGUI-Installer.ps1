# Build AdminGUI Installer
# Compiles the AdminGUI and creates an Inno Setup installer

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Stamp Service AdminGUI Installer Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$ProjectDir = Join-Path $RootDir "src\StampService.AdminGUI"
$OutputDir = Join-Path $ProjectDir "bin\$Configuration\net8.0-windows"
$InstallerScript = Join-Path $ScriptDir "AdminGUI-Installer.iss"

# Check if Inno Setup is installed
$InnoSetupPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $InnoSetupPath)) {
    Write-Host "? Inno Setup 6 not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install Inno Setup 6 from:" -ForegroundColor Yellow
    Write-Host "https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host ""
Write-Host "After installation, run this script again." -ForegroundColor Yellow
    exit 1
}

Write-Host "? Inno Setup found: $InnoSetupPath" -ForegroundColor Green
Write-Host ""

# Step 1: Clean previous builds
Write-Host "Step 1: Cleaning previous builds..." -ForegroundColor Cyan
if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
    Write-Host "  ? Cleaned: $OutputDir" -ForegroundColor Green
}
Write-Host ""

# Step 2: Build AdminGUI
Write-Host "Step 2: Building AdminGUI..." -ForegroundColor Cyan
Write-Host "  Configuration: $Configuration" -ForegroundColor Gray
Write-Host "  Version: $Version" -ForegroundColor Gray
Write-Host ""

$BuildArgs = @(
    "build",
  $ProjectDir,
    "-c", $Configuration,
    "-p:Version=$Version",
    "--no-incremental"
)

& dotnet @BuildArgs

if ($LASTEXITCODE -ne 0) {
Write-Host ""
    Write-Host "? Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "? Build successful!" -ForegroundColor Green
Write-Host ""

# Step 3: Verify build output
Write-Host "Step 3: Verifying build output..." -ForegroundColor Cyan
$ExePath = Join-Path $OutputDir "StampService.AdminGUI.exe"
if (-not (Test-Path $ExePath)) {
  Write-Host "? AdminGUI executable not found: $ExePath" -ForegroundColor Red
    exit 1
}

$FileVersion = (Get-Item $ExePath).VersionInfo.FileVersion
Write-Host "  ? Executable found: StampService.AdminGUI.exe" -ForegroundColor Green
Write-Host "  ? File version: $FileVersion" -ForegroundColor Green

# Count files
$FileCount = (Get-ChildItem -Path $OutputDir -Recurse -File).Count
$TotalSize = [math]::Round(((Get-ChildItem -Path $OutputDir -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB), 2)
Write-Host "  ? Files: $FileCount" -ForegroundColor Green
Write-Host "  ? Total size: $TotalSize MB" -ForegroundColor Green
Write-Host ""

# Step 4: Create documentation folder
Write-Host "Step 4: Preparing documentation..." -ForegroundColor Cyan
$DocsSourceDir = Join-Path $RootDir "Resources"
$DocsDestDir = Join-Path $OutputDir "Docs"

if (-not (Test-Path $DocsDestDir)) {
    New-Item -ItemType Directory -Path $DocsDestDir -Force | Out-Null
}

# Copy key documentation files
$DocFiles = @(
    "QUICKSTART.md",
    "ADMINGUI-COMPLETE-SUMMARY.md",
 "POLISH-QUICK-REF.md",
    "ADMIN-CHECK-QUICK-REF.md",
    "NEXT-INTEGRATION-QUICK-REF.md"
)

foreach ($DocFile in $DocFiles) {
    $SourcePath = Join-Path $DocsSourceDir $DocFile
    if (Test-Path $SourcePath) {
        Copy-Item -Path $SourcePath -Destination $DocsDestDir -Force
        Write-Host "  ? Copied: $DocFile" -ForegroundColor Green
    }
}

# Copy README
$ReadmePath = Join-Path $RootDir "README.md"
if (Test-Path $ReadmePath) {
    Copy-Item -Path $ReadmePath -Destination $OutputDir -Force
    Write-Host "  ? Copied: README.md" -ForegroundColor Green
}

Write-Host ""

# Step 5: Update version in Inno Setup script
Write-Host "Step 5: Updating Inno Setup script version..." -ForegroundColor Cyan
$ScriptContent = Get-Content $InstallerScript -Raw
$ScriptContent = $ScriptContent -replace '#define MyAppVersion ".*"', "#define MyAppVersion `"$Version`""
Set-Content -Path $InstallerScript -Value $ScriptContent -NoNewline
Write-Host "  ? Version updated to: $Version" -ForegroundColor Green
Write-Host ""

# Step 6: Build installer
Write-Host "Step 6: Building installer with Inno Setup..." -ForegroundColor Cyan
Write-Host "  Compiler: $InnoSetupPath" -ForegroundColor Gray
Write-Host "  Script: $InstallerScript" -ForegroundColor Gray
Write-Host ""

$InnoArgs = @(
    "/Q",  # Quiet mode
    $InstallerScript
)

& $InnoSetupPath @InnoArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "? Installer build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "? Installer built successfully!" -ForegroundColor Green
Write-Host ""

# Step 7: Verify installer
Write-Host "Step 7: Verifying installer..." -ForegroundColor Cyan
$InstallerPath = Join-Path $RootDir "StampService-AdminGUI-Setup-$Version.exe"

if (-not (Test-Path $InstallerPath)) {
    Write-Host "? Installer not found: $InstallerPath" -ForegroundColor Red
    exit 1
}

$InstallerSize = [math]::Round(((Get-Item $InstallerPath).Length / 1MB), 2)
Write-Host "  ? Installer created: StampService-AdminGUI-Setup-$Version.exe" -ForegroundColor Green
Write-Host "? Installer size: $InstallerSize MB" -ForegroundColor Green
Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Installer Location:" -ForegroundColor Cyan
Write-Host "  $InstallerPath" -ForegroundColor White
Write-Host ""
Write-Host "Build Details:" -ForegroundColor Cyan
Write-Host "  Configuration: $Configuration" -ForegroundColor White
Write-Host "  Version: $Version" -ForegroundColor White
Write-Host "  Build files: $FileCount files ($TotalSize MB)" -ForegroundColor White
Write-Host "  Installer size: $InstallerSize MB" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Test the installer on a clean Windows machine" -ForegroundColor Yellow
Write-Host "  2. Verify all shortcuts are created correctly" -ForegroundColor Yellow
Write-Host "  3. Test the Admin GUI connects to the service" -ForegroundColor Yellow
Write-Host "  4. Verify uninstaller works correctly" -ForegroundColor Yellow
Write-Host ""
Write-Host "Distribution:" -ForegroundColor Cyan
Write-Host "  The installer can be distributed to users" -ForegroundColor White
Write-Host "  Users will need Administrator privileges to install" -ForegroundColor White
Write-Host "  The Stamp Service must be installed separately" -ForegroundColor White
Write-Host ""

# Optionally open the output folder
$Response = Read-Host "Open installer folder? (Y/N)"
if ($Response -eq "Y" -or $Response -eq "y") {
    explorer.exe $RootDir
}

Write-Host ""
Write-Host "? Build script completed successfully!" -ForegroundColor Green
Write-Host ""
