# Complete Uninstaller for Stamp Service
# Removes ALL components: Service, AdminCLI, AdminGUI, Registry, Folders, Shares

param(
    [switch]$RemoveData = $false,
  [switch]$RemoveShares = $false,
    [switch]$Silent = $false,
    [switch]$Force = $false
)

$ErrorActionPreference = "Stop"

# Require Administrator
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "? This script requires Administrator privileges!" -ForegroundColor Red
    Write-Host "Please run PowerShell as Administrator and try again." -ForegroundColor Yellow
    exit 1
}

function Write-Title {
    param([string]$Text)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  $Text" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Step {
    param([string]$Text)
    Write-Host "? $Text" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Text)
    Write-Host "  ? $Text" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Text)
    Write-Host "  ?? $Text" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Text)
 Write-Host "  ? $Text" -ForegroundColor Red
}

function Show-Menu {
    Clear-Host
    Write-Title "Stamp Service - Complete Uninstaller"
    
    Write-Host "This will remove:" -ForegroundColor White
    Write-Host "  • Stamp Service (Windows Service)" -ForegroundColor Gray
    Write-Host "  • AdminCLI (Command-line tool)" -ForegroundColor Gray
    Write-Host "  • AdminGUI (Desktop application)" -ForegroundColor Gray
    Write-Host "  • Registry entries" -ForegroundColor Gray
    Write-Host "  • Program Files directories" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "Optional removals:" -ForegroundColor White
    Write-Host "  [D] Remove data folder (logs, master key)" -ForegroundColor Gray
    Write-Host "  [S] Remove backup shares folder" -ForegroundColor Gray
  Write-Host ""
    
    Write-Host "Options:" -ForegroundColor Cyan
    Write-Host "  [1] Uninstall ALL (Service + AdminCLI + AdminGUI + Registry)" -ForegroundColor White
    Write-Host "  [2] Uninstall Service only" -ForegroundColor White
 Write-Host "  [3] Uninstall AdminGUI only" -ForegroundColor White
    Write-Host "  [4] Uninstall AdminCLI only" -ForegroundColor White
    Write-Host "  [5] Clean Registry entries only" -ForegroundColor White
Write-Host "  [6] Remove data folder only" -ForegroundColor White
    Write-Host "  [7] Show what will be removed (dry run)" -ForegroundColor White
    Write-Host "  [Q] Quit" -ForegroundColor White
    Write-Host ""
    
$choice = Read-Host "Select option"
    return $choice
}

function Get-Confirmation {
    param(
   [string]$Message,
   [switch]$Critical
    )
 
    if ($Silent -or $Force) {
      return $true
    }
    
    if ($Critical) {
        Write-Host ""
  Write-Host "??  CRITICAL ACTION!" -ForegroundColor Red
        Write-Host $Message -ForegroundColor Yellow
        Write-Host ""
      $response = Read-Host "Type 'YES' in capital letters to confirm"
 return ($response -eq "YES")
    }
    else {
        Write-Host ""
  $response = Read-Host "$Message (Y/N)"
      return ($response -eq "Y" -or $response -eq "y")
    }
}

function Stop-StampService {
    Write-Step "Stopping Stamp Service..."
    
    try {
        $service = Get-Service -Name "SecureStampService" -ErrorAction SilentlyContinue
      
  if ($service) {
     if ($service.Status -eq "Running") {
           Stop-Service -Name "SecureStampService" -Force -ErrorAction Stop
                Start-Sleep -Seconds 2
   Write-Success "Service stopped"
            }
    else {
      Write-Success "Service already stopped"
            }
     }
        else {
          Write-Warning "Service not found (may already be uninstalled)"
        }
    }
    catch {
   Write-Warning "Could not stop service: $($_.Exception.Message)"
    }
}

function Remove-StampService {
    Write-Step "Removing Stamp Service..."
    
  try {
  $service = Get-Service -Name "SecureStampService" -ErrorAction SilentlyContinue
     
    if ($service) {
            # Stop service first
            Stop-StampService
   
   # Delete service
     $result = & sc.exe delete "SecureStampService" 2>&1
            
if ($LASTEXITCODE -eq 0) {
   Write-Success "Service uninstalled"
        }
    else {
 Write-Warning "Service deletion returned code: $LASTEXITCODE"
            }
 }
        else {
     Write-Success "Service not installed"
        }
  }
    catch {
 Write-Error "Failed to remove service: $($_.Exception.Message)"
  }
}

