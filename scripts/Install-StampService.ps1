# StampService Installation Script
# Run this script as Administrator

param(
    [Parameter(Mandatory=$false)]
    [string]$InstallPath = "C:\Program Files\StampService",
    
    [Parameter(Mandatory=$false)]
    [string]$ServiceName = "SecureStampService",
    
    [Parameter(Mandatory=$false)]
    [string]$DataPath = "C:\ProgramData\StampService"
)

$ErrorActionPreference = "Stop"

# Check if running as administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Please right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  Secure Stamp Service - Installation Script  " -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Create directories
Write-Host "[1/6] Creating directories..." -ForegroundColor Yellow
if (-not (Test-Path $InstallPath)) {
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    Write-Host "  Created: $InstallPath" -ForegroundColor Green
}

if (-not (Test-Path $DataPath)) {
    New-Item -ItemType Directory -Path $DataPath -Force | Out-Null
  Write-Host "  Created: $DataPath" -ForegroundColor Green
}

$LogPath = Join-Path $DataPath "Logs"
if (-not (Test-Path $LogPath)) {
    New-Item -ItemType Directory -Path $LogPath -Force | Out-Null
    Write-Host "  Created: $LogPath" -ForegroundColor Green
}

# Step 2: Copy files
Write-Host ""
Write-Host "[2/6] Copying service files..." -ForegroundColor Yellow

# Determine source paths based on whether running from distribution package or development repo
$ScriptDir = Split-Path -Parent $PSScriptRoot
$DistributionSourcePath = Join-Path $ScriptDir "StampService"
$DistributionAdminCLIPath = Join-Path $ScriptDir "AdminCLI"
$DevSourcePath = Join-Path $PSScriptRoot "..\dist\StampService"
$DevAdminCLIPath = Join-Path $PSScriptRoot "..\dist\AdminCLI"

# Check if running from distribution package (extracted ZIP)
if (Test-Path $DistributionSourcePath) {
    $SourcePath = $DistributionSourcePath
    $AdminCLISourcePath = $DistributionAdminCLIPath
    Write-Host "  Using distribution package files" -ForegroundColor Cyan
}
# Check if running from development repo
elseif (Test-Path $DevSourcePath) {
    $SourcePath = $DevSourcePath
    $AdminCLISourcePath = $DevAdminCLIPath
    Write-Host "  Using development build files" -ForegroundColor Cyan
}
else {
    Write-Host "ERROR: Service files not found!" -ForegroundColor Red
    Write-Host "" -ForegroundColor Red
    Write-Host "Expected distribution structure:" -ForegroundColor Yellow
    Write-Host "  ExtractedFolder\" -ForegroundColor White
    Write-Host "    ??? StampService\      (service files)" -ForegroundColor White
    Write-Host "    ??? AdminCLI\        (admin tool)" -ForegroundColor White
    Write-Host "    ??? Scripts\(this script)" -ForegroundColor White
    Write-Host "" -ForegroundColor Yellow
    Write-Host "OR for development, run from repo root:" -ForegroundColor Yellow
    Write-Host "  .\scripts\Build-Distribution.ps1" -ForegroundColor White
  Write-Host "  Then extract the ZIP file and run installer from there" -ForegroundColor White
    Write-Host ""
    exit 1
}

Copy-Item -Path "$SourcePath\*" -Destination $InstallPath -Recurse -Force
Write-Host "  Copied service files to $InstallPath" -ForegroundColor Green

# Copy AdminCLI to same location for convenience
if (Test-Path $AdminCLISourcePath) {
  Copy-Item -Path "$AdminCLISourcePath\*" -Destination $InstallPath -Recurse -Force
    Write-Host "  Copied AdminCLI to $InstallPath" -ForegroundColor Green
}

# Step 3: Set permissions
Write-Host ""
Write-Host "[3/6] Setting up data directories..." -ForegroundColor Yellow

# Create registry key for secure key storage (simpler than file permissions!)
try {
    $regPath = "HKLM:\SOFTWARE\StampService"
    if (-not (Test-Path $regPath)) {
        New-Item -Path $regPath -Force | Out-Null
        Write-Host "  Created registry key: $regPath" -ForegroundColor Green
    }
} catch {
    Write-Host "  WARNING: Could not create registry key: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Set basic permissions on data directory (for logs only now)
$Acl = Get-Acl $DataPath
$Acl.SetAccessRuleProtection($false, $true)  # Allow inheritance

# Grant SYSTEM full control
$SystemRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "NT AUTHORITY\SYSTEM",
    "FullControl",
    "ContainerInherit,ObjectInherit",
    "None",
    "Allow"
)
$Acl.AddAccessRule($SystemRule)

# Grant Administrators full control
$AdminRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
"BUILTIN\Administrators",
    "FullControl",
    "ContainerInherit,ObjectInherit",
"None",
    "Allow"
)
$Acl.AddAccessRule($AdminRule)

Set-Acl -Path $DataPath -AclObject $Acl
Write-Host "  Set permissions on data directory (for logs)" -ForegroundColor Green

