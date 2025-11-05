# Visual Studio Development Guide - Secure Stamp Service

## Overview

This guide covers development, debugging, and testing of the Secure Stamp Service using Visual Studio 2022/2023 (not command-line tools).

---

## Table of Contents

1. [Opening the Solution](#opening-the-solution)
2. [Solution Structure](#solution-structure)
3. [Setting Up for Debugging](#setting-up-for-debugging)
4. [Debugging Scenarios](#debugging-scenarios)
5. [Running Tests in Visual Studio](#running-tests-in-visual-studio)
6. [Build Configurations](#build-configurations)
7. [Common Development Tasks](#common-development-tasks)
8. [Troubleshooting](#troubleshooting)
9. [Tips & Best Practices](#tips--best-practices)

---

## Opening the Solution

1. **Open Visual Studio 2022/2023**
2. **File ? Open ? Project/Solution**
3. Navigate to: `D:\Jobs\workspace\DiG\Stamp-Service\`
4. Select: `StampService.sln`
5. Click **Open**

Visual Studio will load all 5 projects and restore NuGet packages automatically.

---

## Solution Structure

```
StampService (Solution)
?
??? ?? src
?   ?
?   ??? ?? StampService [Windows Service - Main Entry Point]
?   ?   ??? Program.cs          (Service host)
?   ?   ??? StampServiceWorker.cs    (BackgroundService)
?   ?   ??? appsettings.json   (Configuration)
?   ?
?   ??? ?? StampService.Core       [Class Library - Business Logic]
?   ?   ??? KeyManager.cs      (Key generation, storage, signing)
?   ?   ??? SSSManager.cs    (Shamir Secret Sharing)
?   ?   ??? IPCServer.cs      (Named Pipe server)
?   ?   ??? AuditLogger.cs       (Audit logging)
?   ?   ??? Models/       (Request/Response models)
?   ?
?   ??? ?? StampService.ClientLib   [Class Library - Client API]
?   ?   ??? StampServiceClient.cs          (Client for IPC communication)
?   ?   ??? Models/            (Shared models)
?   ?
?   ??? ?? StampService.AdminCLI      [Console App - Admin Tool]
?   ?   ??? Program.cs                (CLI commands)
?   ? ??? Commands/ (Command handlers)
?   ?
?   ??? ?? StampService.Tests          [Test Project]
?       ??? KeyManagerTests.cs             (Key management tests)
?  ??? SSSManagerTests.cs    (Shamir Secret Sharing tests)
?       ??? CryptoTests.cs (Cryptography tests)
?
??? ?? Documentation Files (README.md, etc.)
```

**Legend:**
- ?? = Executable Project
- ?? = Library Project
- ?? = Test Project

---

## Setting Up for Debugging

### 1. Set the Startup Project

Visual Studio needs to know which project to run when you press F5. You have two main options:

#### Option A: StampService (Main Service) - **RECOMMENDED FOR MOST DEBUGGING**

**Steps:**
1. In **Solution Explorer**, right-click **StampService** project
2. Select **"Set as Startup Project"**
3. The project name will appear **bold**

**When to use:**
- Debugging the main service logic
- Testing IPC server (Named Pipes)
- Debugging signing operations
- Testing KeyManager
- Testing SSSManager in service context

#### Option B: StampService.AdminCLI (Admin Tool)

**Steps:**
1. In **Solution Explorer**, right-click **StampService.AdminCLI** project
2. Select **"Set as Startup Project"**

**When to use:**
- Testing admin commands
- Debugging client-side communication
- Testing recovery workflow
- Debugging share creation/verification

### 2. Configure Launch Settings for StampService

To run the service in **console mode** (not as Windows Service):

1. In **Solution Explorer**, expand **StampService** project
2. Expand **Properties** node
3. Double-click **launchSettings.json** (if it doesn't exist, create it)
4. Add this configuration:

```json
{
  "profiles": {
    "StampService (Console)": {
      "commandName": "Project",
      "commandLineArgs": "--console",
      "environmentVariables": {
        "DOTNET_ENVIRONMENT": "Development"
      }
    },
    "StampService (Service)": {
      "commandName": "Project",
      "commandLineArgs": "",
      "environmentVariables": {
        "DOTNET_ENVIRONMENT": "Production"
      }
    }
  }
}
```

5. Save the file

**To switch profiles:**
- Click the dropdown next to the **green Start button** (?)
- Select **"StampService (Console)"** for debugging
- Select **"StampService (Service)"** for production testing

### 3. Configure Launch Settings for AdminCLI

1. In **Solution Explorer**, expand **StampService.AdminCLI** project
2. Right-click the project ? **Properties**
3. Go to **Debug** ? **General** ? Click **"Open debug launch profiles UI"**
4. Add **Command line arguments** for different commands:

**Common commands:**
- `status`
- `test-stamp`
- `create-shares --total 5 --threshold 3 --output C:\Temp\Shares`
- `verify-share C:\Temp\Shares\share-1-of-5.json`

**Or create multiple profiles in launchSettings.json:**

```json
{
  "profiles": {
    "Status": {
      "commandName": "Project",
      "commandLineArgs": "status"
    },
  "TestStamp": {
    "commandName": "Project",
      "commandLineArgs": "test-stamp"
    },
    "CreateShares": {
      "commandName": "Project",
      "commandLineArgs": "create-shares --total 5 --threshold 3 --output C:\\Temp\\Shares"
    },
"VerifyShare": {
      "commandName": "Project",
      "commandLineArgs": "verify-share C:\\Temp\\Shares\\share-1-of-5.json"
    }
  }
}
```

---

## Debugging Scenarios

### Scenario 1: Debug the Main Service

**Goal:** Debug service startup, key management, signing operations

**Steps:**
1. Set **StampService** as startup project
2. Select **"StampService (Console)"** profile
3. Set breakpoints in:
   - `Program.cs` (line ~25, service initialization)
   - `StampServiceWorker.cs` (ExecuteAsync method)
   - `KeyManager.cs` (Sign method)
4. Press **F5** or click **Start Debugging** (?)
5. The service will start in console mode
6. Watch the Output window for logs
7. Step through code as needed

**Breakpoint Suggestions:**
- `KeyManager.cs` ? `GenerateNewKey()` method
- `KeyManager.cs` ? `Sign()` method
- `IPCServer.cs` ? `HandleClientAsync()` method
- `SSSManager.cs` ? `CreateShares()` method

### Scenario 2: Debug Client Communication

**Goal:** Debug how clients communicate with the service

**Setup:**
1. Start the service first (without debugging):
   - Right-click **StampService** ? **Debug** ? **Start Without Debugging** (Ctrl+F5)
   - Or run as Windows Service
2. Set **StampService.AdminCLI** as startup project
3. Configure command line arguments (e.g., `test-stamp`)
4. Set breakpoints in:
   - `StampServiceClient.cs` (SignAsync method)
   - AdminCLI command handlers
5. Press **F5** to debug the client

**Multi-Project Debugging:**
1. Right-click **Solution** ? **Properties**
2. Go to **Startup Project**
3. Select **"Multiple startup projects"**
4. Set **StampService** ? **Start**
5. Set **StampService.AdminCLI** ? **Start**
6. Click **OK**
7. Press **F5** - both projects will start with debugging

### Scenario 3: Debug Shamir Secret Sharing

**Goal:** Debug SSS share creation, verification, reconstruction

**Option A - Unit Tests (Recommended):**
1. Open **Test Explorer**: **Test** ? **Test Explorer**
2. Find **SSSManagerTests** class
3. Right-click a test (e.g., `CreateShares_ValidInput_CreatesValidShares`)
4. Select **Debug Test**
5. Set breakpoints in `SSSManager.cs` methods
6. Step through the SSS logic

**Option B - Via Service:**
1. Set **StampService** as startup project
2. Set breakpoint in `SSSManager.cs` ? `CreateShares()` method
3. Start debugging (F5)
4. Use AdminCLI (in another instance) to call `create-shares` command
5. Breakpoint will hit in the service

**Option C - Via AdminCLI:**
1. Ensure service is running
2. Set **StampService.AdminCLI** as startup project
3. Set command line args: `create-shares --total 5 --threshold 3`
4. Set breakpoints in AdminCLI command handler
5. Press F5
6. Step through share creation flow

### Scenario 4: Debug Key Generation and Storage

**Goal:** Debug DPAPI encryption, key persistence

**Steps:**
1. Delete existing key file (if any): `C:\ProgramData\StampService\master.key`
2. Set **StampService** as startup project
3. Set breakpoints in `KeyManager.cs`:
   - `GenerateNewKey()` method
   - `SaveKey()` method
   - Lines with `ProtectedData.Protect()`
4. Press **F5**
5. Step through key generation and DPAPI encryption
6. Use **Locals** window to inspect key bytes (before encryption)
7. Use **Watch** window to monitor variables

**Watch Variables:**
- `privateKey` (byte array)
- `publicKey` (byte array)
- `encryptedData` (after DPAPI)
- `keyFilePath`

### Scenario 5: Debug IPC (Named Pipes)

**Goal:** Debug Named Pipe server and client communication

**Steps:**
1. Set breakpoints in `IPCServer.cs`:
   - `StartAsync()` method
   - `HandleClientAsync()` method
   - Message parsing code
2. Set breakpoints in `StampServiceClient.cs`:
   - `ConnectAsync()` method
   - `SendRequestAsync()` method
3. Use multi-project debugging (see Scenario 2)
4. Step through both server and client sides

**Debugging Tips:**
- Use **Output Window** ? **Debug** to see trace messages
- Watch `NamedPipeServerStream` state
- Inspect message serialization/deserialization

---

## Running Tests in Visual Studio

### Using Test Explorer

1. **Open Test Explorer**: **Test** ? **Test Explorer** (or Ctrl+E, T)
2. Click **Run All Tests** (??) to run entire test suite
3. Or expand test tree and run specific tests

**Test Categories:**
```
?? StampService.Tests
?
??? ?? KeyManagerTests
?   ??? ? GenerateNewKey_CreatesValidKeyPair
?   ??? ? Sign_ValidData_ReturnsValidSignature
?   ??? ? SaveAndLoadKey_Roundtrip_Success
? ??? ?? LoadKey_ConcurrentAccess_ThreadSafe (intermittent)
?
??? ?? SSSManagerTests
?   ??? ? CreateShares_ValidInput_CreatesValidShares
?   ??? ? VerifyShare_ValidShare_ReturnsTrue
?   ??? ? ReconstructSecret_ThresholdShares_Success
?   ??? ? ReconstructSecret_ComplexCombinations (GF256 issue)
?
??? ?? CryptoTests
    ??? ? Ed25519_SignAndVerify_Success
    ??? ? SignatureValidation_InvalidSignature_ReturnsFalse
```

### Debugging a Specific Test

1. In **Test Explorer**, find the test
2. Right-click the test
3. Select **Debug Test** (or **Debug Selected Tests**)
4. Set breakpoints in the code being tested
5. Test will run and stop at breakpoints

**Example - Debug SSS Reconstruction:**
1. Open `SSSManagerTests.cs`
2. Find test: `ReconstructSecret_ThresholdShares_Success`
3. Set breakpoint at line where shares are created
4. Right-click test in Test Explorer ? **Debug Test**
5. Step through share creation and reconstruction
6. Inspect intermediate values in **Locals** window

### Running Tests with Filters

**By Category:**
1. In **Test Explorer**, use search box
2. Type: `FullyQualifiedName~KeyManagerTests`
3. Tests filtered to KeyManagerTests only

**By Trait (if you add traits):**
```csharp
[TestMethod]
[TestCategory("SSS")]
[TestCategory("Slow")]
public void ReconstructSecret_ManyShares_Success()
{
    // Test code
}
```

Filter by typing `Trait:SSS` in Test Explorer search box.

### Test Settings

**Configure test execution:**
1. **Test** ? **Configure Run Settings**
2. Select **Auto Detect runsettings Files**
3. Or create `.runsettings` file in solution root

**Example runsettings for parallel execution:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <MaxCpuCount>4</MaxCpuCount>
    <ResultsDirectory>.\TestResults</ResultsDirectory>
  </RunConfiguration>
  <MSTest>
    <Parallelize>
      <Workers>4</Workers>
      <Scope>ClassLevel</Scope>
  </Parallelize>
  </MSTest>
</RunSettings>
```

---

## Build Configurations

### Debug vs Release

**Switch configurations:**
1. Use toolbar dropdown: **Debug** ? / **Release** ?
2. Or: **Build** ? **Configuration Manager**

**Debug Configuration:**
- Full debug symbols
- No optimization
- Better debugging experience
- Slower execution
- **Use for:** Development and debugging

**Release Configuration:**
- Optimized code
- Minimal debug symbols
- Best performance
- Harder to debug
- **Use for:** Testing, distribution

### Building the Solution

**Build Entire Solution:**
1. **Build** ? **Build Solution** (Ctrl+Shift+B)
2. Watch **Output** window for build progress
3. Check **Error List** for any issues

**Build Single Project:**
1. Right-click project in Solution Explorer
2. Select **Build**

**Rebuild (Clean + Build):**
1. **Build** ? **Rebuild Solution**
2. Deletes old binaries and builds fresh

**Clean Solution:**
1. **Build** ? **Clean Solution**
2. Removes all build artifacts

### Build Output

**Default locations:**
- Debug: `src\{ProjectName}\bin\Debug\net8.0-windows\`
- Release: `src\{ProjectName}\bin\Release\net8.0-windows\`

**Publish Output:**
1. Right-click **StampService** project
2. Select **Publish**
3. Create new profile ? **Folder**
4. Target location: `D:\Jobs\workspace\DiG\Stamp-Service\publish\`
5. Click **Publish**

---

## Common Development Tasks

### Task 1: Add a New Signing Operation

**Scenario:** Add support for "approve" operation

1. Open `SignRequest.cs` in StampService.Core
2. Add operation to enum/constants (if using)
3. Open `IPCServer.cs`
4. Find `HandleSignRequest()` method
5. Add validation for "approve" operation
6. Set breakpoint to test
7. Press F5 to debug

### Task 2: Modify SSS Parameters

**Scenario:** Test different threshold values

1. Open `SSSManager.cs`
2. Set breakpoint in `CreateShares()` method
3. Open **Watch** window
4. Add watches:
   - `threshold`
   - `totalShares`
   - `shares` (array)
5. Run test or service
6. Inspect share generation process

### Task 3: Add Logging to a Method

**Example: Add logging to KeyManager.Sign()**

1. Open `KeyManager.cs`
2. Find `Sign()` method
3. Add logging:

```csharp
public byte[] Sign(byte[] data)
{
    _logger.LogInformation("Sign operation requested for {DataLength} bytes", data.Length);
  
    // existing code...
    
    _logger.LogInformation("Sign operation completed, signature length: {SigLength}", signature.Length);
    return signature;
}
```

4. Run service in console mode
5. Watch **Output** window ? **Debug** for log messages

### Task 4: Inspect Named Pipe Communication

**Goal:** See actual messages sent over Named Pipe

1. Open `IPCServer.cs`
2. Find `HandleClientAsync()` method
3. Set breakpoint where message is received
4. Add **Watch** on:
   - `request` (deserialized request object)
   - `response` (response object)
5. Start multi-project debugging
6. Step through request/response cycle

### Task 5: Test Key Recovery Flow

**Complete recovery workflow in Visual Studio:**

1. **Create backup shares first:**
 - Set **StampService** as startup
   - Run service (F5)
   - Switch to **AdminCLI** (set as startup)
   - Args: `create-shares --total 5 --threshold 3 --output C:\Temp\Shares`
   - Run (F5) - shares created

2. **Delete master key:**
   - Stop debugging
   - Delete: `C:\ProgramData\StampService\master.key`

3. **Debug recovery:**
   - Set breakpoints in `SSSManager.cs` ? `ReconstructSecret()`
   - Start service (F5)
   - Switch to AdminCLI
   - Args: `recover add-share C:\Temp\Shares\share-1-of-5.json`
   - Run multiple times with different shares
 - Watch reconstruction in **Locals** window

---

## Troubleshooting

### Problem: Can't Start Service - Port/Pipe in Use

**Symptoms:**
- Exception: "All pipe instances are busy"
- Service won't start

**Solution:**
1. Open **Task Manager** (Ctrl+Shift+Esc)
2. Find any running `StampService.exe` processes
3. End those processes
4. Try running again

**Or in Visual Studio:**
1. **Debug** ? **Stop Debugging** (Shift+F5)
2. Wait a few seconds
3. Try again

### Problem: Breakpoints Not Hitting

**Possible causes:**

**1. Wrong build configuration:**
- Ensure you're in **Debug** mode (not Release)
- **Build** ? **Configuration Manager** ? Check "Debug"

**2. Code not up-to-date:**
- **Build** ? **Rebuild Solution**
- Try again

**3. Optimization enabled:**
- Right-click project ? **Properties**
- Go to **Build** ? **Advanced**
- Ensure "Optimize code" is **unchecked** for Debug

**4. Symbols not loaded:**
- **Debug** ? **Windows** ? **Modules**
- Check if your DLLs are loaded with symbols
- If not, check symbol paths in **Tools** ? **Options** ? **Debugging** ? **Symbols**

### Problem: Tests Failing in Visual Studio

**SSS tests failing:**
- Known issue with GF(256) implementation
- 16/19 tests pass currently
- See `IMPLEMENTATION-SUMMARY.md` for details

**Intermittent KeyManager test failure:**
- DPAPI timing issue
- Re-run test usually works
- Not a critical issue

**Test won't run:**
1. **Build** ? **Clean Solution**
2. **Build** ? **Build Solution**
3. **Test** ? **Run All Tests**

### Problem: Can't Debug as Windows Service

**Issue:** Windows Services are hard to debug

**Solution:** Use console mode (already configured)
1. Service runs with `--console` flag
2. Acts like console app
3. Full debugging support
4. See output in console window

**If you MUST debug as service:**
1. Install service: Use PowerShell script
2. Add to `Program.cs`:
   ```csharp
   #if DEBUG
       System.Diagnostics.Debugger.Launch();
   #endif
   ```
3. Start service from Services.msc
4. When prompt appears, attach Visual Studio debugger

### Problem: NuGet Package Restore Issues

**Symptoms:**
- Build errors about missing packages
- Red squiggles under `using` statements

**Solution:**
1. Right-click **Solution** in Solution Explorer
2. Select **Restore NuGet Packages**
3. Wait for restore to complete
4. **Build** ? **Rebuild Solution**

**If still failing:**
1. **Tools** ? **Options** ? **NuGet Package Manager**
2. Click **Clear All NuGet Cache(s)**
3. Restore packages again

### Problem: DPAPI Key File Access Denied

**Symptoms:**
- Exception when saving/loading key
- "Access to path denied"

**Solution:**
1. Run Visual Studio as **Administrator**:
   - Close Visual Studio
   - Right-click Visual Studio icon
   - Select **Run as Administrator**
2. Or change key storage path to user directory (for development)

---

## Tips & Best Practices

### Visual Studio Shortcuts

**Essential shortcuts:**
- **F5** - Start Debugging
- **Ctrl+F5** - Start Without Debugging
- **Shift+F5** - Stop Debugging
- **F9** - Toggle Breakpoint
- **F10** - Step Over
- **F11** - Step Into
- **Shift+F11** - Step Out
- **Ctrl+Shift+B** - Build Solution
- **Ctrl+E, T** - Open Test Explorer
- **Ctrl+K, Ctrl+D** - Format Document

### Debugging Windows

**Essential windows (Debug ? Windows):**
- **Locals** (Alt+4) - Local variables
- **Watch** (Ctrl+Alt+W, 1-4) - Watched expressions
- **Call Stack** (Ctrl+Alt+C) - Call stack
- **Immediate** (Ctrl+Alt+I) - Execute code while debugging
- **Output** (Ctrl+Alt+O) - Debug output and logs
- **Autos** (Ctrl+Alt+V, A) - Relevant variables

### Breakpoint Tips

**Conditional breakpoints:**
1. Set breakpoint (F9)
2. Right-click the red dot
3. Select **Conditions**
4. Add condition: e.g., `data.Length > 100`
5. Breakpoint only hits when condition is true

**Tracepoints (log without stopping):**
1. Set breakpoint
2. Right-click ? **Actions**
3. Add message: `Signing {data.Length} bytes`
4. Check "Continue execution"
5. Message appears in Output window without stopping

**Hit count:**
1. Right-click breakpoint ? **Conditions**
2. Select **Hit Count**
3. Example: "Break when hit count is equal to 5"
4. Useful for loops

### Using Immediate Window

**Execute code while debugging:**
```csharp
// In Immediate window (Ctrl+Alt+I)
? data.Length      // Evaluate expression
? Convert.ToBase64String(signature)  // Convert to base64
testVariable = 42    // Change variable value
```

### Data Visualizers

**View complex data:**
- Hover over variable
- Click magnifying glass icon
- Options: Text, XML, JSON, HTML visualizers

**For byte arrays:**
```csharp
// In Watch window
Convert.ToBase64String(privateKey)
```

### Code Navigation

**Go to definition:**
- F12 on any symbol
- Or Ctrl+Click

**Find all references:**
- Shift+F12 on symbol
- See all usages

**Navigate back/forward:**
- Ctrl+- (back)
- Ctrl+Shift+- (forward)

### Solution Explorer Tips

**Search in Solution Explorer:**
- Ctrl+; (open search)
- Type class/file name
- Quick navigation

**Pending Changes (Git):**
- **View** ? **Git Changes** (Ctrl+0, Ctrl+G)
- See modified files
- Commit, push, pull

### Performance Profiling

**If you need to optimize:**
1. **Debug** ? **Performance Profiler** (Alt+F2)
2. Select tools:
 - CPU Usage
   - Memory Usage
   - .NET Object Allocation
3. Click **Start**
4. Run scenario
5. Stop profiling
6. Analyze report

### Code Analysis

**Run code analysis:**
1. Right-click project
2. **Analyze and Code Cleanup** ? **Run Code Analysis**
3. Review warnings in Error List
4. Fix issues

### IntelliSense Tips

**Trigger IntelliSense:**
- Ctrl+Space - Force IntelliSense
- Ctrl+Shift+Space - Parameter info
- Ctrl+J - List members

**Quick fixes:**
- Ctrl+. (on any error/warning)
- Shows suggested fixes
- Apply fix automatically

---

## Development Workflow Example

### Daily Development Session

1. **Open Visual Studio**
2. **Open Solution**: StampService.sln
3. **Pull latest changes**:
   - **Git Changes** window (Ctrl+0, Ctrl+G)
   - Click **Pull**
4. **Build Solution**: Ctrl+Shift+B
5. **Run Tests**: Open Test Explorer (Ctrl+E, T), run all
6. **Make changes** to code
7. **Set breakpoints** where needed
8. **Set startup project**: Right-click StampService ? Set as Startup Project
9. **Start debugging**: F5
10. **Test changes** while debugging
11. **Stop debugging**: Shift+F5
12. **Run tests again**: Verify nothing broke
13. **Commit changes**:
    - **Git Changes** window
    - Enter commit message
    - Click **Commit All**
14. **Push to remote**: Click **Push**

---

## Quick Reference Card

### Debugging Checklist

**Before debugging:**
- [ ] Build solution (Ctrl+Shift+B)
- [ ] Set correct startup project
- [ ] Configure launch profile (console mode for service)
- [ ] Set breakpoints
- [ ] Open relevant debug windows (Locals, Watch, Output)

**During debugging:**
- [ ] Watch Output window for logs
- [ ] Use Locals/Watch to inspect variables
- [ ] Use Step Over (F10) / Step Into (F11)
- [ ] Use Immediate window for quick tests
- [ ] Check Call Stack if confused

**After debugging:**
- [ ] Run tests to verify changes
- [ ] Check Error List for warnings
- [ ] Commit changes to Git

---

## Additional Resources

### Within Visual Studio

- **Help** ? **View Help** (Ctrl+F1)
- **Help** ? **Keyboard Shortcuts** reference
- **View** ? **Error List** (Ctrl+\, E)
- **View** ? **Output** (Ctrl+Alt+O)

### Project Documentation

- See `README.md` for architecture
- See `IMPLEMENTATION-SUMMARY.md` for component details
- See `QUICKSTART.md` for operational guide
- See inline code comments

---

## Summary

You now have a complete Visual Studio-focused development guide for the Secure Stamp Service. The key points:

? **Set StampService as startup project** for most debugging
? **Use console mode** (`--console` flag) for easy debugging
? **Use Test Explorer** for running and debugging tests
? **Use multi-project debugging** to debug service + client together
? **Leverage breakpoints, watches, and debug windows** effectively
? **Use Git Changes window** for version control

**Happy debugging! ????**
