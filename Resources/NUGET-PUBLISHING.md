# NuGet Package Publishing Guide

This guide explains how to create and distribute NuGet packages for the Secure Stamp Service client libraries.

## Quick Start

### Build NuGet Packages

```powershell
# Build with default version (1.0.0)
.\scripts\Build-NuGetPackages.ps1

# Build with specific version
.\scripts\Build-NuGetPackages.ps1 -Version "1.2.3"

# Build with symbols for debugging
.\scripts\Build-NuGetPackages.ps1 -Version "1.2.3" -IncludeSymbols

# Use existing build (skip compilation)
.\scripts\Build-NuGetPackages.ps1 -SkipBuild
```

This creates packages in `./nupkgs/`:
- `StampService.Core.{version}.nupkg`
- `StampService.ClientLib.{version}.nupkg`

## Distribution Methods

### Method 1: Local Folder (Best for Development)

**Use Case**: Testing, local development, isolated environments

**Steps**:

1. **Build packages**:
   ```powershell
   .\scripts\Build-NuGetPackages.ps1 -Version "1.0.0"
   ```

2. **Create local NuGet source**:
 ```powershell
   # Add local folder as NuGet source
   dotnet nuget add source C:\MyNuGetPackages --name LocalPackages
   
   # Or use the nupkgs folder directly
   dotnet nuget add source D:\Path\To\stamp-service\nupkgs --name StampServiceLocal
   ```

3. **Install in your project**:
 ```powershell
   cd MyClientApp
   dotnet add package StampService.ClientLib --version 1.0.0 --source LocalPackages
   ```

4. **Verify installation**:
   ```powershell
   dotnet list package
   ```

**Pros**:
- ? Simple and fast
- ? No server required
- ? Great for testing

**Cons**:
- ? Not suitable for team sharing
- ? Must manually copy to each machine

---

### Method 2: Network Share (Best for Teams)

**Use Case**: Team development, corporate environments, private distribution

**Steps**:

1. **Setup network share**:
   ```powershell
   # Create share (as Administrator)
   New-SmbShare -Name "NuGetPackages" -Path "C:\NuGetPackages" -FullAccess "Domain\Developers"
   
   # Or use existing share like \\fileserver\shared\nuget
   ```

2. **Copy packages to share**:
   ```powershell
Copy-Item .\nupkgs\*.nupkg \\fileserver\shared\nuget\
   ```

3. **Configure on developer machines**:
   ```powershell
   # Each developer adds the network source
   dotnet nuget add source \\fileserver\shared\nuget --name CompanyPackages
   ```

4. **Install packages**:
   ```powershell
   dotnet add package StampService.ClientLib --source CompanyPackages
   ```

**Pros**:
- ? Easy to share with team
- ? Centralized updates
- ? Works with existing infrastructure

**Cons**:
- ? Requires network access
- ? No versioning control
- ? Manual upload process

---

### Method 3: Azure Artifacts (Best for Enterprise)

**Use Case**: Enterprise teams, CI/CD pipelines, Azure DevOps users

**Setup** (One-time):

1. **Create Azure Artifacts feed**:
   - Go to Azure DevOps ? Artifacts
   - Click "Create Feed"
   - Name: `StampService`
   - Visibility: Choose (Organization/Private)
   - Click "Create"

2. **Get feed URL**:
   - Click "Connect to feed"
   - Select "NuGet.exe"
   - Copy the feed URL

3. **Add feed to NuGet sources**:
   ```powershell
   dotnet nuget add source "https://pkgs.dev.azure.com/{organization}/_packaging/{feedName}/nuget/v3/index.json" `
    --name "AzureArtifacts" `
       --username "anything" `
    --password "{PAT}" `
       --store-password-in-clear-text
 ```

**Publishing**:

1. **Build packages**:
   ```powershell
   .\scripts\Build-NuGetPackages.ps1 -Version "1.0.0"
   ```

2. **Push to Azure Artifacts**:
   ```powershell
   cd nupkgs
   
   # Push each package
   dotnet nuget push StampService.Core.1.0.0.nupkg `
       --source "AzureArtifacts" `
       --api-key az
   
   dotnet nuget push StampService.ClientLib.1.0.0.nupkg `
     --source "AzureArtifacts" `
       --api-key az
   ```

3. **Install in projects**:
   ```powershell
   # No special flags needed if source is configured
   dotnet add package StampService.ClientLib
   ```

**Automation with Azure Pipelines**:

