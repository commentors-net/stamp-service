# Using Secure Stamp Service in Your Project - Complete Checklist

## ?? Pre-Integration Checklist

### Understanding the System
- [ ] Read [README.md](README.md) - Main overview
- [ ] Read [PROJECT-SUMMARY.md](PROJECT-SUMMARY.md) - Complete summary
- [ ] Understand the architecture from [Resources/IMPLEMENTATION-SUMMARY.md](Resources/IMPLEMENTATION-SUMMARY.md)
- [ ] Review security model and key management concepts

### Prerequisites
- [ ] Windows 10/11 or Windows Server 2019+
- [ ] .NET 8 SDK or Runtime
- [ ] Administrator access (for service installation)
- [ ] Visual Studio 2022/2023 (for development)

---

## ?? Option 1: Using as NuGet Package (Recommended for Application Integration)

### Phase 1: Setup (10 minutes)

#### 1.1 Build NuGet Packages
- [ ] Clone or download the repository
- [ ] Stop any running StampService: `Stop-Service SecureStampService -ErrorAction SilentlyContinue`
- [ ] Run: `.\scripts\Build-NuGetPackages.ps1`
- [ ] Verify packages created in `nupkgs/` folder
  - [ ] `StampService.Core.1.0.0.nupkg`
  - [ ] `StampService.ClientLib.1.0.0.nupkg`

**Documentation**: [Resources/NUGET-QUICK-REF.md](Resources/NUGET-QUICK-REF.md)

#### 1.2 Configure Package Source
- [ ] Add local source: `dotnet nuget add source D:\Path\To\nupkgs --name StampServiceLocal`
- [ ] Verify source added: `dotnet nuget list source`
- [ ] (Optional) Publish to network share or Azure Artifacts for team use

**Documentation**: [Resources/NUGET-PUBLISHING.md](Resources/NUGET-PUBLISHING.md)

### Phase 2: Install Service (15 minutes)

#### 2.1 Service Installation
- [ ] Extract distribution package (if using pre-built installer)
- [ ] OR build from source: `.\scripts\Build-Complete-Distribution.ps1`
- [ ] Open PowerShell as Administrator
- [ ] Run: `.\scripts\Install-StampService.ps1`
- [ ] Verify service installed: `Get-Service SecureStampService`
- [ ] Verify service running: Status should be "Running"

**Documentation**: [Resources/QUICKSTART.md](Resources/QUICKSTART.md)

#### 2.2 Create Backup Shares (CRITICAL!)
- [ ] Run: `.\AdminCLI\StampService.AdminCLI.exe create-shares --total 5 --threshold 3 --output C:\Shares`
- [ ] Verify 5 share files created
- [ ] Verify `commitment.txt` created
- [ ] Distribute shares to 5 trusted custodians
- [ ] Each custodian verifies their share: `.\AdminCLI\StampService.AdminCLI.exe verify-share share-X-of-5.json`
- [ ] Document who has which share
- [ ] Store shares offline (USB drive, paper, safe)

**WARNING**: Without backup shares, key loss is permanent!

**Documentation**: [Resources/USER-INSTALLATION-GUIDE.md](Resources/USER-INSTALLATION-GUIDE.md)

### Phase 3: Integrate into Your Application (30 minutes)

#### 3.1 Add NuGet Package to Your Project
```powershell
cd YourProject
dotnet add package StampService.ClientLib --source StampServiceLocal
```

- [ ] Package installed successfully
- [ ] `StampService.ClientLib` appears in project dependencies
- [ ] `StampService.Core` appears as transitive dependency

**Documentation**: [Resources/CLIENT-INTEGRATION.md](Resources/CLIENT-INTEGRATION.md)

#### 3.2 Write Integration Code

**For ASP.NET Core:**
```csharp
// Program.cs
using StampService.ClientLib;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IStampServiceClient, StampServiceClient>();
var app = builder.Build();

app.MapPost("/sign", async (SignRequest request, IStampServiceClient client) =>
{
    var response = await client.SignAsync(request);
    return Results.Ok(response);
});

app.Run();
```

**For Console/Desktop:**
```csharp
using StampService.ClientLib;
using StampService.Core.Models;

var client = new StampServiceClient();

var request = new SignRequest
{
    Operation = "your-operation",
 RequesterId = "YourApp",
    Payload = new Dictionary<string, object>
    {
        ["data"] = "your-data"
    }
};

var response = await client.SignAsync(request);
Console.WriteLine($"Signature: {response.Signature}");
```

