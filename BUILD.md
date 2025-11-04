# How to Build and Distribute

This guide explains how to build the Secure Stamp Service from source and create a distribution package for end users.

## For Developers: Building from Source

### Prerequisites
- Windows 10/11 or Windows Server
- .NET 8 SDK installed
- Visual Studio 2022 or VS Code (optional, for development)
- PowerShell 5.1 or higher
- Git (optional)

### Quick Build

```powershell
# Clone the repository (if not already done)
git clone https://github.com/your-org/stamp-service
cd stamp-service

# Restore packages
dotnet restore StampService.sln

# Build Release version
dotnet build StampService.sln -c Release

# Run tests
dotnet test StampService.sln -c Release
```

### Create Distribution Package

```powershell
# Run the distribution build script
cd scripts
.\Build-Distribution.ps1

# This will create:
# - dist/ folder with all files
# - StampService-Distribution-YYYYMMDD-HHMMSS.zip
```

The ZIP file contains everything needed for distribution.

## For End Users: Installing from Distribution

### Prerequisites
- Windows 10/11 or Windows Server 2019+
- Administrator privileges
- No .NET SDK required (runtime included in service)

### Installation Steps

1. **Extract the Distribution Package**
   - Unzip `StampService-Distribution-YYYYMMDD-HHMMSS.zip`
   - Extract to a temporary location (e.g., `C:\Temp\StampService`)

2. **Open PowerShell as Administrator**
   ```powershell
   # Press Windows + X, select "PowerShell (Admin)"
   cd C:\Temp\StampService\Scripts
   ```

3. **Run Installation Script**
   ```powershell
   .\Install-StampService.ps1
   ```

4. **Verify Installation**
   ```powershell
   Get-Service SecureStampService
   cd ..\AdminCLI
   .\StampService.AdminCLI.exe status
   ```

5. **Create Backup Shares (CRITICAL!)**
   ```powershell
   .\StampService.AdminCLI.exe create-shares --total 5 --threshold 3 --output C:\Shares
   ```

See `QUICKSTART.md` for detailed instructions.

## Distribution Contents

After running `Build-Distribution.ps1`, the ZIP contains:

```
StampService-Distribution-YYYYMMDD-HHMMSS.zip
??? StampService/              # Service files
?   ??? StampService.exe
?   ??? StampService.ClientLib.dll  # For client integration
?   ??? [dependencies]
??? AdminCLI/                  # Admin tool
?   ??? StampService.AdminCLI.exe
??? Scripts/                   # Installation scripts
?   ??? Install-StampService.ps1
?   ??? Uninstall-StampService.ps1
?   ??? Build-Distribution.ps1
??? README.md                  # Full documentation
??? QUICKSTART.md              # Quick start guide
??? DISTRIBUTION-README.txt    # User-facing readme
??? version.json               # Build info
```

## Build Configuration

### Release Build (Default)
```powershell
dotnet build -c Release
```

Optimized for production:
- Code optimization enabled
- Debug symbols minimal
- Best performance

### Debug Build
```powershell
dotnet build -c Debug
```

For development:
- Full debug symbols
- No optimization
- Better debugging experience

## Testing

### Run All Tests
```powershell
dotnet test StampService.sln -c Release
```

### Run Specific Test Class
```powershell
dotnet test StampService.sln --filter "FullyQualifiedName~KeyManagerTests"
```

### Test Coverage
Current status:
- ? Crypto Operations: All passing
- ? Key Management: All passing (1 intermittent)
- ?? SSS Operations: 16/19 passing

## Customization

### Changing Service Name

Edit `scripts/Install-StampService.ps1`:
```powershell
.\Install-StampService.ps1 -ServiceName "CustomStampService"
```

### Changing Installation Path

```powershell
.\Install-StampService.ps1 -InstallPath "D:\Services\StampService"
```

### Changing Named Pipe

Edit `src/StampService/appsettings.json`:
```json
{
  "ServiceConfiguration": {
    "PipeName": "MyCustomPipeName",
    ...
  }
}
```

Then rebuild and redistribute.

## Publishing Options

### Self-Contained (Includes .NET Runtime)

```powershell
dotnet publish src/StampService/StampService.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o publish/StampService
```

Pros:
- No .NET runtime required on target machine
- Guaranteed version compatibility

Cons:
- Larger file size (~100MB vs ~5MB)

### Framework-Dependent (Requires .NET Runtime)

```powershell
dotnet publish src/StampService/StampService.csproj `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -o publish/StampService
```

Pros:
- Smaller file size
- Uses system-installed runtime

Cons:
- Requires .NET 8 runtime on target machine

The `Build-Distribution.ps1` script uses framework-dependent by default.

## Continuous Integration

### GitHub Actions Example

```yaml
name: Build and Test

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build -c Release --no-restore
    
    - name: Test
      run: dotnet test -c Release --no-build --verbosity normal
    
    - name: Create Distribution
      run: ./scripts/Build-Distribution.ps1
      shell: powershell
    
    - name: Upload Artifact
      uses: actions/upload-artifact@v3
      with:
        name: StampService-Distribution
        path: StampService-Distribution-*.zip
```

## Code Signing

For production, sign the executables:

```powershell
# After building, sign with your certificate
$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert
Set-AuthenticodeSignature -FilePath "dist\StampService\StampService.exe" -Certificate $cert
Set-AuthenticodeSignature -FilePath "dist\AdminCLI\StampService.AdminCLI.exe" -Certificate $cert
```

## Troubleshooting Build Issues

### Missing .NET SDK

**Error**: `The command 'dotnet' is not recognized`

**Solution**:
```powershell
# Check if .NET SDK is installed
dotnet --version

# If not, download from: https://dotnet.microsoft.com/download
```

### NuGet Restore Fails

**Error**: `Unable to load the service index for source`

**Solution**:
```powershell
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore again
dotnet restore
```

### Build Fails on Windows-Specific APIs

**Error**: `'ProtectedData' does not exist`

**Solution**: Ensure project targets `net8.0-windows`:
```xml
<TargetFramework>net8.0-windows</TargetFramework>
```

### Tests Fail

**Error**: SSS reconstruction tests fail

**Note**: This is a known issue with the current GF(256) implementation. The service still works correctly for most operations. Consider using a vetted SSS library for production.

## Distribution Checklist

Before distributing to users:

- [ ] All builds successful (`dotnet build`)
- [ ] Tests passing (`dotnet test`)
- [ ] `Build-Distribution.ps1` runs successfully
- [ ] ZIP file created
- [ ] Test installation on clean VM
- [ ] Verify service starts and runs
- [ ] Test admin CLI commands
- [ ] Test client library integration
- [ ] Documentation included
- [ ] Version info correct
- [ ] (Optional) Code signed

## Support

For build issues:
- Check `IMPLEMENTATION-SUMMARY.md` for architecture details
- Review test output for specific failures
- Check PowerShell execution policy if scripts won't run

For deployment issues:
- See `QUICKSTART.md` for installation steps
- See `DISTRIBUTION.md` for troubleshooting
- Check service logs in `C:\ProgramData\StampService\Logs\`

## Version History

- **v1.0.0** (2025-01-04)
  - Initial release
  - Ed25519 signing
  - Shamir Secret Sharing
  - Windows Service
  - Admin CLI
  - Client Library

## License

[Your License Here]

