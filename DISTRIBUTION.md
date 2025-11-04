# Secure Stamp Service - Distribution Guide

## Package Contents

This distribution contains everything needed to install and run the Secure Stamp Service on Windows.

### Directory Structure

```
StampService-Distribution/
??? StampService/              # Windows Service files
?   ??? StampService.exe       # Main service executable
?   ??? StampService.dll       # Service assembly
?   ??? StampService.Core.dll  # Core functionality
?   ??? StampService.ClientLib.dll  # Client library for integration
?   ??? appsettings.json       # Configuration file
?   ??? [dependencies]         # Required DLLs
??? AdminCLI/                  # Administration tool
?   ??? StampService.AdminCLI.exe
?   ??? [dependencies]
??? Scripts/                   # Installation scripts
?   ??? Install-StampService.ps1
?   ??? Uninstall-StampService.ps1
?   ??? Build-Distribution.ps1
??? README.md                  # Full documentation
??? QUICKSTART.md              # Quick start guide
??? DISTRIBUTION-README.txt    # This file
??? version.json               # Build information
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

**Optional Parameters:**
```powershell
# Custom installation path
.\Install-StampService.ps1 -InstallPath "D:\Services\StampService"

# Custom data path
.\Install-StampService.ps1 -DataPath "D:\ProgramData\StampService"

# Custom service name
.\Install-StampService.ps1 -ServiceName "MyStampService"
```

### Step 4: Verify Installation

```powershell
# Check service status
Get-Service SecureStampService

# Test with AdminCLI
cd ..\AdminCLI
.\StampService.AdminCLI.exe status
.\StampService.AdminCLI.exe test-stamp
```

## Post-Installation Tasks

### 1. Create Backup Shares (CRITICAL!)

**This step is mandatory before using the service in production!**

```powershell
cd path\to\AdminCLI
.\StampService.AdminCLI.exe create-shares --total 5 --threshold 3 --initiator "YourName" --output C:\Shares
```

This creates 5 shares, requiring 3 to recover the key.

### 2. Distribute Shares Securely

- Give each share to a different trusted custodian
- Store shares offline (USB drive, paper printout, secure safe)
- Each custodian should verify their share:
  ```powershell
  .\StampService.AdminCLI.exe verify-share C:\Shares\share-1-of-5.json
  ```

### 3. Document Share Holders

Maintain a secure record of:
- Who has each share (Share 1: John Doe, Share 2: Jane Smith, etc.)
- Contact information for each custodian
- Location where shares are stored

### 4. Save Public Key

The public key can be safely distributed to client applications:

```powershell
.\StampService.AdminCLI.exe status > public-key.txt
```

## Client Integration

### Adding Reference

In your .NET application, add a reference to:
```
StampService\StampService.ClientLib.dll
```

### Basic Usage

```csharp
using StampService.ClientLib;
using StampService.Core.Models;

// Create client
var client = new StampServiceClient();

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

// Use the signature
Console.WriteLine($"Signature: {response.Signature}");
```

See `examples/CLIENT-EXAMPLES.md` for more detailed examples.

## Configuration

### Service Configuration

Edit `C:\Program Files\StampService\appsettings.json`:

```json
{
  "ServiceConfiguration": {
    "PipeName": "StampServicePipe",
    "KeyStorePath": "C:\\ProgramData\\StampService\\master.key",
    "AuditLogPath": "C:\\ProgramData\\StampService\\Logs\\audit.log"
  }
}
```

**Important**: Restart service after configuration changes:
```powershell
Restart-Service SecureStampService
```

### Client Configuration

```csharp
// Custom pipe name (must match service configuration)
var client = new StampServiceClient("CustomPipeName");

// Custom timeout
var client = new StampServiceClient(timeoutMs: 10000);
```

## Operations

### Daily Operations

```powershell
# Check service status
Get-Service SecureStampService

# View service logs
Get-Content "C:\ProgramData\StampService\Logs\service*.log" -Tail 50

# View audit logs
Get-Content "C:\ProgramData\StampService\Logs\audit*.log" -Tail 50

# Test service health
cd AdminCLI
.\StampService.AdminCLI.exe test-stamp

# Restart service
Restart-Service SecureStampService

# Stop service
Stop-Service SecureStampService