# Ensure Logs directory inherits permissions
if (Test-Path $LogPath) {
    $LogAcl = Get-Acl $LogPath
  $LogAcl.SetAccessRuleProtection($false, $false)  # Inherit from parent
    Set-Acl -Path $LogPath -AclObject $LogAcl
    Write-Host "  Applied permissions to Logs folder" -ForegroundColor Green
}

# Step 4: Update appsettings.json with correct paths
Write-Host ""
Write-Host "[4/6] Configuring service..." -ForegroundColor Yellow

$AppSettingsPath = Join-Path $InstallPath "appsettings.json"
if (Test-Path $AppSettingsPath) {
    $keyPath = (Join-Path $DataPath "master.key").Replace('\', '\\')
    $auditPath = (Join-Path $LogPath "audit.log").Replace('\', '\\')
    
    $config = @"
{
  "ServiceConfiguration": {
    "PipeName": "StampServicePipe",
 "KeyStorePath": "$keyPath",
    "AuditLogPath": "$auditPath"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
"@
    
    $config | Out-File -FilePath $AppSettingsPath -Encoding UTF8 -Force
    Write-Host "  Updated configuration with correct paths" -ForegroundColor Green
}

# Step 5: Check if service exists
Write-Host ""
Write-Host "[5/6] Installing Windows Service..." -ForegroundColor Yellow

$ExistingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($ExistingService) {
    Write-Host "  Service already exists. Stopping..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    
    Write-Host "  Removing existing service..." -ForegroundColor Yellow
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

$ServiceExe = Join-Path $InstallPath "StampService.exe"
if (-not (Test-Path $ServiceExe)) {
    Write-Host "ERROR: Service executable not found at $ServiceExe" -ForegroundColor Red
    exit 1
}

$result = sc.exe create $ServiceName binPath= "`"$ServiceExe`"" start= auto DisplayName= "Secure Stamp Service"
if ($LASTEXITCODE -eq 0) {
    Write-Host "  Service installed successfully" -ForegroundColor Green
    
    # Set service description
    sc.exe description $ServiceName "HSM-like signing authority for cryptographic stamp operations" | Out-Null
    
    # Configure service recovery options
    sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
    Write-Host "  Configured auto-restart on failure" -ForegroundColor Green
} else {
    Write-Host "ERROR: Failed to install service" -ForegroundColor Red
    Write-Host $result -ForegroundColor Red
    exit 1
}

# Step 6: Start service
Write-Host ""
Write-Host "[6/6] Starting service..." -ForegroundColor Yellow

Start-Service -Name $ServiceName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5

$ServiceStatus = Get-Service -Name $ServiceName
if ($ServiceStatus.Status -eq "Running") {
    Write-Host "  Service started successfully!" -ForegroundColor Green
    
    # Wait a moment for key generation
    Start-Sleep -Seconds 2
    
    # Verify key was created in Registry
  try {
 $regPath = "HKLM:\SOFTWARE\StampService"
        $keyExists = (Get-ItemProperty -Path $regPath -Name "MasterKey" -ErrorAction SilentlyContinue) -ne $null
        if ($keyExists) {
            Write-Host "  Master key generated and stored in Registry" -ForegroundColor Green
     }
    } catch {
        Write-Host "  Key verification skipped" -ForegroundColor Yellow
    }
} else {
    Write-Host "  WARNING: Service did not start. Status: $($ServiceStatus.Status)" -ForegroundColor Yellow
    Write-Host "  Check logs at: $LogPath" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Troubleshooting:" -ForegroundColor Yellow
    Write-Host "    1. View service logs: Get-Content '$LogPath\service*.log' -Tail 50" -ForegroundColor White
    Write-Host "    2. Check Windows Event Viewer: Application logs" -ForegroundColor White
  Write-Host "    3. Verify .NET 8 runtime: dotnet --list-runtimes" -ForegroundColor White
}

# Summary
Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "Installation Complete!              " -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Service Name:    $ServiceName" -ForegroundColor White
Write-Host "Install Path:    $InstallPath" -ForegroundColor White
Write-Host "Data Path:    $DataPath" -ForegroundColor White
Write-Host "Log Path:        $LogPath" -ForegroundColor White
Write-Host ""

if ($ServiceStatus.Status -eq "Running") {
    Write-Host "Next Steps:" -ForegroundColor Green
    Write-Host "  1. Test the service:" -ForegroundColor White
    Write-Host "       cd '$InstallPath'" -ForegroundColor Gray
    Write-Host "       .\StampService.AdminCLI.exe status" -ForegroundColor Gray
    Write-Host "       .\StampService.AdminCLI.exe test-stamp" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. CREATE BACKUP SHARES (CRITICAL!):" -ForegroundColor White
    Write-Host "       .\StampService.AdminCLI.exe create-shares --total 5 --threshold 3 --output C:\Shares" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Check service logs: Get-Content '$LogPath\service*.log' -Tail 50" -ForegroundColor White
    Write-Host "  2. Try starting manually: Start-Service $ServiceName" -ForegroundColor White
    Write-Host "  3. Check for errors in Windows Event Viewer" -ForegroundColor White
    Write-Host ""
}

Write-Host "To uninstall:" -ForegroundColor Yellow
Write-Host "  Run: .\Uninstall-StampService.ps1" -ForegroundColor White
Write-Host ""
