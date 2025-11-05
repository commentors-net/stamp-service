# Secure Stamp Service - Quick Start Guide

## Overview

The Secure Stamp Service is a Windows service that acts like an HSM (Hardware Security Module), providing cryptographic signing operations while keeping the private key secure and never exposed.

## Installation (5 minutes)

### Prerequisites
- Windows 10/11 or Windows Server 2019+
- .NET 8 Runtime (included in distribution)
- Administrator privileges

### Steps

1. **Extract the distribution package**
   ```
   StampService-Distribution-YYYYMMDD.zip
   ```

2. **Open PowerShell as Administrator**
   - Right-click PowerShell
   - Select "Run as Administrator"

3. **Navigate to Scripts folder**
   ```powershell
   cd path\to\extracted\Scripts
   ```

4. **Run installation script**
   ```powershell
   .\Install-StampService.ps1
   ```

   The installer will:
   - Copy files to `C:\Program Files\StampService`
   - Create secure data directory at `C:\ProgramData\StampService`
   - Install Windows Service named "SecureStampService"
   - Start the service
   - Generate and secure a master signing key

5. **Verify installation**
   ```powershell
   # Check service status
   Get-Service SecureStampService
   
   # Test the service
   cd ..\AdminCLI
   .\StampService.AdminCLI.exe status
   ```

## First Use

### 1. Test the Service

```powershell
cd path\to\AdminCLI
.\StampService.AdminCLI.exe test-stamp
```

Expected output:
```
=== Test Stamp Response ===
Algorithm: Ed25519
Signer ID: StampService-v1
Timestamp: 2025-01-04 12:34:56.789

? Signature Valid: True
```

### 2. View Service Status

```powershell
.\StampService.AdminCLI.exe status
```

Shows:
- Service running status
- Key present confirmation
- Uptime
- Public key (safe to share)

### 3. Create Backup Shares (IMPORTANT!)

**Before production use, create backup shares:**

```powershell
# Create 5 shares, requiring 3 for recovery
.\StampService.AdminCLI.exe create-shares --total 5 --threshold 3 --output C:\Shares

# Or with custom parameters
.\StampService.AdminCLI.exe create-shares -n 7 -t 4 -i "John Doe" -o C:\SecureShares
```

This creates:
- `share-1-of-5.json` through `share-5-of-5.json`
- `commitment.txt` (hash for verification)

**CRITICAL: Distribute these shares to trusted custodians!**
- Each person gets ONE share
- Shares should be stored offline (USB drive, paper printout, safe)
- Need threshold shares (e.g., 3 out of 5) to recover the key

### 4. Verify Shares

Each custodian should verify their share:

```powershell
.\StampService.AdminCLI.exe verify-share share-1-of-5.json
```

## Client Integration

### From C# Application

1. **Add reference to StampService.ClientLib.dll**

2. **Use the client library:**

```csharp
using StampService.ClientLib;
using StampService.Core.Models;

// Create client
var client = new StampServiceClient();

// Sign a request
var request = new SignRequest
{
    Operation = "mint",
    RequesterId = "MyApplication",
    Payload = new Dictionary<string, object>
    {
        ["recipient"] = "user123",
        ["amount"] = 1000,
        ["currency"] = "TOKEN",
        ["timestamp"] = DateTime.UtcNow
    }
};

var response = await client.SignAsync(request);

// Use the signature
Console.WriteLine($"Signature: {response.Signature}");
Console.WriteLine($"Algorithm: {response.Algorithm}");

// Verify signature (optional, locally)
bool isValid = client.VerifySignature(response);
```

### Supported Operations

The service signs any operation you specify:
- `mint` - Create new tokens
- `burn` - Destroy tokens
- `freeze` - Freeze accounts
- `move` - Transfer operations
- Or any custom operation name

## Key Recovery Process

**If you need to recover the key on a new machine:**

### 1. Install the service (new machine)

```powershell
.\Install-StampService.ps1
```

The service will start WITHOUT a key.

### 2. Start recovery session

```powershell
cd ..\AdminCLI
.\StampService.AdminCLI.exe recover start --threshold 3
```

### 3. Gather custodians with shares

Have custodians bring their share files to the recovery location.

### 4. Add each share