**Checklist:**
- [ ] Import necessary namespaces
- [ ] Create `StampServiceClient` instance
- [ ] Create `SignRequest` with your operation data
- [ ] Call `SignAsync()` method
- [ ] Handle `SignedResponse`
- [ ] (Optional) Verify signature locally with `VerifySignature()`

**Examples**: See [Resources/CLIENT-INTEGRATION.md](Resources/CLIENT-INTEGRATION.md) for 15+ integration patterns

#### 3.3 Error Handling
```csharp
try
{
    var response = await client.SignAsync(request);
}
catch (TimeoutException)
{
    // Service didn't respond
}
catch (IOException)
{
    // Cannot connect to service
}
catch (Exception ex)
{
    // Other errors
    _logger.LogError(ex, "Signing failed");
}
```

- [ ] Implement timeout handling
- [ ] Implement connection error handling
- [ ] Implement retry logic (if needed)
- [ ] Add logging for all errors

**Documentation**: [Resources/CLIENT-INTEGRATION.md](Resources/CLIENT-INTEGRATION.md) - Troubleshooting section

### Phase 4: Testing (20 minutes)

#### 4.1 Test Service Connectivity
- [ ] Run your application
- [ ] Test `GetStatusAsync()` - should return service status
- [ ] Verify `IsRunning = true` and `KeyPresent = true`
- [ ] Test `TestStampAsync()` - should return valid signature
- [ ] Verify signature locally with `VerifySignature()`

#### 4.2 Test Your Operations
- [ ] Test sign request with your operation type
- [ ] Verify signature returned
- [ ] Verify timestamp is correct
- [ ] Test with different payload types
- [ ] Test concurrent requests (if applicable)
- [ ] Test error scenarios (invalid data, service down, etc.)

#### 4.3 Load Testing (Optional)
- [ ] Test with expected request volume
- [ ] Monitor service performance logs
- [ ] Check for any memory leaks
- [ ] Verify throughput meets requirements (~500-1000 sig/sec)

### Phase 5: Deployment (30 minutes)

#### 5.1 Deploy Service to Production
- [ ] Build distribution: `.\scripts\Build-Complete-Distribution.ps1`
- [ ] Copy installer to production server
- [ ] Run installer on production server
- [ ] Create production backup shares
- [ ] Distribute shares to production custodians
- [ ] Verify shares created and distributed
- [ ] Test service in production environment

#### 5.2 Deploy Your Application
- [ ] Build your application for production
- [ ] Deploy to production environment
- [ ] Configure to connect to production StampService
- [ ] Test connectivity from production app
- [ ] Monitor initial production usage

#### 5.3 Monitoring Setup
- [ ] Set up health check endpoint
- [ ] Configure logging to SIEM (if available)
- [ ] Set up alerting for service failures
- [ ] Document operational procedures
- [ ] Train operations team

---

## ?? Option 2: Using as Windows Service Only (No NuGet)

### Phase 1: Installation (15 minutes)
Same as Option 1 Phase 2

### Phase 2: Direct Integration (20 minutes)

#### 2.1 Add Client Library Reference
- [ ] Copy `StampService.ClientLib.dll` from installation directory
- [ ] Copy `StampService.Core.dll` from installation directory
- [ ] Add both as project references
- [ ] OR copy source files into your project

#### 2.2 Integration Code
Same as Option 1 Phase 3

---

## ?? Option 3: Building from Source

### Phase 1: Setup Development Environment (20 minutes)

#### 1.1 Clone Repository
- [ ] Clone: `git clone https://github.com/commentors-net/stamp-service`
- [ ] Navigate to directory: `cd stamp-service`
- [ ] Check branch: `git branch` (should be on `main`)

#### 1.2 Open in Visual Studio
- [ ] Open `StampService.sln` in Visual Studio 2022/2023
- [ ] Restore NuGet packages (automatic)
- [ ] Build solution: `Ctrl+Shift+B`
- [ ] Verify all projects build successfully
- [ ] Run tests: Open Test Explorer and run all tests

