# StampService Uninstallation Script
# Run this script as Administrator

param(
    [Parameter(Mandatory=$false)]
    [string]$ServiceName = "SecureStampService",
    
    [Parameter(Mandatory=$false)]
    [switch]$RemoveData = $false
)

$ErrorActionPreference = "Stop"

# Check if running as administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    exit 1
}

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host " Secure Stamp Service - Uninstallation Script " -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Stop service
Write-Host "[1/3] Stopping service..." -ForegroundColor Yellow
$Service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($Service) {
    if ($Service.Status -eq "Running") {
        Stop-Service -Name $ServiceName -Force
        Write-Host "  Service stopped" -ForegroundColor Green
    }
    Start-Sleep -Seconds 2
} else {
    Write-Host "  Service not found" -ForegroundColor Yellow
}

# Step 2: Remove service
Write-Host ""
Write-Host "[2/3] Removing service..." -ForegroundColor Yellow

if ($Service) {
    sc.exe delete $ServiceName | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Service removed" -ForegroundColor Green
    } else {
        Write-Host "  ERROR: Failed to remove service" -ForegroundColor Red
    }
} else {
    Write-Host "  Nothing to remove" -ForegroundColor Yellow
}

# Step 3: Remove data (optional)
Write-Host ""
Write-Host "[3/3] Cleaning up..." -ForegroundColor Yellow

if ($RemoveData) {
    Write-Host ""
    Write-Host "WARNING: You are about to delete all service data including:" -ForegroundColor Red
    Write-Host "  - Master key files" -ForegroundColor Red
    Write-Host "  - Audit logs" -ForegroundColor Red
    Write-Host "  - Configuration" -ForegroundColor Red
    Write-Host ""
    
    $Confirm = Read-Host "Type 'DELETE' to confirm data removal"
    
    if ($Confirm -eq "DELETE") {
        $DataPath = "C:\ProgramData\StampService"
        if (Test-Path $DataPath) {
            Remove-Item -Path $DataPath -Recurse -Force
            Write-Host "  Data directory removed: $DataPath" -ForegroundColor Green
        }
        
        $InstallPath = "C:\Program Files\StampService"
        if (Test-Path $InstallPath) {
            Remove-Item -Path $InstallPath -Recurse -Force
            Write-Host "  Installation directory removed: $InstallPath" -ForegroundColor Green
        }
    } else {
        Write-Host "  Data removal cancelled" -ForegroundColor Yellow
    }
} else {
    Write-Host "  Data preserved (use -RemoveData to delete)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "        Uninstallation Complete!               " -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""
