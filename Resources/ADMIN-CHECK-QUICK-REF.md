# AdminGUI - Quick Developer Reference

## Running Modes

| Mode | Command | Admin Required? | Use Case |
|------|---------|----------------|----------|
| **Debug** | F5 in VS | ? No | Development |
| **Release** | Ctrl+F5 in VS | ? Yes | Testing |
| **Distribution** | Build script | ? Yes | End users |

---

## Build Commands

```powershell
# Debug (no admin required)
dotnet build src\StampService.AdminGUI\StampService.AdminGUI.csproj -c Debug

# Release (admin required)
dotnet build src\StampService.AdminGUI\StampService.AdminGUI.csproj -c Release

# Run Debug
.\src\StampService.AdminGUI\bin\Debug\net8.0-windows\StampService.AdminGUI.exe

# Run Release (will prompt for admin)
.\src\StampService.AdminGUI\bin\Release\net8.0-windows\StampService.AdminGUI.exe
```

---

## What You'll See

### Debug Mode (F5)
```
?? Debug Mode - Running Without Admin Privileges

The application is running in Debug mode without 
administrator privileges.
Some features may not work correctly.

In Release mode, administrator privileges will be required.

[OK]
```

### Release Mode
```
?? Administrator Privileges Required

This application requires administrator privileges 
to manage the Stamp Service.

Would you like to restart with administrator privileges?

[Yes] [No]
```

---

## Footer Status Messages

| Configuration | With Admin | Without Admin |
|--------------|------------|---------------|
| **Debug** | `? Debug Mode - Running with admin (optional)` | `?? Debug Mode - Running without admin (some features may not work)` |
| **Release** | `? Running with administrator privileges` | `?? Administrator privileges required but not present!` |

---

## Key Features

? **Debug Mode**
- No UAC prompts
- Instant F5 debugging
- Info message (can dismiss)
- Most features work

? **Release Mode**
- Requires admin
- UAC prompt shown
- Secure operation
- All features work

---

## Distribution

```powershell
# Build distribution (uses Release)
.\scripts\Build-Distribution.ps1

# Result:
# - AdminGUI built in Release mode
# - Requires admin when run
# - UAC prompt for end users
# - Professional deployment
```

---

## Troubleshooting

**Problem**: "Service not accessible" in Debug mode  
**Solution**: Service requires admin to connect. Run VS as admin or use Release build.

**Problem**: No UAC prompt in Release  
**Solution**: Already running as admin! Check footer status.

**Problem**: UAC keeps appearing  
**Solution**: You're in Release mode. Use Debug for development (F5).

---

## Quick Tips

?? **For Development**: Always use Debug mode (F5)  
?? **For Testing**: Use Release mode before distribution  
?? **For Distribution**: `Build-Distribution.ps1` handles it  
?? **Check Status**: Look at footer for privilege state  

---

See `Resources/CONDITIONAL-ADMIN-CHECK.md` for full details.