function Remove-ProgramFiles {
    param([string]$Component)
    
    Write-Step "Removing $Component program files..."
    
    $paths = @()
    
    switch ($Component) {
        "Service" {
            $paths = @(
       "${env:ProgramFiles}\StampService",
     "${env:ProgramFiles(x86)}\StampService"
    )
        }
        "AdminGUI" {
            $paths = @(
          "${env:ProgramFiles}\StampService\AdminGUI",
      "${env:ProgramFiles(x86)}\StampService\AdminGUI"
            )
 }
        "AdminCLI" {
       $paths = @(
      "${env:ProgramFiles}\StampService\AdminCLI",
"${env:ProgramFiles(x86)}\StampService\AdminCLI"
   )
        }
        "All" {
            $paths = @(
    "${env:ProgramFiles}\StampService",
                "${env:ProgramFiles(x86)}\StampService"
            )
    }
    }
    
    $removed = $false
    foreach ($path in $paths) {
 if (Test-Path $path) {
       try {
      Remove-Item -Path $path -Recurse -Force -ErrorAction Stop
                Write-Success "Removed: $path"
         $removed = $true
            }
            catch {
            Write-Error "Could not remove: $path - $($_.Exception.Message)"
            }
        }
    }
    
    if (-not $removed) {
        Write-Success "No program files found"
    }
}

function Remove-DataFolder {
    Write-Step "Removing data folder..."
    
    $dataPath = "${env:ProgramData}\StampService"
    
    if (Test-Path $dataPath) {
        $items = Get-ChildItem -Path $dataPath -Recurse | Measure-Object
     Write-Host "  Found $($items.Count) items in data folder" -ForegroundColor Gray
        
$confirm = Get-Confirmation -Message "??  This will delete the master key and all logs. Continue?" -Critical
        
        if ($confirm) {
            try {
    Remove-Item -Path $dataPath -Recurse -Force -ErrorAction Stop
       Write-Success "Data folder removed: $dataPath"
            }
            catch {
    Write-Error "Could not remove data folder: $($_.Exception.Message)"
      }
     }
        else {
    Write-Warning "Data folder removal cancelled"
    }
    }
    else {
        Write-Success "Data folder not found"
    }
}

function Remove-SharesFolder {
    Write-Step "Removing backup shares folder..."
    
 Write-Host "  Common share locations:" -ForegroundColor Gray
    Write-Host "    • C:\Shares" -ForegroundColor Gray
    Write-Host "    • ${env:USERPROFILE}\Shares" -ForegroundColor Gray
    Write-Host "    • Desktop\Shares" -ForegroundColor Gray
    Write-Host ""
    
    $customPath = Read-Host "Enter share folder path (or press Enter to skip)"
    
    if ($customPath -and (Test-Path $customPath)) {
        $confirm = Get-Confirmation -Message "??  Delete all shares in $customPath?" -Critical
        
        if ($confirm) {
     try {
           Remove-Item -Path $customPath -Recurse -Force -ErrorAction Stop
  Write-Success "Shares folder removed: $customPath"
            }
      catch {
      Write-Error "Could not remove shares folder: $($_.Exception.Message)"
            }
        }
        else {
 Write-Warning "Shares folder removal cancelled"
        }
    }
    else {
        Write-Success "No shares folder specified"
    }
}

function Remove-RegistryEntries {
    Write-Step "Removing registry entries..."
    
    $registryPaths = @(
    "HKLM:\SOFTWARE\StampService",
        "HKLM:\SOFTWARE\WOW6432Node\StampService",
        "HKCU:\SOFTWARE\StampService"
    )
    
    $removed = $false
    foreach ($path in $registryPaths) {
   if (Test-Path $path) {
            try {
                Remove-Item -Path $path -Recurse -Force -ErrorAction Stop
        Write-Success "Removed: $path"
      $removed = $true
     }
        catch {
          Write-Error "Could not remove: $path - $($_.Exception.Message)"
            }
        }
  }
    
    if (-not $removed) {
        Write-Success "No registry entries found"
    }
}

function Remove-Shortcuts {
    Write-Step "Removing shortcuts..."
    
    $shortcuts = @(
        "${env:PUBLIC}\Desktop\Stamp Service Admin GUI.lnk",
        "${env:APPDATA}\Microsoft\Internet Explorer\Quick Launch\Stamp Service Admin GUI.lnk",
   "${env:ProgramData}\Microsoft\Windows\Start Menu\Programs\Stamp Service Admin GUI"
    )
    
    $removed = $false
    foreach ($shortcut in $shortcuts) {
     if (Test-Path $shortcut) {
            try {
    Remove-Item -Path $shortcut -Recurse -Force -ErrorAction Stop
            Write-Success "Removed: $shortcut"
       $removed = $true
         }
        catch {
             Write-Error "Could not remove: $shortcut - $($_.Exception.Message)"
       }
        }
    }
    
    if (-not $removed) {
 Write-Success "No shortcuts found"
    }
}