```yaml
# azure-pipelines.yml
trigger:
  tags:
    include:
      - 'v*'

pool:
  vmImage: 'windows-latest'

steps:
- task: DotNetCoreCLI@2
  displayName: 'Build NuGet Packages'
  inputs:
    command: 'custom'
    custom: 'pack'
    arguments: '-c Release -o $(Build.ArtifactStagingDirectory)'

- task: NuGetCommand@2
  displayName: 'Push to Azure Artifacts'
  inputs:
    command: 'push'
    packagesToPush: '$(Build.ArtifactStagingDirectory)/*.nupkg'
    nuGetFeedType: 'internal'
    publishVstsFeed: 'StampService'
```

**Pros**:
- ? Integrated with Azure DevOps
- ? Version management
- ? CI/CD integration
- ? Access control
- ? Package retention policies

**Cons**:
- ? Requires Azure DevOps
- ? Cost for private feeds (beyond free tier)

---

### Method 4: GitHub Packages (Best for Open Source/GitHub Users)

**Use Case**: GitHub-hosted projects, open source, GitHub Actions

**Setup** (One-time):

1. **Create GitHub Personal Access Token (PAT)**:
   - GitHub ? Settings ? Developer settings ? Personal access tokens
   - Generate new token (classic)
   - Select scopes: `write:packages`, `read:packages`, `delete:packages`
   - Copy token

2. **Configure NuGet.config**:
   
   Create/edit `NuGet.config` in your solution root:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <packageSources>
       <add key="github" value="https://nuget.pkg.github.com/commentors-net/index.json" />
     </packageSources>
     <packageSourceCredentials>
       <github>
   <add key="Username" value="YOUR-GITHUB-USERNAME" />
         <add key="ClearTextPassword" value="YOUR-GITHUB-PAT" />
</github>
     </packageSourceCredentials>
   </configuration>
   ```

**Publishing**:

1. **Build packages**:
   ```powershell
   .\scripts\Build-NuGetPackages.ps1 -Version "1.0.0"
   ```

2. **Push to GitHub Packages**:
   ```powershell
   cd nupkgs
   
   dotnet nuget push StampService.Core.1.0.0.nupkg `
       --source "https://nuget.pkg.github.com/commentors-net/index.json" `
       --api-key YOUR-GITHUB-PAT
   
   dotnet nuget push StampService.ClientLib.1.0.0.nupkg `
       --source "https://nuget.pkg.github.com/commentors-net/index.json" `
       --api-key YOUR-GITHUB-PAT
   ```

**Automation with GitHub Actions**:

```yaml
# .github/workflows/publish-nuget.yml
name: Publish NuGet Packages

on:
  push:
    tags:
      - 'v*'

jobs:
  publish:
    runs-on: windows-latest
 
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
   uses: actions/setup-dotnet@v3
  with:
        dotnet-version: '8.0.x'
    
