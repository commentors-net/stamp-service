# ? Complete Uninstaller Implementation

## ?? **What Was Created**

A comprehensive PowerShell uninstaller with an **interactive menu** that can remove:

? **Windows Service** - SecureStampService  
? **AdminCLI** - Command-line tool  
? **AdminGUI** - Desktop application  
? **Registry Entries** - All Stamp Service keys  
? **Program Files** - All installation directories  
? **Data Folder** - Logs and master key (optional)  
? **Backup Shares** - Share files (optional)  
? **Shortcuts** - Desktop and Start Menu  

---

## ?? **Features**

### **Interactive Menu**
```
========================================
  Stamp Service - Complete Uninstaller
========================================

This will remove:
  • Stamp Service (Windows Service)
  • AdminCLI (Command-line tool)
  • AdminGUI (Desktop application)
  • Registry entries
  • Program Files directories

Optional removals:
  [D] Remove data folder (logs, master key)
  [S] Remove backup shares folder

Options:
  [1] Uninstall ALL (Service + AdminCLI + AdminGUI + Registry)
  [2] Uninstall Service only
  [3] Uninstall AdminGUI only
  [4] Uninstall AdminCLI only
  [5] Clean Registry entries only
  [6] Remove data folder only
  [7] Show what will be removed (dry run)
  [Q] Quit

Select option:
```

### **Safety Features**
- ? **Administrator Check** - Requires admin privileges
- ? **Confirmation Prompts** - For critical actions
- ? **Dry Run Mode** - See what will be removed
- ? **Selective Removal** - Choose components
- ? **Data Preservation** - Option to keep data folder
- ? **Share Protection** - Manual share folder selection

---

## ?? **Usage**

### **Method 1: Interactive Menu (Recommended)**

```powershell
# Run as Administrator
.\scripts\Complete-Uninstaller.ps1
```

**Follow the prompts:**
1. Select option from menu
2. Confirm critical actions
3. Wait for completion
4. Review summary

### **Method 2: Start Menu Shortcut**

```
Start Menu ? Stamp Service Admin GUI ? Complete Uninstaller
```

**Features:**
- Launches PowerShell automatically
- Runs with admin privileges
- Interactive menu
- Easy access

### **Method 3: Silent Mode**

```powershell
# Complete uninstall without prompts
.\scripts\Complete-Uninstaller.ps1 -Silent

# Remove everything including data
.\scripts\Complete-Uninstaller.ps1 -Silent -RemoveData -RemoveShares

# Force mode (skip confirmations)
.\scripts\Complete-Uninstaller.ps1 -Force -RemoveData
```

**Parameters:**
- `-Silent` - No interactive prompts
- `-RemoveData` - Delete data folder
- `-RemoveShares` - Delete backup shares
- `-Force` - Skip all confirmations

---

## ?? **What Gets Removed**

### **Option 1: Uninstall ALL**

**Automatically Removed:**
```
? Windows Service
  - Stops SecureStampService
  - Removes service registration
  
? Program Files
  - C:\Program Files\StampService\
  - C:\Program Files\StampService\AdminGUI\
  - C:\Program Files\StampService\AdminCLI\
  
? Registry Entries
  - HKLM:\SOFTWARE\StampService
  - HKLM:\SOFTWARE\WOW6432Node\StampService
  - HKCU:\SOFTWARE\StampService
  
? Shortcuts
  - Desktop\Stamp Service Admin GUI.lnk
  - Start Menu\Stamp Service Admin GUI\
  - Quick Launch\Stamp Service Admin GUI.lnk
```

**Optionally Removed** (with flags):
```
??  Data Folder (-RemoveData)
  - C:\ProgramData\StampService\
  • master.key (master signing key)
    • Logs\ (all log files)
    • Any other data files
    
??  Backup Shares (-RemoveShares)
  - User-specified folder
  - Contains share-X-of-Y.json files
  - commitment.txt
  - README.txt
```

### **Component-Specific Options**

**Option 2: Service Only**
```
? Stops SecureStampService
? Removes service registration
? Removes C:\Program Files\StampService\
? Preserves AdminGUI
? Preserves AdminCLI
? Preserves data folder
```

**Option 3: AdminGUI Only**
```
? Removes C:\Program Files\StampService\AdminGUI\
? Removes shortcuts
? Preserves Service
? Preserves AdminCLI
? Preserves data folder
```

**Option 4: AdminCLI Only**
```
? Removes C:\Program Files\StampService\AdminCLI\
? Preserves Service
? Preserves AdminGUI
? Preserves data folder
```