function Show-WhatWillBeRemoved {
    Write-Title "Dry Run - What Will Be Removed"
    
    Write-Host "Checking installed components..." -ForegroundColor Cyan
 Write-Host ""
    
    # Check Service
    Write-Host "Windows Service:" -ForegroundColor Yellow
    $service = Get-Service -Name "SecureStampService" -ErrorAction SilentlyContinue
    if ($service) {
    Write-Host "  ? Found: SecureStampService (Status: $($service.Status))" -ForegroundColor Green
    }
    else {
        Write-Host "  ? Not installed" -ForegroundColor Gray
    }
    
  # Check Program Files
    Write-Host ""
    Write-Host "Program Files:" -ForegroundColor Yellow
  
    $paths = @(
   "${env:ProgramFiles}\StampService",
        "${env:ProgramFiles}\StampService\AdminGUI",
        "${env:ProgramFiles}\StampService\AdminCLI"
    )
    
    foreach ($path in $paths) {
        if (Test-Path $path) {
            $size = (Get-ChildItem -Path $path -Recurse -File | Measure-Object -Property Length -Sum).Sum
  $sizeMB = [math]::Round($size / 1MB, 2)
   Write-Host "  ? Found: $path ($sizeMB MB)" -ForegroundColor Green
        }
        else {
   Write-Host "  ? Not found: $path" -ForegroundColor Gray
  }
    }
    
    # Check Data Folder
    Write-Host ""
    Write-Host "Data Folder:" -ForegroundColor Yellow
    $dataPath = "${env:ProgramData}\StampService"
    if (Test-Path $dataPath) {
        $items = (Get-ChildItem -Path $dataPath -Recurse | Measure-Object).Count
        $size = (Get-ChildItem -Path $dataPath -Recurse -File | Measure-Object -Property Length -Sum).Sum
        $sizeMB = [math]::Round($size / 1MB, 2)
      Write-Host "  ? Found: $dataPath ($items items, $sizeMB MB)" -ForegroundColor Green
        
 # Check for master key
        $keyPath = Join-Path $dataPath "master.key"
        if (Test-Path $keyPath) {
 Write-Host "    ??Contains master key file" -ForegroundColor Yellow
   }
        
     # Check for logs
 $logsPath = Join-Path $dataPath "Logs"
if (Test-Path $logsPath) {
        $logCount = (Get-ChildItem -Path $logsPath -File | Measure-Object).Count
  Write-Host "    ?? Contains $logCount log files" -ForegroundColor Gray
    }
}
    else {
        Write-Host "  ? Not found: $dataPath" -ForegroundColor Gray
    }
    
    # Check Registry
    Write-Host ""
    Write-Host "Registry Entries:" -ForegroundColor Yellow
    $registryPaths = @(
        "HKLM:\SOFTWARE\StampService",
 "HKCU:\SOFTWARE\StampService"
    )
    
    $foundRegistry = $false
    foreach ($path in $registryPaths) {
      if (Test-Path $path) {
       Write-Host "  ? Found: $path" -ForegroundColor Green
  $foundRegistry = $true
        }
    }
    
    if (-not $foundRegistry) {
        Write-Host "  ? No registry entries found" -ForegroundColor Gray
    }
    
    # Check Shortcuts
    Write-Host ""
    Write-Host "Shortcuts:" -ForegroundColor Yellow
    $shortcuts = @(
        "${env:PUBLIC}\Desktop\Stamp Service Admin GUI.lnk",
     "${env:ProgramData}\Microsoft\Windows\Start Menu\Programs\Stamp Service Admin GUI"
    )
    
    $foundShortcut = $false
    foreach ($shortcut in $shortcuts) {
        if (Test-Path $shortcut) {
       Write-Host "  ? Found: $shortcut" -ForegroundColor Green
   $foundShortcut = $true
        }
    }
    
    if (-not $foundShortcut) {
     Write-Host "  ? No shortcuts found" -ForegroundColor Gray
    }
    
    Write-Host ""
  Write-Host "Press any key to return to menu..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

function Uninstall-All {
    Write-Title "Complete Uninstallation"
    
    Write-Host "This will remove ALL Stamp Service components:" -ForegroundColor Yellow
    Write-Host "  • Windows Service" -ForegroundColor Gray
    Write-Host "  • AdminGUI application" -ForegroundColor Gray
    Write-Host "  • AdminCLI tool" -ForegroundColor Gray
    Write-Host "  • Program Files directories" -ForegroundColor Gray
    Write-Host "  • Registry entries" -ForegroundColor Gray
    Write-Host "  • Desktop and Start Menu shortcuts" -ForegroundColor Gray
    Write-Host ""
 
    if ($RemoveData) {
        Write-Host "  • Data folder (master key and logs)" -ForegroundColor Red
    }
    else {
        Write-Host "  Data folder will be PRESERVED" -ForegroundColor Green
  }
  
    if ($RemoveShares) {
   Write-Host "  • Backup shares folder" -ForegroundColor Red
    }
    Write-Host ""
    
    $confirm = Get-Confirmation -Message "Proceed with complete uninstallation?" -Critical
    
    if (-not $confirm) {
        Write-Host ""
        Write-Host "Uninstallation cancelled." -ForegroundColor Yellow
      return
    }
    
    Write-Host ""
    Write-Host "Starting uninstallation..." -ForegroundColor Cyan
    Write-Host ""
    
    # 1. Stop and remove service
    Stop-StampService
    Remove-StampService
    
    # 2. Remove shortcuts
    Remove-Shortcuts
    
# 3. Remove program files
    Remove-ProgramFiles -Component "All"
    
    # 4. Remove registry entries
    Remove-RegistryEntries
    
# 5. Remove data folder (if requested)
    if ($RemoveData) {
        Remove-DataFolder
    }
 else {
   Write-Step "Preserving data folder..."
        Write-Success "Data folder preserved at: ${env:ProgramData}\StampService"
    }
    
    # 6. Remove shares folder (if requested)
    if ($RemoveShares) {
   Remove-SharesFolder
  }
    
    Write-Host ""
    Write-Title "Uninstallation Complete!"
    
    Write-Host "Summary:" -ForegroundColor Cyan
    Write-Host "  ? Service removed" -ForegroundColor Green
    Write-Host "  ? Program files removed" -ForegroundColor Green
    Write-Host "  ? Registry entries cleaned" -ForegroundColor Green
    Write-Host "  ? Shortcuts removed" -ForegroundColor Green
    
    if ($RemoveData) {
        Write-Host "  ? Data folder removed" -ForegroundColor Green
  }
    else {
    Write-Host "  ??  Data folder preserved" -ForegroundColor Yellow
        Write-Host "     Location: ${env:ProgramData}\StampService" -ForegroundColor Gray
    }
    
    if ($RemoveShares) {
        Write-Host "  ? Shares folder removed" -ForegroundColor Green
    }
    
 Write-Host ""
    Write-Host "The Stamp Service has been completely uninstalled." -ForegroundColor Green
    Write-Host ""
}

# Main execution
if ($Silent) {
    Uninstall-All
    exit 0
}

# Interactive menu
while ($true) {
    $choice = Show-Menu
    
switch ($choice) {
        "1" {
            # Uninstall all
            Uninstall-All
         Write-Host "Press any key to exit..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
          exit 0
   }
        "2" {
     # Service only
            Write-Title "Uninstall Service Only"
     Stop-StampService
   Remove-StampService
 Remove-ProgramFiles -Component "Service"
   Write-Host ""
            Write-Host "Press any key to continue..." -ForegroundColor Gray
            $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        }
     "3" {
        # AdminGUI only
Write-Title "Uninstall AdminGUI Only"
            Remove-ProgramFiles -Component "AdminGUI"
          Remove-Shortcuts
            Write-Host ""
            Write-Host "Press any key to continue..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
      }
  "4" {
      # AdminCLI only
  Write-Title "Uninstall AdminCLI Only"
            Remove-ProgramFiles -Component "AdminCLI"
    Write-Host ""
            Write-Host "Press any key to continue..." -ForegroundColor Gray
            $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        }
    "5" {
            # Registry only
            Write-Title "Clean Registry Entries Only"
            Remove-RegistryEntries
     Write-Host ""
            Write-Host "Press any key to continue..." -ForegroundColor Gray
            $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        }
        "6" {
            # Data folder only
            Write-Title "Remove Data Folder Only"
            Remove-DataFolder
  Write-Host ""
            Write-Host "Press any key to continue..." -ForegroundColor Gray
            $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
      }
        "7" {
         # Dry run
      Show-WhatWillBeRemoved
        }
     { $_ -eq "Q" -or $_ -eq "q" } {
            Write-Host ""
   Write-Host "Exiting uninstaller." -ForegroundColor Gray
    exit 0
  }
        "D" {
  $RemoveData = $true
            Write-Host "? Data folder will be removed" -ForegroundColor Yellow
    Start-Sleep -Seconds 1
        }
        "S" {
         $RemoveShares = $true
            Write-Host "? Shares folder will be removed" -ForegroundColor Yellow
  Start-Sleep -Seconds 1
  }
      default {
  Write-Host "Invalid option. Please try again." -ForegroundColor Red
  Start-Sleep -Seconds 1
      }
    }
}
