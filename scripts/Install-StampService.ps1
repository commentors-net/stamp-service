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

$SourcePath = Join-Path $PSScriptRoot "publish"
if (-not (Test-Path $SourcePath)) {
    Write-Host "ERROR: Published files not found at $SourcePath" -ForegroundColor Red
    Write-Host "Please run: dotnet publish -c Release -r win-x64 --self-contained false" -ForegroundColor Yellow
    exit 1
}

Copy-Item -Path "$SourcePath\*" -Destination $InstallPath -Recurse -Force
Write-Host "  Copied service files to $InstallPath" -ForegroundColor Green

# Step 3: Set permissions
Write-Host ""
Write-Host "[3/6] Setting permissions..." -ForegroundColor Yellow

$Acl = Get-Acl $DataPath
$Acl.SetAccessRuleProtection($true, $false)

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
Write-Host "  Set restrictive permissions on $DataPath" -ForegroundColor Green

# Step 4: Check if service exists
Write-Host ""
Write-Host "[4/6] Checking for existing service..." -ForegroundColor Yellow

$ExistingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($ExistingService) {
    Write-Host "  Service already exists. Stopping..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    
    Write-Host "  Removing existing service..." -ForegroundColor Yellow
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

# Step 5: Install service
Write-Host ""
Write-Host "[5/6] Installing Windows Service..." -ForegroundColor Yellow

$ServiceExe = Join-Path $InstallPath "StampService.exe"
if (-not (Test-Path $ServiceExe)) {
    Write-Host "ERROR: Service executable not found at $ServiceExe" -ForegroundColor Red
    exit 1
}

$result = sc.exe create $ServiceName binPath= "`"$ServiceExe`"" start= auto DisplayName= "Secure Stamp Service"
if ($LASTEXITCODE -eq 0) {
    Write-Host "  Service installed successfully" -ForegroundColor Green
    
    # Set service description
    sc.exe description $ServiceName "HSM-like signing authority for cryptographic stamp operations"
    
    # Configure service recovery options
    sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000
    Write-Host "  Configured auto-restart on failure" -ForegroundColor Green
} else {
    Write-Host "ERROR: Failed to install service" -ForegroundColor Red
    Write-Host $result -ForegroundColor Red
    exit 1
}

# Step 6: Start service
Write-Host ""
Write-Host "[6/6] Starting service..." -ForegroundColor Yellow

Start-Service -Name $ServiceName
Start-Sleep -Seconds 3

$ServiceStatus = Get-Service -Name $ServiceName
if ($ServiceStatus.Status -eq "Running") {
    Write-Host "  Service started successfully!" -ForegroundColor Green
} else {
    Write-Host "  WARNING: Service did not start. Status: $($ServiceStatus.Status)" -ForegroundColor Yellow
    Write-Host "  Check logs at: $LogPath" -ForegroundColor Yellow
}

# Summary
Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "           Installation Complete!              " -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Service Name:    $ServiceName" -ForegroundColor White
Write-Host "Install Path:    $InstallPath" -ForegroundColor White
Write-Host "Data Path:       $DataPath" -ForegroundColor White
Write-Host "Log Path:        $LogPath" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Check service status: Get-Service $ServiceName" -ForegroundColor White
Write-Host "  2. View logs: Get-Content '$LogPath\service*.log' -Tail 50" -ForegroundColor White
Write-Host "  3. Test connection: StampService.AdminCLI.exe status" -ForegroundColor White
Write-Host ""
Write-Host "To uninstall:" -ForegroundColor Yellow
Write-Host "  Run: .\Uninstall-StampService.ps1" -ForegroundColor White
Write-Host ""