- name: Build and Pack
      run: |
        dotnet restore
        dotnet build -c Release
        dotnet pack -c Release -o ./nupkgs
    
    - name: Push to GitHub Packages
   run: |
 dotnet nuget push ./nupkgs/*.nupkg `
          --source "https://nuget.pkg.github.com/commentors-net/index.json" `
          --api-key ${{ secrets.GITHUB_TOKEN }} `
      --skip-duplicate
```

**Consuming**:

1. **Add GitHub Packages as source**:
   ```powershell
   dotnet nuget add source "https://nuget.pkg.github.com/commentors-net/index.json" `
       --name "GitHubPackages" `
       --username "YOUR-USERNAME" `
       --password "YOUR-PAT" `
       --store-password-in-clear-text
   ```

2. **Install packages**:
   ```powershell
   dotnet add package StampService.ClientLib --source GitHubPackages
   ```

**Pros**:
- ? Free for public repos
- ? Integrated with GitHub
- ? GitHub Actions integration
- ? Version management

**Cons**:
- ? Requires GitHub account
- ? PAT management
- ? Private packages require GitHub subscription

---

### Method 5: NuGet.org (Public Distribution)

**Use Case**: Public/open-source distribution

**?? WARNING**: Only use this if you want to make packages publicly available!

**Setup**:

1. **Create NuGet.org account**:
   - Go to nuget.org ? Sign up

2. **Generate API key**:
   - NuGet.org ? Account ? API Keys
   - Create new key
   - Copy key

**Publishing**:

```powershell
cd nupkgs

dotnet nuget push StampService.Core.1.0.0.nupkg `
 --source https://api.nuget.org/v3/index.json `
    --api-key YOUR-NUGET-API-KEY

dotnet nuget push StampService.ClientLib.1.0.0.nupkg `
    --source https://api.nuget.org/v3/index.json `
    --api-key YOUR-NUGET-API-KEY
```

**Pros**:
- ? Public discovery
- ? No setup for consumers
- ? Free

**Cons**:
- ? Public (anyone can see/use)
- ? Cannot unpublish versions
- ? Not suitable for private/proprietary code

---

## Version Management

### Semantic Versioning

Follow SemVer: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes

Examples:
- `1.0.0` - Initial release
- `1.1.0` - Added new features
- `1.1.1` - Bug fixes
- `2.0.0` - Breaking API changes

### Using Git Tags

```powershell
# Create and push a version tag
git tag v1.0.0
git push origin v1.0.0

# Build packages (will use git tag for version)
.\scripts\Build-NuGetPackages.ps1

# Or override version
.\scripts\Build-NuGetPackages.ps1 -Version "1.0.1"
```

### Pre-release Versions

```powershell
# Build preview version
.\scripts\Build-NuGetPackages.ps1 -Version "1.0.0-preview.1"

# Beta version
.\scripts\Build-NuGetPackages.ps1 -Version "1.0.0-beta.2"
```

---

## Consuming Packages

### In a New Project

```powershell
# Create new project
dotnet new console -n MyStampClient
cd MyStampClient

# Add package
dotnet add package StampService.ClientLib --version 1.0.0

# Restore and build
dotnet restore
dotnet build
```

### Example Code

```csharp
using StampService.ClientLib;
using StampService.Core.Models;

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
Console.WriteLine($"Signature: {response.Signature}");
```

---

## Troubleshooting

### Cannot find package

```powershell
# List configured sources
dotnet nuget list source

# Add source if missing
dotnet nuget add source {URL} --name {Name}

# Clear local cache
dotnet nuget locals all --clear
```

### Authentication failures

```powershell
# Update credentials
dotnet nuget update source {Name} --username {User} --password {PAT}

# Or remove and re-add
dotnet nuget remove source {Name}
dotnet nuget add source {URL} --name {Name} --username {User} --password {PAT}
```

### Package not updating

```powershell
# Clear NuGet cache
dotnet nuget locals all --clear

# Remove package and re-add
dotnet remove package StampService.ClientLib
dotnet add package StampService.ClientLib --version {NewVersion}
```

---

## Best Practices

### For Package Authors

1. **Always increment version** for each release
2. **Include README** in packages (already configured)
3. **Use semantic versioning** consistently
4. **Test packages locally** before publishing
5. **Document breaking changes** in release notes
6. **Keep packages small** - don't include unnecessary files

### For Package Consumers

1. **Pin versions** in production: `<PackageReference Include="StampService.ClientLib" Version="1.0.0" />`
2. **Use version ranges** for development: `Version="1.*"`
3. **Check for updates** regularly: `dotnet list package --outdated`
4. **Review release notes** before updating
5. **Test updates** in non-production first

---

## Summary: Which Method to Use?

| Method | Best For | Complexity | Cost |
|--------|----------|------------|------|
| **Local Folder** | Solo dev, testing | ? Easy | Free |
| **Network Share** | Small teams, corporate | ?? Moderate | Free |
| **Azure Artifacts** | Enterprise, CI/CD | ??? Complex | $ (beyond free tier) |
| **GitHub Packages** | GitHub users, OSS | ?? Moderate | Free (public) |
| **NuGet.org** | Public/OSS | ?? Moderate | Free |

**Recommendation**:
- ?? **Development/Testing**: Local Folder
- ?? **Team (5-20 people)**: Network Share
- ?? **Enterprise**: Azure Artifacts
- ?? **GitHub Projects**: GitHub Packages
- ?? **Open Source**: NuGet.org

---

## Quick Reference Commands

```powershell
# Build packages
.\scripts\Build-NuGetPackages.ps1 -Version "1.0.0"

# Add NuGet source
dotnet nuget add source {PATH/URL} --name {Name}

# List sources
dotnet nuget list source

# Push to source
dotnet nuget push package.nupkg --source {Name} --api-key {Key}

# Install package
dotnet add package StampService.ClientLib --version 1.0.0

# Update package
dotnet add package StampService.ClientLib --version 1.1.0

# Remove package
dotnet remove package StampService.ClientLib

# List installed packages
dotnet list package

# Clear NuGet cache
dotnet nuget locals all --clear
```

---

For more help, see:
- Official NuGet docs: https://docs.microsoft.com/nuget
- Azure Artifacts: https://docs.microsoft.com/azure/devops/artifacts
- GitHub Packages: https://docs.github.com/packages
