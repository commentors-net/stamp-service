# Secure Stamp Service - Distribution Guide

## Package Contents

This distribution contains everything needed to install and run the Secure Stamp Service on Windows.

### Directory Structure

```
StampService-Distribution/
??? StampService/      # Windows Service files
?   ??? StampService.exe       # Main service executable
?   ??? StampService.dll       # Service assembly
?   ??? StampService.Core.dll  # Core functionality
?   ??? StampService.ClientLib.dll  # Client library for integration
?   ??? appsettings.json     # Configuration file
?   ??? [dependencies]  # Required DLLs
??? AdminCLI/         # Administration tool
?   ??? StampService.AdminCLI.exe
?   ??? [dependencies]
??? Scripts/             # Installation scripts
?   ??? Install-StampService.ps1
???? Uninstall-StampService.ps1
?   ??? [other scripts]
??? README.md        # Full documentation
??? QUICKSTART.md       # Quick start guide
??? DISTRIBUTION-README.txt    # This file
??? version.json        # Build information
```

## System Requirements

- **Operating System**: Windows 10/11 or Windows Server 2019+
- **Framework**: .NET 8 Runtime (included in service executable)
- **Privileges**: Administrator rights for installation
- **Disk Space**: ~50 MB for installation
- **Memory**: Minimum 100 MB RAM for service
- **Network**: Local-only (Named Pipes) - no network ports required

## Installation Instructions

### Step 1: Extract Package

Extract the distribution ZIP file to a temporary location (e.g., `C:\Temp\StampService`).

### Step 2: Open PowerShell as Administrator

1. Press `Windows + X`
2. Select "Windows PowerShell (Admin)" or "Terminal (Admin)"
3. Navigate to the Scripts directory:
   ```powershell
   cd C:\Temp\StampService\Scripts
   ```

### Step 3: Run Installer

```powershell
.\Install-StampService.ps1
```

**The installer will automatically:**
- ? Detect the distribution package structure
- ? Copy files to `C:\Program Files\StampService`
- ? Create secure data directory at `C:\ProgramData\StampService`
- ? Set correct permissions with inheritance enabled
- ? Configure paths in `appsettings.json`
- ? Install Windows Service
- ? Start the service
- ? Generate master signing key

**Optional Parameters:**
```powershell
# Custom installation path
.\Install-StampService.ps1 -InstallPath "D:\Services\StampService"

# Custom data path
.\Install-StampService.ps1 -DataPath "D:\ProgramData\StampService"

# Custom service name
.\Install-StampService.ps1 -ServiceName "MyStampService"

# Combined example
.\Install-StampService.ps1 `
    -InstallPath "D:\Services\StampService" `
  -DataPath "D:\ProgramData\StampService" `
    -ServiceName "SecureStampService"
```

### Step 4: Verify Installation

```powershell
# Check service status
Get-Service SecureStampService

# Navigate to install directory (AdminCLI is copied here for convenience)
cd "C:\Program Files\StampService"

# Test with AdminCLI
.\StampService.AdminCLI.exe status
.\StampService.AdminCLI.exe test-stamp
```

**Expected Output:**
```
=== StampService Status ===
Running: True
Key Present: True
Uptime: 00:00:15
Algorithm: Ed25519

=== Test Stamp Response ===
? Signature Valid: True
```

## Post-Installation Tasks

### 1. Create Backup Shares (CRITICAL!)

**This step is mandatory before using the service in production!**

```powershell
cd "C:\Program Files\StampService"
.\StampService.AdminCLI.exe create-shares --total 5 --threshold 3 --output C:\Shares
```

**What this does:**
- Creates 5 shares using Shamir's Secret Sharing
- Requires 3 shares to recover the key
- Saves shares to `C:\Shares\share-1-of-5.json` through `share-5-of-5.json`
- Creates `C:\Shares\commitment.txt` for verification

**Output:**
```
? Shares created successfully!

Commitment: Q+HcasmNH9WeZchAeMslX6s4acbQBke2P5TMIlZGErM=
Algorithm: Ed25519