**Documentation**: [Resources/VISUAL-STUDIO-DEVELOPMENT.md](Resources/VISUAL-STUDIO-DEVELOPMENT.md)

### Phase 2: Debug and Develop (Variable)

#### 2.1 Run Service in Console Mode
- [ ] Set `StampService` as startup project
- [ ] Select "StampService (Console)" launch profile
- [ ] Press F5 to debug
- [ ] Service should start in console window
- [ ] Test with AdminCLI in separate terminal

#### 2.2 Make Your Changes
- [ ] Make code changes as needed
- [ ] Add tests for new functionality
- [ ] Build and test: `dotnet build && dotnet test`
- [ ] Debug with breakpoints as needed

**Documentation**: [Resources/VISUAL-STUDIO-DEVELOPMENT.md](Resources/VISUAL-STUDIO-DEVELOPMENT.md)

### Phase 3: Build Distribution
- [ ] Run: `.\scripts\Build-Complete-Distribution.ps1`
- [ ] Verify installer created: `StampService-Setup-1.0.0.exe`
- [ ] Test installer on clean VM
- [ ] Continue with Option 1 Phase 2 onwards

---

## ?? Option 4: Distributing to Team/Users

### Phase 1: Build Packages (10 minutes)

#### 1.1 Service Installer
- [ ] Run: `.\scripts\Build-Complete-Distribution.ps1`
- [ ] Verify created:
  - [ ] `StampService-Setup-1.0.0.exe` (single-file installer)
  - [ ] `StampService-Distribution-*.zip` (ZIP package)
  - [ ] `BUILD-SUMMARY.txt` (build report)

**Documentation**: [Resources/BUILD.md](Resources/BUILD.md)

#### 1.2 NuGet Packages
- [ ] Run: `.\scripts\Build-NuGetPackages.ps1`
- [ ] Verify created in `nupkgs/`:
  - [ ] `StampService.Core.*.nupkg`
  - [ ] `StampService.ClientLib.*.nupkg`
  - [ ] `PACKAGE-INFO.txt`

**Documentation**: [Resources/NUGET-PUBLISHING.md](Resources/NUGET-PUBLISHING.md)

### Phase 2: Choose Distribution Method

#### Option A: Single-File Installer (For End Users)
- [ ] Distribute: `StampService-Setup-1.0.0.exe`
- [ ] Provide: [Resources/USER-INSTALLATION-GUIDE.md](Resources/USER-INSTALLATION-GUIDE.md)
- [ ] Provide: [Resources/QUICK-INSTALL-CARD.txt](Resources/QUICK-INSTALL-CARD.txt)
- [ ] (Optional) Code sign installer: `signtool sign /f cert.pfx /p password installer.exe`

#### Option B: ZIP Package (For IT Professionals)
- [ ] Distribute: `StampService-Distribution-*.zip`
- [ ] Provide: [Resources/QUICKSTART.md](Resources/QUICKSTART.md)
- [ ] Provide: [Resources/DISTRIBUTION.md](Resources/DISTRIBUTION.md)

#### Option C: NuGet Packages (For Developers)

**Local Folder:**
- [ ] Copy `nupkgs/` folder to shared location
- [ ] Team adds source: `dotnet nuget add source \\path\to\nupkgs --name CompanyPackages`

**Network Share:**
- [ ] Run: `.\scripts\Publish-NuGetPackages.ps1 -Destination NetworkShare -Source "\\server\share\nuget"`
- [ ] Team adds source: `dotnet nuget add source \\server\share\nuget --name CompanyPackages`

**Azure Artifacts:**
- [ ] Create Azure DevOps feed
- [ ] Run: `.\scripts\Publish-NuGetPackages.ps1 -Destination AzureArtifacts -Source "URL" -ApiKey "PAT"`
- [ ] Configure team authentication

**GitHub Packages:**
- [ ] Run: `.\scripts\Publish-NuGetPackages.ps1 -Destination GitHub -Source "URL" -ApiKey "PAT"`
- [ ] Configure team authentication

**Documentation**: [Resources/NUGET-PUBLISHING.md](Resources/NUGET-PUBLISHING.md)

