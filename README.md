# Secure Stamp Service

> A local, HSM-like signing authority implemented as a Windows Service (.NET 8)

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/Windows-Service-0078D6)](https://docs.microsoft.com/windows/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Generates and holds a master private key inside a secure process, never exposes the key, supports Shamir Secret Sharing backups, and provides a secure local API for Windows client apps to request cryptographic signatures.**

---

## 🎯 What Is This?

A Windows service that acts like a **hardware security module (HSM)**:

- 🔐 Master signing key generated once and stored securely (never exported)
- ✍️ Client apps submit requests → service returns cryptographic signatures
- 💾 Master key backups via **Shamir's Secret Sharing** (distributed to custodians)
- 🔄 Key recovery from threshold shares (e.g., 3 of 5 needed)

**Use Cases**: Token minting, approval workflows, multi-signature systems, audit trails, cryptographic stamping

---

## 📚 Documentation

> **All documentation is in the [Resources](Resources/) folder** • [View Index](Resources/INDEX.md)

### Quick Links

| Getting Started | Development | Integration |
|----------------|-------------|-------------|
| [Quick Start](Resources/QUICKSTART.md) | [Visual Studio](Resources/VISUAL-STUDIO-DEVELOPMENT.md) | [Client Integration](Resources/CLIENT-INTEGRATION.md) |
| [Installation](Resources/USER-INSTALLATION-GUIDE.md) | [Build Guide](Resources/BUILD.md) | [Code Examples](examples/CLIENT-EXAMPLES.md) |
| [Distribution](Resources/DISTRIBUTION.md) | [Contributing](Resources/CONTRIBUTING.md) | [NuGet Publishing](Resources/NUGET-PUBLISHING.md) |

[**→ Complete Documentation Index**](Resources/INDEX.md)

---

## ⚡ Quick Start

### Install the Service

```powershell
# Extract distribution ZIP and run as Administrator
cd ExtractedFolder\Scripts
.\Install-StampService.ps1

# Verify and test
Get-Service SecureStampService
cd ..\AdminCLI
.\StampService.AdminCLI.exe status
.\StampService.AdminCLI.exe test-stamp

# Create backup shares (CRITICAL!)
.\StampService.AdminCLI.exe create-shares --total 5 --threshold 3 --output C:\Shares
```

[**→ Full Installation Guide**](Resources/QUICKSTART.md)

### Build from Source

```powershell
# Simple build
.\build.bat

# Or manual
dotnet restore
dotnet build -c Release
dotnet test

# Or Visual Studio
# Open StampService.sln, Press F5
```

[**→ Build Guide**](Resources/BUILD.md) | [**→ Visual Studio Guide**](Resources/VISUAL-STUDIO-DEVELOPMENT.md)

### Integrate in Your App

```powershell
dotnet add package StampService.ClientLib --version 1.0.0
```

```csharp
using StampService.ClientLib;

var client = new StampServiceClient();
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
bool isValid = client.VerifySignature(response);
```

[**→ Integration Guide**](Resources/CLIENT-INTEGRATION.md) | [**→ Code Examples**](examples/CLIENT-EXAMPLES.md)

---

## 🏗️ Architecture

```
Client Apps → Named Pipes → Windows Service → DPAPI-encrypted Key
    ↓
          Audit Logging
     ↓
     Shamir Shares (Offline Backup)
```

### Components

| Component | Type | Purpose |
|-----------|------|---------|
| **StampService** | Windows Service | Main service executable |
| **StampService.Core** | Library | Business logic, crypto, SSS |
| **StampService.ClientLib** | Library | Client integration (NuGet) |
| **StampService.AdminCLI** | Tool | Administration CLI |

[**→ Technical Details**](Resources/README.md)

---

## ✨ Features

### Security
- Windows DPAPI encryption • Shamir Secret Sharing backup • Named Pipes (local-only)
- Comprehensive audit logging • Private key never exported

### Cryptography
- **Algorithm**: Ed25519 (EdDSA)
- **Performance**: ~1-2ms per signature
- **Provider**: BouncyCastle

### Operations
Sign any operation: `mint`, `burn`, `freeze`, `move`, `approve`, or custom

---

## 🔐 Backup & Recovery

### Create Shares
```powershell
.\StampService.AdminCLI.exe create-shares --total 5 --threshold 3 --output C:\Shares
# Distribute to 5 custodians, need 3 to recover
```

### Recover Key
```powershell
.\Install-StampService.ps1  # On new machine
.\StampService.AdminCLI.exe recover start --threshold 3
.\StampService.AdminCLI.exe recover add-share share-1-of-5.json
.\StampService.AdminCLI.exe recover add-share share-3-of-5.json
.\StampService.AdminCLI.exe recover add-share share-5-of-5.json
# Key automatically reconstructed
```

[**→ Full Recovery Guide**](Resources/QUICKSTART.md#key-recovery-process)

---

## 🛠️ Administration

```powershell
# Service management
Get-Service SecureStampService
Start-Service SecureStampService
Restart-Service SecureStampService

# CLI commands
.\StampService.AdminCLI.exe status
.\StampService.AdminCLI.exe test-stamp
.\StampService.AdminCLI.exe create-shares --total 5 --threshold 3
.\StampService.AdminCLI.exe verify-share share-1-of-5.json
.\StampService.AdminCLI.exe recover start --threshold 3

# View logs
Get-Content "C:\ProgramData\StampService\Logs\service*.log" -Tail 50
Get-Content "C:\ProgramData\StampService\Logs\audit*.log" -Tail 50
```

---

## 🔧 Requirements

**End Users**: Windows 10/11 or Server 2019+ • .NET 8 Runtime (included) • Administrator privileges

**Developers**: Windows 10/11 or Server • .NET 8 SDK • Visual Studio 2022/2023 or VS Code

---

## 📈 Performance

- **Signing**: ~1-2ms (Ed25519)
- **Throughput**: ~500-1000 signatures/sec (single-threaded)
- **Memory**: ~50-100 MB
- **Size**: ~50 MB installation

---

## 🚨 Security Notes

**CRITICAL**:
- 🔴 Private key NEVER leaves the service
- 🔴 Create backup shares immediately after installation
- 🔴 Distribute shares to 5+ trusted custodians (offline storage)
- 🔴 Test recovery process before production
- 🔴 Monitor audit logs regularly

**Best Practices**:
- Run under dedicated service account
- Store shares in physical safes or encrypted offline storage
- Set up daily health checks
- Never store all shares together

[**→ Security Guide**](Resources/DISTRIBUTION.md)

---

## 🤝 Contributing

Contributions welcome! See [Contributing Guide](Resources/CONTRIBUTING.md)

- 🐛 Report bugs via GitHub Issues
- 💡 Suggest features via GitHub Discussions
- 🔀 Submit PRs

---

## 📞 Support

- 📚 [Documentation Index](Resources/INDEX.md)
- 🚀 [Quick Start](Resources/QUICKSTART.md)
- 💻 [Developer Guide](Resources/VISUAL-STUDIO-DEVELOPMENT.md)
- 🔍 Logs: `C:\ProgramData\StampService\Logs\`

---

## 📝 License

MIT License - see [LICENSE](LICENSE) file

---

## 🎯 Next Steps

**New Users** → [Quick Start](Resources/QUICKSTART.md) → Install → Create shares

**Developers** → [Visual Studio Guide](Resources/VISUAL-STUDIO-DEVELOPMENT.md) → Clone → F5

**Integrators** → [Integration Guide](Resources/CLIENT-INTEGRATION.md) → NuGet → Code

---

**Built for secure cryptographic operations on Windows**

[View Complete Documentation →](Resources/INDEX.md)