Share 1 saved to: C:\Shares\share-1-of-5.json
Share 2 saved to: C:\Shares\share-2-of-5.json
...

??  IMPORTANT: Distribute shares to trusted custodians securely!
```

### 2. Distribute Shares Securely

**Best Practices:**
- ? Give each share to a **different** trusted custodian
- ? Store shares **offline** (USB drive, paper printout, secure safe)
- ? Each custodian should verify their share:
  ```powershell
  .\StampService.AdminCLI.exe verify-share C:\Shares\share-1-of-5.json
  ```
- ? Document who has which share
- ? Store commitment hash separately for verification
- ? **Never** store all shares together
- ? **Never** email shares or store in cloud without encryption

### 3. Document Share Holders

Maintain a secure record of:
- Who has each share (Share 1: John Doe, Share 2: Jane Smith, etc.)
- Contact information for each custodian
- Physical location where shares are stored
- Date shares were created

### 4. Save Public Key

The public key can be safely distributed to client applications:

```powershell
.\StampService.AdminCLI.exe status > public-key.txt
```

## Client Integration

### Option 1: Using NuGet Package (Recommended)

```powershell
# Add NuGet package to your project
dotnet add package StampService.ClientLib --version 1.0.0
```

```csharp
using StampService.ClientLib;
using StampService.Core.Models;

// Create client
using var client = new StampServiceClient();

// Sign a request
var request = new SignRequest
{
    Operation = "mint",
    RequesterId = "MyApp",
    Payload = new Dictionary<string, object>
    {
        ["recipient"] = "user123",
        ["amount"] = 1000
    }
};

var response = await client.SignAsync(request);

// Verify signature (optional, locally)
bool isValid = client.VerifySignature(response);

