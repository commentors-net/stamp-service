# Secure Stamp Service

> A local, HSM-like signing authority implemented as a Windows Service (.NET 8)

## Quick Links

This is the main README for the Secure Stamp Service. All detailed documentation is organized in the **Resources** folder.

### ?? Documentation

- **[Complete README](Resources/README.md)** - Full technical documentation and architecture
- **[Quick Start Guide](Resources/QUICKSTART.md)** - Get up and running in 5 minutes
- **[Visual Studio Development Guide](Resources/VISUAL-STUDIO-DEVELOPMENT.md)** - Development and debugging using Visual Studio
- **[Build Guide](Resources/BUILD.md)** - Building and distribution instructions
- **[Implementation Summary](Resources/IMPLEMENTATION-SUMMARY.md)** - What has been built
- **[Distribution Guide](Resources/DISTRIBUTION.md)** - Deployment and distribution
- **[Contributing Guide](Resources/CONTRIBUTING.md)** - How to contribute

## What is This?

The Secure Stamp Service is a Windows service that acts as a secure signing authority (like an HSM) for cryptographic operations:

- ? **Secure Key Storage** - Master key never exposed, stored with Windows DPAPI
- ? **Ed25519 Signatures** - Fast, secure cryptographic signing
- ? **Shamir Secret Sharing** - Distributed backup and recovery
- ? **Local IPC** - Named Pipes for secure local communication
- ? **Audit Logging** - Comprehensive logging of all operations
- ? **Windows Service** - Runs automatically on system startup

## Quick Start

### For Users (Installation)

1. Extract the distribution package
2. Open PowerShell as Administrator
3. Run: `.\Scripts\Install-StampService.ps1`
4. Create backup shares: `.\AdminCLI\StampService.AdminCLI.exe create-shares --total 5 --threshold 3`

See **[Quick Start Guide](Resources/QUICKSTART.md)** for detailed instructions.

### For Developers

1. Open `StampService.sln` in Visual Studio 2022/2023
2. Set `StampService` as startup project
3. Press F5 to debug (runs in console mode)

See **[Visual Studio Development Guide](Resources/VISUAL-STUDIO-DEVELOPMENT.md)** for detailed instructions.

## Project Structure

```
Stamp-Service/
??? src/
?   ??? StampService/  # Windows Service (Main Entry Point)
?   ??? StampService.Core/      # Business Logic
?   ??? StampService.ClientLib/ # Client Library
?   ??? StampService.AdminCLI/  # Admin Tool
?   ??? StampService.Tests/     # Test Suite
??? scripts/             # Installation & Build Scripts
??? Resources/      # ?? All Documentation
??? StampService.sln       # Visual Studio Solution
```

## Key Features

### Security
- Private key generated and stored securely (Windows DPAPI)
- Never exported or exposed
- Shamir Secret Sharing for backup/recovery (configurable threshold)
- Named Pipes for local-only communication
- Comprehensive audit logging

### Cryptography
- **Algorithm**: Ed25519 (EdDSA)
- **Key Size**: 256 bits
- **Signature Size**: 512 bits
- **Performance**: ~1-2ms per signature

### Operations
The service can sign any operation type:
- `mint` - Create new tokens/assets
- `burn` - Destroy tokens
- `freeze` - Freeze accounts
- `move` - Transfer operations
- Or any custom operation name

## Components

| Component | Type | Purpose |
|-----------|------|---------|
| **StampService** | Windows Service | Main service executable |
| **StampService.Core** | Class Library | Business logic and crypto |
| **StampService.ClientLib** | Class Library | Client integration library |
| **StampService.AdminCLI** | Console App | Administration tool |
| **StampService.Tests** | Test Project | Comprehensive test suite |

## Documentation Index

### Getting Started
- [Quick Start Guide](Resources/QUICKSTART.md) - Installation and first use
- [Visual Studio Development](Resources/VISUAL-STUDIO-DEVELOPMENT.md) - Development setup and debugging

### Technical Documentation
- [Complete README](Resources/README.md) - Full technical documentation
- [Implementation Summary](Resources/IMPLEMENTATION-SUMMARY.md) - Architecture and components
- [Build Guide](Resources/BUILD.md) - Build and distribution

### Deployment
- [Distribution Guide](Resources/DISTRIBUTION.md) - Deployment procedures
- [Contributing Guide](Resources/CONTRIBUTING.md) - Contribution guidelines

## Requirements

### For End Users
- Windows 10/11 or Windows Server 2019+
- .NET 8 Runtime (included in distribution)
- Administrator privileges for installation

### For Developers
- Windows 10/11 or Windows Server
- .NET 8 SDK
- Visual Studio 2022/2023
- Git (optional)

## Support

- **Documentation**: See [Resources](Resources/) folder
- **Issues**: Check GitHub Issues
- **Logs**: `C:\ProgramData\StampService\Logs\`

## License

See LICENSE file for details.

---

**Security Note**: The private key NEVER leaves the service. It cannot be exported or viewed. The only way to backup/recover the key is through Shamir Secret Sharing.

**For detailed documentation, see the [Resources](Resources/) folder.**
