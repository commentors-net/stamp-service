# ?? Complete Distribution Installer - Quick Reference

## ? What's Included

- **Unified installer** with all components:
  - Windows Service
- AdminCLI
  - AdminGUI
- Desktop shortcut creation
- Start Menu integration
- Documentation packaging
- Complete uninstaller

---

## ?? Build Commands

### Build Unified Installer + ZIP
```powershell
.\scripts\Build-Complete-Distribution.ps1 -IncludeAdminGUI
```

### Build ZIP Only (Service + AdminCLI)
```powershell
.\scripts\Build-Distribution.ps1
```

### Custom Version
```powershell
.\scripts\Build-Complete-Distribution.ps1 -Version "1.2.3" -IncludeAdminGUI
```

---

## ?? Output

**Files Created:**
```
1. StampService-Distribution-1.0.0-[timestamp].zip (21 MB)
   - ZIP package with all components
   
2. StampService-Complete-Setup-1.0.0.exe (19 MB)
   - Unified installer that installs everything
```

---

## ?? Installation

**For End Users:**
```
1. Run StampService-Complete-Setup-1.0.0.exe as Administrator
2. Follow wizard
3. Choose components:
   - Full (Service + AdminCLI + AdminGUI) [Default]
 - Server (Service + AdminCLI only)
   - Custom (select components)
4. Service installs automatically
5. Launch AdminGUI from Desktop or Start Menu
```

**Silent Install:**
```cmd
StampService-Complete-Setup-1.0.0.exe /SILENT /COMPONENTS="service,admincli,admingui"
```

---

## ?? Installed Locations

**Service:**
```
C:\Program Files\StampService\Service\
```

**AdminCLI:**
```
C:\Program Files\StampService\AdminCLI\
```

**AdminGUI:**
```
C:\Program Files\StampService\AdminGUI\
```

**Data:**
```
C:\ProgramData\StampService\
```

**Shortcuts:**
```
??? Desktop: Stamp Service Admin
?? Start Menu\Stamp Service\
   ??? Admin GUI
   ??? Service\
   ? ??? Service Status
   ?   ??? Test Stamp
   ?   ??? View Logs
   ??? Documentation
   ??? Complete Uninstaller
   ??? Uninstall
```

---

## ?? Features

**Unified Installer:**
? Installs all components together  
? Windows Service installation  
? Service auto-start configuration  
? Desktop shortcut (AdminGUI)  
? Start Menu folder structure  
? Documentation included  
? Complete uninstaller  
? Silent mode support  
? Component selection  

---

## ?? Prerequisites

**To Build:**
- Inno Setup 6
- .NET 8 SDK

**To Install:**
- Windows 10/11 or Server
- Administrator privileges

---

## ?? Result

**Two Distribution Options:**

1. **ZIP Package** (for IT pros)
   - Extract and use scripts
   - Manual installation
   - Size: ~21 MB

2. **Unified Installer** (for end users)
   - One-click installation
   - All components together
   - Size: ~19 MB
   - Professional wizard

---

## ?? What Gets Installed

### Full Installation:
```
? Windows Service (SecureStampService)
   - Installed and started automatically
   - Runs as LocalSystem
   
? AdminCLI
   - Command-line administration
   - Available at: C:\Program Files\StampService\AdminCLI\
   
? AdminGUI
   - Desktop administration interface
   - Desktop shortcut created
   - Start Menu shortcuts created
   
? Scripts
   - Installation scripts
   - Complete uninstaller
   
? Documentation
   - Quick start guide
   - User manual
   - Integration guide
```

---

## ??? Uninstallation

**Option 1: Start Menu**
```
Start Menu ? Stamp Service ? Complete Uninstaller
```

**Option 2: Standard Uninstaller**
```
Settings ? Apps ? Stamp Service ? Uninstall
```

**What Gets Removed:**
- ? Windows Service (stopped and removed)
- ? All program files
- ? Desktop shortcuts
- ? Start Menu shortcuts
- ?? Data folder preserved (optional removal)

---

See `scripts/Complete-Installer.iss` for installer configuration.
