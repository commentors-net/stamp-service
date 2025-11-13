# AdminGUI Installer - Information

## What This Installer Does

The **Stamp Service Admin GUI Installer** provides a professional installation experience for the AdminGUI application.

### Features

? **Single-File Installer** - One `.exe` file for easy distribution  
? **Desktop Shortcut** - Automatically creates desktop icon  
? **Start Menu Integration** - Adds to Start Menu with folder  
? **Quick Launch** - Optional quick launch shortcut  
? **Professional Branding** - Modern wizard interface  
? **Uninstaller** - Complete uninstall capability  
? **Prerequisites Check** - Verifies Stamp Service installation  
? **Admin Check** - Requires Administrator privileges  
? **Documentation** - Includes all relevant docs  

---

## Installation Locations

### Application Files
```
C:\Program Files\StampService\AdminGUI\
??? StampService.AdminGUI.exe
??? StampService.ClientLib.dll
??? StampService.Core.dll
??? [Other dependencies]
??? README.md
??? Docs\
    ??? QUICKSTART.md
    ??? ADMINGUI-COMPLETE-SUMMARY.md
    ??? [Other documentation]
```

### Shortcuts Created

**Desktop:**
```
Desktop\Stamp Service Admin GUI.lnk ? Run as Administrator
```

**Start Menu:**
```
Start Menu\Stamp Service Admin GUI\
??? Stamp Service Admin GUI.lnk
??? Documentation.lnk
??? Logs Folder.lnk
??? Uninstall.lnk
```

**Quick Launch (Optional):**
```
Quick Launch\Stamp Service Admin GUI.lnk
```

---

## Building the Installer

### Prerequisites

1. **Inno Setup 6** - Download from: https://jrsoftware.org/isdl.php
2. **.NET 8 SDK** - For building the AdminGUI
3. **Administrator privileges** - Required for installation

### Build Steps

#### Option 1: PowerShell Script (Recommended)

```powershell
# Build with default settings (Release, version 1.0.0)
.\scripts\Build-AdminGUI-Installer.ps1

# Build with custom version
.\scripts\Build-AdminGUI-Installer.ps1 -Version "1.2.3"

# Build Debug version
.\scripts\Build-AdminGUI-Installer.ps1 -Configuration Debug
```

**Script automatically:**
- ? Cleans previous builds
- ? Builds AdminGUI in Release mode
- ? Copies documentation
- ? Updates version in Inno Script
- ? Compiles installer with Inno Setup
- ? Verifies output

#### Option 2: Manual Build

```powershell
# 1. Build AdminGUI
cd src\StampService.AdminGUI
dotnet build -c Release

# 2. Open Inno Setup Compiler
# 3. Open: scripts\AdminGUI-Installer.iss
# 4. Click Build ? Compile
# 5. Installer created in root directory
```

---

## Installer Output

### File Created
```
StampService-AdminGUI-Setup-1.0.0.exe
```

**Size:** ~15-25 MB (compressed)  
**Format:** Windows executable installer  
**Architecture:** x64 only  

---

## Installation Process

### For End Users

1. **Download** the installer: `StampService-AdminGUI-Setup-1.0.0.exe`
2. **Right-click** the installer ? **Run as Administrator**
3. **Follow** the installation wizard:
   - Accept license agreement
   - Choose installation directory (default: `C:\Program Files\StampService\AdminGUI`)
   - Select tasks:
     - ? Create Desktop Icon (recommended)
     - ? Create Quick Launch Icon (optional)
   - Click **Install**
4. **Wait** for installation to complete
5. **Launch** the AdminGUI (checkbox on finish page)

### Installation Notes

?? **Administrator Required**: The installer requires Administrator privileges.

?? **Stamp Service**: The Admin GUI requires the Stamp Service to be installed and running. If the service is not detected, a warning will be shown (but installation continues).

? **Unattended Install**: For silent installation, use:
```cmd
StampService-AdminGUI-Setup-1.0.0.exe /SILENT
```

? **Very Silent Install** (no UI):
```cmd
StampService-AdminGUI-Setup-1.0.0.exe /VERYSILENT
```

---

## Uninstallation

### Option 1: Start Menu
```
Start Menu ? Stamp Service Admin GUI ? Uninstall
```

### Option 2: Control Panel
```
Settings ? Apps ? Stamp Service Admin GUI ? Uninstall
```

### Option 3: Command Line
```cmd
"C:\Program Files\StampService\AdminGUI\unins000.exe" /SILENT
```