**Option 5: Registry Only**
```
? Removes HKLM:\SOFTWARE\StampService
? Removes HKCU:\SOFTWARE\StampService
? Preserves all applications
? Preserves data folder
```

**Option 6: Data Folder Only**
```
??  Removes C:\ProgramData\StampService\
??  Deletes master.key (critical!)
??  Deletes all logs
? Preserves all applications
```

**Option 7: Dry Run**
```
Shows:
• What components are installed
• What will be removed
• File sizes
• Number of items
• Warning for critical files
```

---

## ?? **User Experience**

### **Confirmation Prompts**

**Standard Confirmation:**
```
? Removing data folder...
??  This will delete the master key and all logs. Continue? (Y/N):
```

**Critical Confirmation:**
```
??  CRITICAL ACTION!
This will delete the master key and all logs. Continue?

Type 'YES' in capital letters to confirm:
```

### **Progress Indicators**

```
? Stopping Stamp Service...
  ? Service stopped

? Removing Stamp Service...
  ? Service uninstalled

? Removing program files...
  ? Removed: C:\Program Files\StampService

? Removing registry entries...
  ? Removed: HKLM:\SOFTWARE\StampService

? Removing shortcuts...
  ? Removed: Desktop\Stamp Service Admin GUI.lnk
```

### **Summary Report**

```
========================================
  Uninstallation Complete!
========================================

Summary:
  ? Service removed
  ? Program files removed
  ? Registry entries cleaned
  ? Shortcuts removed
  ??  Data folder preserved
     Location: C:\ProgramData\StampService

The Stamp Service has been completely uninstalled.
```

---

## ?? **Dry Run Example**

```powershell
.\scripts\Complete-Uninstaller.ps1
# Select option 7
```

**Output:**
```
========================================
  Dry Run - What Will Be Removed
========================================

Checking installed components...

Windows Service:
  ? Found: SecureStampService (Status: Running)

Program Files:
  ? Found: C:\Program Files\StampService (45.23 MB)
  ? Found: C:\Program Files\StampService\AdminGUI (23.45 MB)
  ? Found: C:\Program Files\StampService\AdminCLI (8.12 MB)

Data Folder:
  ? Found: C:\ProgramData\StampService (156 items, 12.34 MB)
    ??  Contains master key file
    ?? Contains 45 log files

Registry Entries:
  ? Found: HKLM:\SOFTWARE\StampService

Shortcuts:
  ? Found: Desktop\Stamp Service Admin GUI.lnk
  ? Found: Start Menu\Stamp Service Admin GUI

Press any key to return to menu...
```

---

## ??? **Safety Features**

### **1. Administrator Check**
```powershell
if (-not ([Security.Principal.WindowsPrincipal]...).IsInRole(...Administrator)) {
    Write-Host "? This script requires Administrator privileges!"
    exit 1
}
```

### **2. Confirmation for Critical Actions**
```powershell
$confirm = Get-Confirmation -Message "Delete master key?" -Critical

if (-not $confirm) {
    Write-Warning "Operation cancelled"
    return
}
```

### **3. Error Handling**
```powershell
try {
    Remove-Item -Path $path -Recurse -Force
    Write-Success "Removed: $path"
}
catch {
    Write-Error "Could not remove: $path - $($_.Exception.Message)"
}
```

### **4. Data Preservation by Default**
```
Data folder preserved at: C:\ProgramData\StampService
```

---

## ?? **Integration with Installer**

### **Start Menu Shortcut**

After AdminGUI installation, users see:
```
Start Menu\Stamp Service Admin GUI\
??? Stamp Service Admin GUI
??? Documentation
??? Logs Folder
??? Complete Uninstaller        ? NEW!
??? Uninstall Stamp Service Admin GUI
```

**Complete Uninstaller:**
- Removes ALL components (Service + AdminCLI + AdminGUI)
- Interactive menu
- Safety confirmations

**Standard Uninstaller:**
- Removes AdminGUI only
- Preserves Service and AdminCLI
- Quick uninstall

---

## ?? **Use Cases**

### **Use Case 1: Complete Removal**

**Scenario:** Completely remove Stamp Service from machine

**Steps:**
```powershell
.\scripts\Complete-Uninstaller.ps1
# Select option [1] - Uninstall ALL
# Press [D] to remove data
# Press [S] to remove shares
# Confirm with 'YES'
```

**Result:** Everything removed, clean machine

### **Use Case 2: Upgrade Preparation**

**Scenario:** Remove old version before upgrading