Console.WriteLine($"Signature: {response.Signature}");
Console.WriteLine($"Valid: {isValid}");
```

See [CLIENT-INTEGRATION.md](CLIENT-INTEGRATION.md) for detailed integration guide.

### Option 2: Direct DLL Reference

In your .NET application, add a reference to:
```
C:\Program Files\StampService\StampService.ClientLib.dll
```

## Configuration

### Service Configuration

The installer automatically configures `appsettings.json` with correct paths based on your installation:

**Default Installation** (`C:\Program Files\StampService\appsettings.json`):
```json
{
  "ServiceConfiguration": {
    "PipeName": "StampServicePipe",
    "KeyStorePath": "C:\\ProgramData\\StampService\\master.key",
 "AuditLogPath": "C:\\ProgramData\\StampService\\Logs\\audit.log"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

**Custom Installation** (paths auto-updated by installer):
If you used `-InstallPath "D:\Services\StampService"` and `-DataPath "D:\Data\StampService"`, the installer automatically updates paths to match.

**Important**: Restart service after manual configuration changes:
```powershell
Restart-Service SecureStampService
```

### Client Configuration

```csharp
// Default pipe name (matches service)
var client = new StampServiceClient();

// Custom pipe name (must match service configuration)
var client = new StampServiceClient("CustomPipeName");

// Custom timeout (in milliseconds)
var client = new StampServiceClient("StampServicePipe", timeoutMs: 10000);
```

## Operations

### Daily Operations

```powershell
# Check service status
Get-Service SecureStampService

# View service logs
Get-Content "C:\ProgramData\StampService\Logs\service*.log" -Tail 50

# View audit logs (all sign operations)
Get-Content "C:\ProgramData\StampService\Logs\audit*.log" -Tail 50

# Test service health
cd "C:\Program Files\StampService"
.\StampService.AdminCLI.exe test-stamp

# Service management
Restart-Service SecureStampService
Stop-Service SecureStampService
Start-Service SecureStampService
```

### Key Recovery Process

**In case of disaster, you can recover the key on a new machine:**

#### Step 1: Install Service on New Machine

```powershell
# Extract distribution ZIP on new machine
cd C:\Temp\StampService\Scripts
.\Install-StampService.ps1
```

The service will start **without a key** (keyless state).

#### Step 2: Start Recovery Session

```powershell
cd "C:\Program Files\StampService"
.\StampService.AdminCLI.exe recover start --threshold 3
```

**Output:**
```
? Recovery session started
Status: Recovery mode activated. Provide shares.
Shares needed: 3
```

#### Step 3: Gather Custodians

Contact the share holders and have them provide their share files.

#### Step 4: Add Shares

```powershell
.\StampService.AdminCLI.exe recover add-share C:\path\to\share-1-of-5.json
# Output: ? Share accepted. Need 2 more.

.\StampService.AdminCLI.exe recover add-share C:\path\to\share-3-of-5.json
# Output: ? Share accepted. Need 1 more.

.\StampService.AdminCLI.exe recover add-share C:\path\to\share-5-of-5.json
# Output: ? Recovery successful. Key restored.
```

After threshold shares are provided (3 in this example), the key is **automatically reconstructed** and securely stored.

#### Step 5: Verify Recovery

```powershell
.\StampService.AdminCLI.exe status
# Should show: Key Present: True

.\StampService.AdminCLI.exe test-stamp
# Should show: ? Signature Valid: True
```

**Important**: The public key should **match** the original installation! Compare with saved `public-key.txt`.

## Uninstallation

### Remove Service Only (Keep Data)

```powershell
cd C:\Temp\StampService\Scripts
.\Uninstall-StampService.ps1
```

This removes:
- ? Windows Service registration
- ? Files in `C:\Program Files\StampService`

This **preserves**:
- ? Master key file
- ? Logs
- ? Data in `C:\ProgramData\StampService`

### Remove Service AND Data

```powershell
.\Uninstall-StampService.ps1 -RemoveData
```

**?? WARNING**: The `-RemoveData` flag will **permanently delete**:
- ? Master key file
- ? All logs
- ? All configuration
- ? All data in `C:\ProgramData\StampService`

**Before using `-RemoveData`:**
1. ? Ensure you have valid backup shares
2. ? Test share verification
3. ? Document share holder locations
4. ? Export public key if needed

## Troubleshooting

### Service Won't Start

**1. Check Windows Event Viewer:**
   - Open Event Viewer (`eventvwr.msc`)
   - Navigate to: Windows Logs ? Application
 - Look for errors from "SecureStampService"

**2. Check Service Logs:**
```powershell
Get-Content "C:\ProgramData\StampService\Logs\service*.log" -Tail 100
```

Common errors:
- `Access denied` ? **Fixed**: Installer now sets correct permissions with inheritance
- `File not found` ? Check paths in `appsettings.json`

**3. Verify .NET Runtime:**
```powershell
dotnet --list-runtimes
```
Should show: `Microsoft.NETCore.App 8.0.x` or higher

**4. Check Permissions:**
```powershell
# Verify SYSTEM has access to data directory
icacls "C:\ProgramData\StampService"
```
Should show: `NT AUTHORITY\SYSTEM:(OI)(CI)(F)`

**5. Manual Start (for debugging):**
```powershell
Stop-Service SecureStampService
cd "C:\Program Files\StampService"
.\StampService.exe --console
```

This runs the service in console mode with visible output.

### Client Can't Connect

**1. Verify Service is Running:**
```powershell
Get-Service SecureStampService
# Should show: Status = Running
```

**2. Check Named Pipe:**
- Named Pipe should be: `\\.\pipe\StampServicePipe`
- Verify client uses correct pipe name
- Named Pipes are **local-only** (no network configuration needed)

**3. Test with AdminCLI:**
```powershell
cd "C:\Program Files\StampService"
.\StampService.AdminCLI.exe status
```

If AdminCLI works but your client doesn't:
- ? Ensure you're using `StampService.ClientLib` version 1.0.0 or later
- ? Check that `ReadMode = PipeTransmissionMode.Message` is set (fixed in v1.0.0)
- ? Verify timeout settings (increase if needed)

**4. Check Firewall:**
- Named Pipes don't require firewall rules
- If using custom configuration, ensure Windows Firewall isn't blocking the service

### Test Failures

If `test-stamp` fails:

**1. Check Key is Present:**
```powershell
.\StampService.AdminCLI.exe status
```
Should show: `Key Present: True`

**2. Check Audit Logs:**
```powershell
Get-Content "C:\ProgramData\StampService\Logs\audit*.log" -Tail 50
```

**3. Restart Service:**
```powershell
Restart-Service SecureStampService
Start-Sleep -Seconds 5
cd "C:\Program Files\StampService"
.\StampService.AdminCLI.exe test-stamp
```

**4. Verify Key File Exists:**
```powershell
Test-Path "C:\ProgramData\StampService\master.key"
# Should return: True
```

### Performance Issues

**Symptoms:**
- Slow response times
- Timeout errors
- High CPU usage

**Solutions:**

1. **Check System Resources:**
```powershell
Get-Process StampService | Select-Object CPU, WorkingSet
```

2. **Review Audit Logs for Excessive Requests:**
```powershell
Get-Content "C:\ProgramData\StampService\Logs\audit*.log" | Measure-Object
```

3. **Increase Client Timeout:**
```csharp
var client = new StampServiceClient(timeoutMs: 30000); // 30 seconds
```

4. **Monitor Service Health:**
```powershell
# Add to scheduled task (daily)
$status = & "C:\Program Files\StampService\StampService.AdminCLI.exe" status
if ($status -notmatch "Running: True") {
    # Send alert
}
```

## Security Best Practices

### Share Management

? **DO:**
- Create shares immediately after installation
- Distribute to 5+ trusted individuals
- Store offline (not in cloud or network drives)
- Each custodian verifies their share
- Document who has which share
- Test recovery process annually
- Store commitment hash separately

? **DON'T:**
- Store all shares together
- Email shares without encryption
- Store shares in plain text on network
- Give multiple shares to one person
- Lose track of share holders
- Store commitment with shares

### Service Security

? **DO:**
- Run service under dedicated service account
- Restrict access to service machine
- Monitor audit logs regularly
- Set up alerting on service failures
- Keep system updated and patched
- Perform regular health checks
- Use strong access controls

? **DON'T:**
- Copy `master.key` file as backup (use shares!)
- Expose service to network
- Grant unnecessary users access
- Ignore audit log warnings
- Run service with elevated privileges unnecessarily

### Operational Security

? **DO:**
- Test recovery process in non-production
- Have documented recovery procedures
- Maintain contact list of share holders
- Rotate shares if compromised
- Review logs for unusual activity
- Monitor service health daily
- Backup audit logs regularly

? **DON'T:**
- Skip backup share creation
- Store commitment hash with shares
- Allow automated recovery
- Ignore security events in logs
- Deploy without testing

## Important File Locations

| Item | Default Path | Purpose |
|------|--------------|---------|
| **Service Executable** | `C:\Program Files\StampService\` | Service binaries |
| **AdminCLI** | `C:\Program Files\StampService\` | Administration tool (copied for convenience) |
| **Master Key** | `C:\ProgramData\StampService\master.key` | Encrypted private key (DPAPI) |
| **Service Logs** | `C:\ProgramData\StampService\Logs\service-*.log` | Service operation logs |
| **Audit Logs** | `C:\ProgramData\StampService\Logs\audit-*.log` | All sign operations |
| **Configuration** | `C:\Program Files\StampService\appsettings.json` | Service configuration |
| **Backup Shares** | User-defined (offline storage) | Shamir shares for recovery |

**Custom Installation Paths:**
If you used custom paths during installation, files will be at:
- Service: `<InstallPath>\StampService.exe`
- Data: `<DataPath>\master.key`
- Logs: `<DataPath>\Logs\`

## Support

### Documentation

- **Full README**: See `README.md` for complete technical documentation
- **Quick Start**: See `QUICKSTART.md` for step-by-step guide
- **Client Examples**: See `CLIENT-INTEGRATION.md` for integration guide
- **NuGet Guide**: See `NUGET-PUBLISHING.md` for package distribution

### Logs

View logs for troubleshooting:

```powershell
# Service logs (operational)
Get-Content "C:\ProgramData\StampService\Logs\service-YYYYMMDD.log" -Tail 100

# Audit logs (all sign operations - no sensitive data)
Get-Content "C:\ProgramData\StampService\Logs\audit-YYYYMMDD.log" -Tail 100

# Filter for errors
Get-Content "C:\ProgramData\StampService\Logs\service*.log" | Select-String "ERROR"
```

### AdminCLI Commands Reference

| Command | Description | Example |
|---------|-------------|---------|
| `status` | Get service status | `.\StampService.AdminCLI.exe status` |
| `test-stamp` | Health check signature | `.\StampService.AdminCLI.exe test-stamp` |
| `create-shares` | Create backup shares | `.\StampService.AdminCLI.exe create-shares --total 5 --threshold 3 --output C:\Shares` |
| `verify-share` | Verify a share file | `.\StampService.AdminCLI.exe verify-share share-1-of-5.json` |
| `recover start` | Start recovery session | `.\StampService.AdminCLI.exe recover start --threshold 3` |
| `recover add-share` | Add share to recovery | `.\StampService.AdminCLI.exe recover add-share share-1-of-5.json` |
| `recover status` | Check recovery status | `.\StampService.AdminCLI.exe recover status` |

### Contact

For issues and questions:
- Check documentation in `Resources/` folder
- Review logs at `C:\ProgramData\StampService\Logs\`
- Contact your system administrator
- Refer to internal documentation

## Version Information

- **Build Date**: See `version.json` in distribution package
- **Configuration**: Release
- **Target**: Windows x64
- **Framework**: .NET 8
- **Algorithm**: Ed25519 (EdDSA)
- **Cryptography**: BouncyCastle

## What's New

### Version 1.0.0 (Production Ready)

**Fixes:**
- ? **Permission Inheritance**: Service can now create files without "Access Denied" errors
- ? **Named Pipe Mode**: Client-server communication mode mismatch resolved
- ? **Path Detection**: Installer auto-detects distribution package vs development repo structure
- ? **Configuration Auto-Update**: Paths automatically configured based on installation location
- ? **AdminCLI Convenience**: AdminCLI now copied to install directory for easy access

**Features:**
- ? Master key generation and secure storage (Windows DPAPI)
- ? Ed25519 cryptographic signatures (~1-2ms per operation)
- ? Shamir's Secret Sharing backup (configurable threshold)
- ? Key recovery from distributed shares
- ? Named Pipe IPC (local-only, secure)
- ? Comprehensive audit logging (no sensitive data exposure)
- ? Windows Service with auto-restart
- ? Full AdminCLI toolset
- ? NuGet packages for client integration

## License

See LICENSE file (if included) or check with your organization's legal department.

---

## Quick Reference

### Installation
```powershell
cd ExtractedFolder\Scripts
.\Install-StampService.ps1
```

### Create Shares
```powershell
cd "C:\Program Files\StampService"
.\StampService.AdminCLI.exe create-shares --total 5 --threshold 3 --output C:\Shares
```

### Daily Health Check
```powershell
cd "C:\Program Files\StampService"
.\StampService.AdminCLI.exe status
.\StampService.AdminCLI.exe test-stamp
```

### Recovery
```powershell
.\StampService.AdminCLI.exe recover start --threshold 3
.\StampService.AdminCLI.exe recover add-share share-1-of-5.json
# Repeat for threshold shares...
```

---

**?? Critical Reminder**: The private key **NEVER** leaves the service. It cannot be exported or viewed. The **only** way to backup/recover the key is through Shamir Secret Sharing. **Create your backup shares immediately** after installation!

**For detailed client integration examples and patterns, see [CLIENT-INTEGRATION.md](CLIENT-INTEGRATION.md).**