**What Gets Removed:**
- ? All application files
- ? Desktop shortcut
- ? Start Menu shortcuts
- ? Quick Launch shortcut (if created)
- ? Registry entries

**What Stays:**
- ? Stamp Service (not removed)
- ? Service logs at `C:\ProgramData\StampService\Logs`
- ? User settings (if any)

---

## Customization

### Change Application Info

Edit `scripts/AdminGUI-Installer.iss`:

```pascal
#define MyAppName "Your App Name"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Your Organization"
#define MyAppURL "https://your-url.com"
```

### Change Installation Directory

Edit `scripts/AdminGUI-Installer.iss`:

```pascal
DefaultDirName={autopf}\YourCompany\YourApp
```

### Add Custom Icons

1. Create icon file: `src/StampService.AdminGUI/Resources/icon.ico`
2. Update installer script:
   ```pascal
   SetupIconFile=..\src\StampService.AdminGUI\Resources\icon.ico
   ```

### Add More Documentation

Edit `scripts/Build-AdminGUI-Installer.ps1`:

```powershell
$DocFiles = @(
    "QUICKSTART.md",
    "YOUR-CUSTOM-DOC.md",  # Add here
    "ANOTHER-DOC.md"
)
```

---

## Distribution

### Code Signing (Recommended)

**For production distribution, sign the installer:**

```cmd
signtool sign /f "your-certificate.pfx" /p "password" /t http://timestamp.digicert.com "StampService-AdminGUI-Setup-1.0.0.exe"
```

**Benefits:**
- ? Removes "Unknown Publisher" warning
- ? Builds user trust
- ? Required for some corporate environments

### Distribution Methods

**Internal (Corporate):**
- Network share: `\\fileserver\software\StampService\`
- SCCM/Intune deployment
- Group Policy software installation

**External:**
- GitHub Releases
- Company website
- Direct download link

**Portable Version:**
The installer is not required. You can also distribute as a ZIP file:
```
StampService-AdminGUI-Portable-1.0.0.zip
```

---

## Troubleshooting

### "Inno Setup not found"
- Install Inno Setup 6 from: https://jrsoftware.org/isdl.php
- Verify installation: `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`

### "Build failed"
- Check .NET 8 SDK is installed: `dotnet --version`
- Ensure no other process is using the output directory
- Run PowerShell as Administrator

### "Installer won't run"
- Right-click installer ? Properties ? Unblock
- Run as Administrator
- Check Windows SmartScreen settings

### "Service not detected warning"
- This is informational only
- Install the Stamp Service separately
- AdminGUI can still connect to remote services

---

## Advanced Options

### Silent Installation with Logging

```cmd
StampService-AdminGUI-Setup-1.0.0.exe /VERYSILENT /LOG="C:\Temp\install-log.txt"
```

### Custom Installation Directory

```cmd
StampService-AdminGUI-Setup-1.0.0.exe /DIR="D:\CustomPath\AdminGUI"
```

### Skip Desktop Icon

```cmd
StampService-AdminGUI-Setup-1.0.0.exe /TASKS="!desktopicon"
```

### Force Desktop Icon

```cmd
StampService-AdminGUI-Setup-1.0.0.exe /TASKS="desktopicon"
```

---

## File Structure

```
scripts/
??? AdminGUI-Installer.iss          # Inno Setup script
??? Build-AdminGUI-Installer.ps1    # Build automation
??? INSTALLER-README.md # This file

Output:
??? StampService-AdminGUI-Setup-1.0.0.exe  # Installer (root)
??? src/StampService.AdminGUI/bin/Release/net8.0-windows/
    ??? StampService.AdminGUI.exe
    ??? [Dependencies]
  ??? README.md
    ??? Docs/
        ??? [Documentation]
```

---

## Version History

### 1.0.0 (Initial Release)
- ? Complete AdminGUI installer
- ? Desktop and Start Menu shortcuts
- ? Documentation included
- ? Professional wizard interface
- ? Uninstaller support
- ? Service detection
- ? Admin privilege check

---

## Support

For issues or questions:
- Check documentation in `Docs` folder
- Review installer log: `%TEMP%\Setup Log YYYY-MM-DD #NNN.txt`
- Contact your system administrator
- Open GitHub issue: https://github.com/commentors-net/stamp-service/issues

---

## License

See LICENSE.txt in the installation directory.

---

**Ready to distribute your AdminGUI professionally!** ??