### Phase 3: Documentation for Users
- [ ] Provide [Resources/INDEX.md](Resources/INDEX.md) - Documentation index
- [ ] For service users: [Resources/QUICKSTART.md](Resources/QUICKSTART.md)
- [ ] For developers: [Resources/NUGET-QUICK-REF.md](Resources/NUGET-QUICK-REF.md)
- [ ] For integration: [Resources/CLIENT-INTEGRATION.md](Resources/CLIENT-INTEGRATION.md)

---

## ?? Verification Checklist

### Service Verification
- [ ] Service installed: `Get-Service SecureStampService`
- [ ] Service running: Status = "Running"
- [ ] Test stamp works: `.\AdminCLI\StampService.AdminCLI.exe test-stamp`
- [ ] Status shows key present: `.\AdminCLI\StampService.AdminCLI.exe status`
- [ ] Logs are being written: Check `C:\ProgramData\StampService\Logs\`

### Backup Verification
- [ ] 5 share files exist
- [ ] `commitment.txt` exists
- [ ] Each share verified successfully
- [ ] Shares distributed to different people
- [ ] Share locations documented
- [ ] Test recovery process (on test machine)

### Integration Verification
- [ ] NuGet package installed in your project
- [ ] Your app can connect to service
- [ ] Sign requests work
- [ ] Signatures verify correctly
- [ ] Error handling works
- [ ] Logging implemented

### Security Verification
- [ ] Private key never exported
- [ ] Key file has restrictive permissions
- [ ] Audit logs enabled
- [ ] Shares stored offline
- [ ] Access to service machine restricted
- [ ] Monitoring configured

---

## ?? Documentation Quick Reference

| Task | Document | Time |
|------|----------|------|
| **Quick NuGet setup** | [NUGET-QUICK-REF.md](Resources/NUGET-QUICK-REF.md) | 2 min |
| **Complete NuGet guide** | [NUGET-PUBLISHING.md](Resources/NUGET-PUBLISHING.md) | 20 min |
| **Integration examples** | [CLIENT-INTEGRATION.md](Resources/CLIENT-INTEGRATION.md) | 15 min |
| **Service installation** | [QUICKSTART.md](Resources/QUICKSTART.md) | 5 min |
| **Visual Studio development** | [VISUAL-STUDIO-DEVELOPMENT.md](Resources/VISUAL-STUDIO-DEVELOPMENT.md) | 20 min |
| **Build from source** | [BUILD.md](Resources/BUILD.md) | 15 min |
| **All documentation** | [INDEX.md](Resources/INDEX.md) | - |

---

## ?? Common Issues & Solutions

### Issue: Can't connect to service
**Solution**: 
1. Check service is running: `Get-Service SecureStampService`
2. Check pipe name matches (default: "StampServicePipe")
3. Restart service: `Restart-Service SecureStampService`

### Issue: NuGet package not found
**Solution**:
1. Verify source configured: `dotnet nuget list source`
2. Clear cache: `dotnet nuget locals all --clear`
3. Build packages again: `.\scripts\Build-NuGetPackages.ps1`

### Issue: Service locks DLLs during build
**Solution**:
1. Stop service: `Stop-Service SecureStampService`
2. Build: `.\scripts\Build-NuGetPackages.ps1`
3. Start service: `Start-Service SecureStampService`

### Issue: Test stamp fails
**Solution**:
1. Check service logs: `C:\ProgramData\StampService\Logs\`
2. Verify key is present: `.\AdminCLI\StampService.AdminCLI.exe status`
3. Check Event Viewer for errors

---

## ? Final Checklist

Before going to production:
- [ ] Service installed and tested
- [ ] Backup shares created and distributed
- [ ] Shares verified by all custodians
- [ ] Test recovery process completed
- [ ] Your application integrated and tested
- [ ] Error handling implemented
- [ ] Logging configured
- [ ] Monitoring set up
- [ ] Documentation provided to team
- [ ] Operations team trained
- [ ] Security review completed (optional)
- [ ] Load testing completed (optional)

---

## ?? Success!

You're now ready to use Secure Stamp Service in your project!

**Need help?** Check the documentation index: [Resources/INDEX.md](Resources/INDEX.md)

**Questions about integration?** See: [Resources/CLIENT-INTEGRATION.md](Resources/CLIENT-INTEGRATION.md)

**Issues?** Check troubleshooting sections in each guide.

---

**Total Setup Time**: 1-2 hours (depending on your scenario)

**You're ready to build secure, cryptographically signed applications!** ??