```powershell
.\StampService.AdminCLI.exe recover add-share C:\path\to\share-1-of-5.json
.\StampService.AdminCLI.exe recover add-share C:\path\to\share-3-of-5.json
.\StampService.AdminCLI.exe recover add-share C:\path\to\share-5-of-5.json
```

After threshold shares (3 in this example), the key is automatically reconstructed.

### 5. Verify recovery

```powershell
.\StampService.AdminCLI.exe status
.\StampService.AdminCLI.exe test-stamp
```

The public key should match the original!

## Daily Operations

### Check Service Health

```powershell
# Quick status
Get-Service SecureStampService

# Detailed status
cd path\to\AdminCLI
.\StampService.AdminCLI.exe status

# Test signature
.\StampService.AdminCLI.exe test-stamp
```

### View Logs

```powershell
# Service logs
Get-Content "C:\ProgramData\StampService\Logs\service*.log" -Tail 50

# Audit logs (all sign operations)
Get-Content "C:\ProgramData\StampService\Logs\audit*.log" -Tail 50
```

### Restart Service

```powershell
Restart-Service SecureStampService
```

## Security Best Practices

### 1. Share Management
- ? Create shares immediately after installation
- ? Distribute to 5+ trusted individuals
- ? Store offline (not in cloud)
- ? Each custodian verifies their share
- ? Document who has which share
- ? Never store all shares together
- ? Never share the master key file

### 2. Access Control
- Service runs as LocalSystem by default
- Only local connections allowed (Named Pipe)
- Consider restricting client access via Windows ACLs
- Monitor audit logs regularly

### 3. Monitoring
- Set up daily health checks (`test-stamp`)
- Alert on service failures
- Review audit logs for unusual activity
- Monitor share creation events

### 4. Backup
- The master key is at: `C:\ProgramData\StampService\master.key`
- This file is encrypted with Windows DPAPI
- **Do NOT copy this file as backup** - use Shamir shares instead!
- Backup the public key and share commitment for verification

## Troubleshooting

### Service won't start

1. Check Windows Event Viewer
2. Check service logs: `C:\ProgramData\StampService\Logs\service*.log`
3. Verify .NET 8 runtime is installed
4. Ensure data directory has correct permissions

### Can't connect from client

1. Verify service is running: `Get-Service SecureStampService`
2. Check Named Pipe exists: Should be `\\.\pipe\StampServicePipe`
3. Verify client has permission to access the pipe
4. Check firewall (though Named Pipes are local only)

### Key recovery fails

1. Verify you have enough shares (? threshold)
2. Verify shares are from the same backup (check commitment)
3. Ensure shares are not corrupted (verify each share first)
4. Check that service has no existing key

### Performance issues

1. Check service logs for errors
2. Verify adequate memory and CPU
3. Consider that each signature operation is cryptographically intensive
4. Review audit logs for excessive requests

## Advanced Configuration

### Custom Named Pipe

Edit `appsettings.json` in install directory:

```json
{
  "ServiceConfiguration": {
    "PipeName": "MyCustomPipeName",
    "KeyStorePath": "C:\\ProgramData\\StampService\\master.key",
    "AuditLogPath": "C:\\ProgramData\\StampService\\Logs\\audit.log"
  }
}
```

Restart service after changes.

### Client Configuration

```csharp
// Custom pipe name
var client = new StampServiceClient("MyCustomPipeName");

// Custom timeout
var client = new StampServiceClient("StampServicePipe", timeoutMs: 10000);
```

## Uninstallation

### Remove service only (keeps data)

```powershell
cd path\to\Scripts
.\Uninstall-StampService.ps1
```

### Remove service AND data

```powershell
.\Uninstall-StampService.ps1 -RemoveData
```

**WARNING:** This deletes the master key! Ensure you have valid backup shares first!

## Support & Documentation

- Full README: See `README.md` in distribution package
- API Documentation: See source code comments
- Issue Tracker: Check your organization's issue tracker

## License

See LICENSE file in distribution package.

---

**Remember:** The security of this system depends on:
1. Keeping shares safe and distributed
2. Monitoring the service and logs
3. Restricting access to the service machine
4. Following security best practices

**The private key never leaves the service - it cannot be exported or viewed!**