# Start service
Start-Service SecureStampService
```

### Key Recovery

**In case of disaster, you can recover the key on a new machine:**

1. Install the service on new machine
2. Start recovery session:
   ```powershell
   .\StampService.AdminCLI.exe recover start --threshold 3
   ```
3. Gather share holders and add their shares:
   ```powershell
   .\StampService.AdminCLI.exe recover add-share share-1-of-5.json
   .\StampService.AdminCLI.exe recover add-share share-3-of-5.json
   .\StampService.AdminCLI.exe recover add-share share-5-of-5.json
   ```
4. Verify recovery:
   ```powershell
   .\StampService.AdminCLI.exe status
   .\StampService.AdminCLI.exe test-stamp
   ```

The public key should match the original installation!

## Uninstallation

### Remove Service Only (Keep Data)

```powershell
cd Scripts
.\Uninstall-StampService.ps1
```

### Remove Service AND Data

```powershell
.\Uninstall-StampService.ps1 -RemoveData
```

**WARNING**: The `-RemoveData` flag will delete:
- Master key file
- All logs
- Configuration

Ensure you have valid backup shares before removing data!

## Troubleshooting

### Service Won't Start

1. **Check Event Viewer**:
   - Open Event Viewer
   - Navigate to Windows Logs ? Application
   - Look for errors from "SecureStampService"

2. **Check Service Logs**:
   ```powershell
   Get-Content "C:\ProgramData\StampService\Logs\service*.log" -Tail 100
   ```

3. **Verify .NET Runtime**:
   ```powershell
   dotnet --list-runtimes
   ```
   Should show .NET 8.0 or higher

4. **Check Permissions**:
   - Ensure service has access to `C:\ProgramData\StampService`
   - Verify file permissions on key file

### Client Can't Connect

1. **Verify Service is Running**:
   ```powershell
   Get-Service SecureStampService
   ```

2. **Check Named Pipe**:
   - Named Pipe should be: `\\.\pipe\StampServicePipe`
   - Verify client is using correct pipe name

3. **Check Firewall**:
   - Named Pipes are local-only, but ensure no local firewall is blocking

### Test Failures

If `test-stamp` fails:

1. **Check Key is Present**:
   ```powershell
   .\StampService.AdminCLI.exe status
   ```
   Should show "Key Present: true"

2. **Check Audit Logs**:
   ```powershell
   Get-Content "C:\ProgramData\StampService\Logs\audit*.log" -Tail 50
   ```

3. **Try Restarting Service**:
   ```powershell
   Restart-Service SecureStampService
   Start-Sleep -Seconds 5
   .\StampService.AdminCLI.exe test-stamp
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

? **DON'T:**
- Store all shares together
- Email shares
- Store shares in plain text on network
- Give multiple shares to one person
- Lose track of share holders

### Service Security

? **DO:**
- Run service under dedicated service account
- Restrict access to data directory
- Monitor audit logs regularly
- Set up alerting on service failures
- Keep system updated and patched
- Perform regular health checks

? **DON'T:**
- Copy master.key file as backup (use shares!)
- Expose service to network
- Grant unnecessary users access to service machine
- Ignore audit log warnings

### Operational Security

? **DO:**
- Test recovery process in non-production
- Have documented recovery procedures
- Maintain contact list of share holders
- Rotate shares if compromised
- Review logs for unusual activity
- Monitor service health daily

? **DON'T:**
- Skip backup share creation
- Store commitment hash with shares
- Allow automated recovery
- Ignore security events in logs

## Support

### Documentation

- **Full README**: See `README.md` for complete documentation
- **Quick Start**: See `QUICKSTART.md` for step-by-step guide
- **Client Examples**: See `examples/CLIENT-EXAMPLES.md`

### Logs

- **Service Logs**: `C:\ProgramData\StampService\Logs\service-YYYYMMDD.log`
- **Audit Logs**: `C:\ProgramData\StampService\Logs\audit-YYYYMMDD.log`

### Contact

For issues and questions, contact your system administrator or refer to internal documentation.

## Version Information

- **Build Date**: See `version.json`
- **Configuration**: Release
- **Target**: Windows x64
- **Framework**: .NET 8

## License

See LICENSE file (if included) or check with your organization's legal department.

---

**Important Reminder**: The private key NEVER leaves the service. It cannot be exported or viewed. The only way to backup/recover the key is through Shamir Secret Sharing. Create your backup shares immediately after installation!