**Steps:**
```powershell
.\scripts\Complete-Uninstaller.ps1
# Select option [1] - Uninstall ALL
# DO NOT remove data folder
# Confirm
```

**Result:** Applications removed, data preserved, ready for new install

### **Use Case 3: AdminGUI Reinstall**

**Scenario:** AdminGUI corrupted, need fresh install

**Steps:**
```powershell
.\scripts\Complete-Uninstaller.ps1
# Select option [3] - Uninstall AdminGUI only
# Reinstall AdminGUI installer
```

**Result:** AdminGUI removed, Service still running, clean reinstall

### **Use Case 4: Clean Development Machine**

**Scenario:** Testing installer, need clean slate

**Steps:**
```powershell
.\scripts\Complete-Uninstaller.ps1 -Silent -RemoveData -RemoveShares -Force
```

**Result:** Instant complete removal, no prompts

---

## ?? **Testing Checklist**

### **Before Distribution**

- [ ] Test interactive menu
- [ ] Test option 1 (Uninstall ALL)
- [ ] Test option 2 (Service only)
- [ ] Test option 3 (AdminGUI only)
- [ ] Test option 4 (AdminCLI only)
- [ ] Test option 5 (Registry only)
- [ ] Test option 6 (Data folder only)
- [ ] Test option 7 (Dry run)
- [ ] Test with -RemoveData flag
- [ ] Test with -RemoveShares flag
- [ ] Test silent mode
- [ ] Test Start Menu shortcut
- [ ] Verify all files removed
- [ ] Verify data folder preserved (when not flagged)
- [ ] Verify service stops correctly
- [ ] Verify shortcuts removed
- [ ] Test on Windows 10
- [ ] Test on Windows 11
- [ ] Test on Windows Server

### **Expected Behavior**

**Complete Uninstall (Option 1):**
```
Before:
- Service running
- AdminGUI installed
- AdminCLI installed
- Registry entries present
- Shortcuts exist
- Data folder exists

After (without -RemoveData):
- Service removed
- AdminGUI removed
- AdminCLI removed
- Registry entries removed
- Shortcuts removed
- Data folder preserved ?

After (with -RemoveData):
- Everything removed
- Clean machine
```

---

## ?? **Distribution**

### **Included in Installer**

The Complete-Uninstaller.ps1 is automatically included in:

**AdminGUI Installer:**
```
StampService-AdminGUI-Setup-1.0.0.exe
??? Includes: Scripts\Complete-Uninstaller.ps1
```

**Complete Distribution ZIP:**
```
StampService-Distribution-1.0.0.zip
??? Scripts\Complete-Uninstaller.ps1
```

### **Access Methods**

**For End Users:**
1. Start Menu ? Stamp Service Admin GUI ? Complete Uninstaller
2. Run PowerShell as Admin ? Navigate to Scripts folder ? Run script

**For IT Professionals:**
```powershell
# Silent deployment removal
.\Complete-Uninstaller.ps1 -Silent -RemoveData
```

---

## ?? **Summary**

**The Complete Uninstaller provides:**

? **Interactive Menu** - Easy selection of components  
? **Selective Removal** - Choose what to remove  
? **Safety Confirmations** - Prevent accidental deletion  
? **Dry Run Mode** - Preview before removal  
? **Data Preservation** - Keep logs and keys by default  
? **Share Protection** - Manual folder selection  
? **Silent Mode** - Scriptable uninstallation  
? **Start Menu Integration** - Easy access  
? **Comprehensive Logging** - See what's happening  
? **Error Handling** - Graceful failures  

**Perfect for:**
- Complete removal
- Upgrade preparation
- Component-specific uninstall
- Development testing
- IT automation
- Clean machine setup

---

## ?? **Files Created**

| File | Purpose |
|------|---------|
| `scripts/Complete-Uninstaller.ps1` | Main uninstaller script |
| `Resources/UNINSTALLER-COMPLETE.md` | This documentation |

### **Modified Files**

| File | Change |
|------|--------|
| `scripts/AdminGUI-Installer.iss` | Added Start Menu shortcut |
| `scripts/AdminGUI-Installer.iss` | Included uninstaller in Files |

---

**Your users now have a professional, safe, comprehensive uninstaller!** ???

---

## ?? **Related Documentation**

- `scripts/INSTALLER-README.md` - Installer guide
- `Resources/INSTALLER-IMPLEMENTATION-COMPLETE.md` - Installer details
- `Resources/ADMINGUI-PROJECT-COMPLETE.md` - Complete project summary

---

**Complete uninstallation capability implemented!** ??
