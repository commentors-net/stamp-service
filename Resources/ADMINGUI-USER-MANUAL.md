# Stamp Service Administrator Manual

## Table of Contents
1. [Introduction](#introduction)
2. [Getting Started](#getting-started)
3. [Main Dashboard](#main-dashboard)
4. [Creating Tokens](#creating-tokens)
5. [Importing Existing Mnemonics](#importing-existing-mnemonics)
6. [Managing Secrets](#managing-secrets)
7. [Backup & Recovery](#backup--recovery)
8. [Service Health Monitoring](#service-health-monitoring)
9. [Settings & Configuration](#settings--configuration)
10. [Keyboard Shortcuts](#keyboard-shortcuts)
11. [Troubleshooting](#troubleshooting)
12. [Best Practices](#best-practices)

---

## Introduction

### What is Stamp Service AdminGUI?

Stamp Service AdminGUI is a Windows desktop application that provides a user-friendly interface for managing cryptocurrency tokens, private keys, and sensitive data. It connects to the Stamp Service backend to securely store, retrieve, and manage secrets using industry-standard encryption.

### Key Features

- **Token Creation**: Generate new blockchain tokens with automatic key generation
- **Secret Management**: Store, view, search, and delete sensitive information
- **Backup & Recovery**: Create encrypted backup shares using Shamir's Secret Sharing
- **Service Monitoring**: Real-time service health and status information
- **Security**: Administrator privileges required, secure secret storage

### System Requirements

- **Operating System**: Windows 10/11 (64-bit)
- **Framework**: .NET 8.0 Runtime
- **Privileges**: Administrator rights required
- **Disk Space**: 50 MB minimum
- **RAM**: 512 MB minimum

---

## Getting Started

### Launching the Application

1. **From Desktop**: Double-click the "Stamp Service Manager" shortcut
2. **From Start Menu**: Search for "Stamp Service Manager" and click
3. **From Installation Folder**: Run `StampService.AdminGUI.exe`

> **Note**: If prompted by Windows User Account Control (UAC), click "Yes" to grant administrator privileges.

**[SCREENSHOT PLACEHOLDER: Application launch icon and UAC prompt]**

### First Launch

When you first launch the application:

1. The Stamp Service will automatically start in the background
2. The main dashboard will display the service status
3. You'll see an empty activity feed (no tokens created yet)

**[SCREENSHOT PLACEHOLDER: First launch main dashboard]**

### Administrator Privileges

The application requires administrator privileges to:
- Access the Windows service
- Store secrets in the Windows Registry
- Manage named pipes for inter-process communication
- Create backup files in protected directories

If the application is not running as administrator, you will see a warning message prompting you to restart with elevated privileges.

**[SCREENSHOT PLACEHOLDER: Administrator privilege warning dialog]**

---

## Main Dashboard

The main dashboard is your central hub for monitoring and managing the Stamp Service.

**[SCREENSHOT PLACEHOLDER: Full main dashboard with all sections labeled]**

### Dashboard Sections

#### 1. Service Status Panel (Top)
Displays real-time information about the Stamp Service:

- **Status Indicator**: Green (Running) or Red (Stopped)
- **Uptime**: How long the service has been running
- **Secrets Count**: Total number of stored secrets
- **Last Activity**: Timestamp of the most recent operation

**[SCREENSHOT PLACEHOLDER: Service status panel closeup]**

#### 2. Quick Action Buttons (Center)
Five buttons for common tasks:

**Row 1:**
- **Create New Token** (Blue) - Launch the token creation wizard
- **Import Existing Key** (Blue) - Import existing 12-word mnemonic

**Row 2:**
- **Manage Secrets** (Blue) - Open the secret manager
- **Backup & Recovery** (Orange) - Access backup and recovery tools

**Row 3:**
- **Service Health** (Teal, full width) - View detailed service diagnostics

**[SCREENSHOT PLACEHOLDER: Quick action buttons grid]**

#### 3. Recent Activity Feed (Bottom)
Shows the 10 most recent operations:

- Timestamp of each activity
- Description of the action performed
- Color-coded by operation type

**[SCREENSHOT PLACEHOLDER: Activity feed with sample entries]**

### Dashboard Actions

#### Refreshing Status
- **Manual Refresh**: Click the "Refresh Status" button in the status panel
- **Auto Refresh**: Status updates automatically every 30 seconds
- **Keyboard Shortcut**: Press `F5` to refresh

#### Navigating from Dashboard
Click any of the quick action buttons to navigate to the respective feature area. You can always return to the dashboard using the navigation menu or by closing the current view.

---

## Creating Tokens

The Create Token wizard guides you through generating a new blockchain token with automatic key generation and secure storage.

**[SCREENSHOT PLACEHOLDER: Create Token wizard step navigation]**

### Step 1: Token Details

**[SCREENSHOT PLACEHOLDER: Step 1 - Token Details form]**

#### Fields:
1. **Token Name** (Required)
   - Enter a unique, descriptive name
   - Examples: "MainToken", "PROD_ETH_Token", "Test_Token_1"
   - Must be unique across all stored secrets

2. **Network** (Required)
   - Select the blockchain network from dropdown:
     - Ethereum Mainnet
     - Ethereum Testnet (Goerli)
     - Binance Smart Chain
     - Polygon
     - Solana Mainnet
   - Solana Devnet

3. **Description** (Optional)
   - Add notes about the token's purpose
   - Example: "Production master contract for NFT marketplace"

#### Actions:
- **Next**: Proceed to key generation (requires Name and Network)
- **Cancel**: Exit wizard and return to dashboard

### Step 2: Key Generation

**[SCREENSHOT PLACEHOLDER: Step 2 - Key generation progress]**

The wizard automatically generates cryptographic keys:

#### Master Contract Keys
- **Mnemonic**: 12-word recovery phrase (BIP39 standard)
- **Private Key**: Hexadecimal private key
- **Address**: Public blockchain address

#### Proxy Contract Keys
- **Mnemonic**: Separate 12-word phrase for proxy
- **Private Key**: Proxy contract private key
- **Address**: Proxy contract public address

> **Important**: These keys are generated offline and stored securely in the Stamp Service. Never share your private keys or mnemonics.

#### Generation Progress
1. Generating Master mnemonic... ?
2. Deriving Master keys... ?
3. Generating Proxy mnemonic... ?
4. Deriving Proxy keys... ?
5. Storing secrets securely... ?

#### Actions:
- **Next**: Proceed to backup creation
- **Back**: Return to token details (discards generated keys)
- **Cancel**: Exit wizard (discards all data)

### Step 3: Backup Share Creation

**[SCREENSHOT PLACEHOLDER: Step 3 - Backup location selection]**

Automatically creates encrypted backup shares for recovery:

#### Backup Configuration
- **Default Shares**: 5 backup files
- **Default Threshold**: 3 shares needed for recovery
- **Location**: `C:\ProgramData\StampService\Backups\[TokenName]_[Timestamp]`

#### Backup Files Created
Each backup contains:
- `[TokenName]_share_1.txt`
- `[TokenName]_share_2.txt`
- `[TokenName]_share_3.txt`
- `[TokenName]_share_4.txt`
- `[TokenName]_share_5.txt`

> **Security Note**: Store these shares in different physical locations. You need any 3 shares to recover the keys.

#### Actions:
- **Open Backup Folder**: View the created backup files
- **Next**: Proceed to summary
- **Back**: Return to key generation

### Step 4: Summary & Completion

**[SCREENSHOT PLACEHOLDER: Step 4 - Summary and completion]**

Review the created token:

#### Summary Information
- Token name
- Network selected
- Number of secrets stored (Master + Proxy = 2)
- Backup location
- Total shares created
- Recovery threshold

#### Next Steps Checklist
- [ ] Backup shares stored in separate locations
- [ ] Master and Proxy addresses recorded
- [ ] Access tested via Secret Manager

#### Actions:
- **Finish**: Complete wizard and return to dashboard
- **Back**: Return to backup step
- **View in Secret Manager**: Open Secret Manager and filter to your new token

### After Token Creation

Once completed:
- Token secrets are stored in the service
- Activity logged in dashboard feed
- Secrets available in Secret Manager
- Backup shares saved to disk

---

## Importing Existing Mnemonics

The Import Mnemonic wizard allows you to add existing 12-word mnemonic phrases to the Stamp Service for secure storage and management.

**[SCREENSHOT PLACEHOLDER: Import Mnemonic wizard]**

### When to Use Import

Use the Import wizard when you:
- Already have existing mnemonic phrases that need secure storage
- Want to migrate keys from another wallet or system
- Need to store recovery phrases for existing blockchain accounts
- Want to centralize key management in Stamp Service

> **Security Note**: Only import mnemonics on trusted, secure computers. Never share your mnemonics or enter them on untrusted websites.

### Accessing the Import Wizard

Three ways to open:
1. **Dashboard Button**: Click "Import Existing Key" in Quick Actions
2. **Keyboard Shortcut**: Press `Ctrl+I`
3. **Menu**: (if menu bar exists)

**[SCREENSHOT PLACEHOLDER: Import Existing Key button highlighted]**

---

### Step 1: Mnemonic Input

**[SCREENSHOT PLACEHOLDER: Step 1 - Mnemonic input form]**

#### Fields:

1. **Secret Name** (Required)
   - Enter a unique, descriptive identifier
   - Examples: "MyWallet_Master", "PROD_ETH_Contract", "Personal_Backup"
   - Must be unique across all stored secrets
   - Used to identify the key in Secret Manager

2. **12-Word Mnemonic Phrase** (Required)
   - Enter your existing mnemonic phrase
   - Must be exactly 12 words
   - Words separated by spaces
   - BIP39 standard English wordlist
   - Can paste from clipboard or type manually

   **Real-time Validation:**
   - Word count display shows current count
   - Turns green when exactly 12 words detected
   - Validates words against BIP39 wordlist

3. **Key Type** (Required)
   - **Master Contract Key**: Main contract signing key
   - **Master Proxy Key**: Proxy contract key
   - Select based on your key's intended use

4. **Network** (Required)
   - Select the blockchain network:
     - Ethereum Mainnet
     - Ethereum Testnet (Sepolia)
 - Polygon Mainnet
     - Polygon Testnet (Mumbai)
     - Binance Smart Chain Mainnet
     - Binance Smart Chain Testnet
     - Solana Mainnet
     - Solana Devnet
     - Other Network

#### Security Warning

A prominent warning is displayed:
- Only enter mnemonics on trusted computers
- Never share mnemonics with anyone
- Don't enter on untrusted websites
- Ensure administrator privileges are active

#### Actions:
- **Next**: Validates and proceeds to verification (requires all fields)
- **Cancel**: Exit wizard without importing
- **Back**: (disabled on first step)

---

### Step 2: Verification & Storage

**[SCREENSHOT PLACEHOLDER: Step 2 - Verification progress]**

The wizard automatically performs these steps:

#### Automated Process

1. **? Mnemonic Validation**
   - Verifies 12-word format
   - Validates words against BIP39 English wordlist
   - Ensures proper format and structure
   - Shows error if invalid

2. **? Wallet Derivation**
   - Derives HD wallet from mnemonic (BIP32/BIP44)
   - Calculates account 0 (standard derivation path)
   - Extracts public address
   - Retrieves private key
   - **Address displayed**: Shows derived blockchain address

3. **? Secure Storage**
   - Encrypts mnemonic with DPAPI (Windows Data Protection API)
   - Stores in Windows Registry (LocalMachine scope)
   - Saves metadata (network, address, key type, timestamp)
   - Confirms successful storage

#### Progress Indicators

Each step shows:
- Checkmark (?) when complete
- Progress description
- Any relevant information (e.g., derived address)

#### If Errors Occur

- Clear error message displayed
- Returns to Step 1 for correction
- No data stored if validation fails
- Can modify input and retry

#### Actions:
- **Next**: Proceed to summary (automatic after successful storage)
- **Back**: Return to Step 1 (loses current progress)
- **Cancel**: Exit wizard (no data saved)

---

### Step 3: Completion Summary

**[SCREENSHOT PLACEHOLDER: Step 3 - Success summary]**

Review the imported mnemonic:

#### Summary Information

Displays:
- **Secret Name**: The identifier you entered
- **Key Type**: Master Contract or Master Proxy
- **Network**: Selected blockchain network
- **Address**: Derived public blockchain address (full address shown)

#### Success Confirmation

- ? Mnemonic imported and stored securely
- Green checkmark with success message
- Confirmation that encryption was successful

#### Next Steps Recommendations

The wizard suggests:
1. **? View in Secret Manager**: Open Secret Manager to verify
2. **? Create backup shares**: Use Backup & Recovery to create shares
3. **? Test with small transaction**: Verify the address is correct

**[SCREENSHOT PLACEHOLDER: Next steps card]**

#### Actions:
- **Finish**: Complete wizard and return to dashboard
- **Back**: Return to verification step (view details again)

---

### After Import Completion

Once you click Finish:

1. **Dashboard Updated**:
   - Activity feed shows: "Mnemonic '[SecretName]' imported"
   - Service status refreshes
   - Secret count increments

2. **Secret Stored**:
   - Available in Secret Manager
   - Searchable by name, network, or address
   - Can be viewed, copied, or deleted
   - Full metadata stored

3. **Recommended Actions**:
   - Open Secret Manager to verify
   - Create backup shares immediately
   - Test the key with a small transaction
   - Document the import in your records

**[SCREENSHOT PLACEHOLDER: Dashboard after import]**

---

### Import Examples

#### Example 1: Import Ethereum Mainnet Master Key

**Scenario:** You have an existing Ethereum wallet mnemonic you want to manage

```
Steps:
1. Click "Import Existing Key" or press Ctrl+I
2. Secret Name: "Personal_ETH_Master"
3. Paste 12-word mnemonic
4. Key Type: Master Contract Key
5. Network: Ethereum Mainnet
6. Click Next ? validates and stores
7. Review derived address (verify it matches your wallet)
8. Click Finish
9. Verify in Secret Manager
```

#### Example 2: Import Testnet Proxy Key

**Scenario:** Import a Solana testnet key for development

```
Steps:
1. Open Import Wizard (Ctrl+I)
2. Secret Name: "DEV_SOL_Proxy"
3. Enter 12-word mnemonic
4. Key Type: Master Proxy Key
5. Network: Solana Devnet
6. Complete wizard
7. Address displayed: [Solana address]
8. Create backup shares
9. Test with devnet tokens
```

#### Example 3: Import Multiple Keys

**Scenario:** Import several keys from different networks

```
For each key:
1. Open Import Wizard
2. Use descriptive naming: "PROD_POLY_Master", "TEST_BSC_Proxy", etc.
3. Select correct network for each
4. Verify addresses after import
5. Create backup shares for critical keys
6. Document in external inventory
```

---

### Import Validation

#### Mnemonic Validation Rules

Your mnemonic must meet these criteria:
- **Exactly 12 words** (15/18/21/24 not currently supported)
- **Valid BIP39 words**: All words from English wordlist
- **Proper format**: Words separated by spaces
- **No typos**: All words spelled correctly

#### Common Validation Errors

**"Mnemonic must be exactly 12 words"**
- **Cause**: Too many or too few words
- **Solution**: Count words, check for extra spaces

**"Invalid mnemonic phrase"**
- **Cause**: Words not in BIP39 wordlist, typos
- **Solution**: Verify each word, check spelling

**"Secret name already exists"**
- **Cause**: Name conflicts with existing secret
- **Solution**: Use a different, unique name

**"Failed to store secret"**
- **Cause**: Service connection issue, permissions
- **Solution**: Verify service is running, check admin privileges

---

### Security Considerations

#### Best Practices

**? DO:**
- Only use on trusted, secure computers
- Verify derived address matches expected
- Create backup shares immediately after import
- Test with small amounts before large transactions
- Keep original mnemonic backup separately
- Document import in secure external records

**? DON'T:**
- Enter mnemonics on public/shared computers
- Share mnemonics via email, chat, or cloud
- Screenshot the mnemonic during import
- Import on computers with malware
- Use the same mnemonic for multiple purposes
- Store mnemonics in plain text files

#### Encryption Details

Imported mnemonics are secured using:
- **DPAPI**: Windows Data Protection API (LocalMachine scope)
- **Registry Storage**: Encrypted in Windows Registry
- **Administrator-only Access**: Requires admin privileges
- **No Plain Text**: Never stored unencrypted
- **Metadata**: Network, address, timestamps stored with encryption

#### Verification Steps

After importing, verify:
1. ? Derived address matches your expected address
2. ? Secret appears in Secret Manager
3. ? Can view secret details (reveals correctly)
4. ? Metadata is accurate (network, key type)
5. ? Backup shares created
6. ? Test transaction succeeds (small amount)

---

### Troubleshooting Import Issues

#### Issue: Word count stays orange/wrong

**Symptoms:**
- Word count doesn't reach 12
- Extra spaces, line breaks

**Solution:**
1. Remove extra spaces, tabs, line breaks
2. Paste into plain text editor first
3. Copy again and paste into wizard
4. Verify 12 words separated by single spaces

#### Issue: "Invalid mnemonic phrase" error

**Symptoms:**
- Mnemonic looks correct but validation fails

**Solution:**
1. Verify all words are BIP39 English words
2. Check for typos (common: "work" vs "word", "from" vs "form")
3. Try typing manually instead of pasting
4. Verify original source of mnemonic
5. Check if it's actually 12 words (not 24-word phrase split)

#### Issue: Derived address doesn't match expected

**Symptoms:**
- Import succeeds but address is different

**Possible causes:**
1. **Wrong network selected**: Ethereum vs Solana give different addresses
2. **Different derivation path**: Other wallets may use non-standard paths
3. **Wrong mnemonic**: Double-check source
4. **Passphrase expected**: Some mnemonics use BIP39 passphrase (25th word)

**Solution:**
1. Verify correct network selected
2. Compare with original wallet's derivation path
3. Check if original wallet uses BIP39 passphrase (not currently supported)
4. Try test transaction to verify

#### Issue: Import succeeds but can't find in Secret Manager

**Symptoms:**
- Import completes successfully
- Secret not visible in list

**Solution:**
1. Click "Refresh" in Secret Manager
2. Check search filter isn't hiding it
3. Search by name in Secret Manager
4. Restart AdminGUI
5. Check service is running

#### Issue: Permission errors during storage

**Symptoms:**
- "Access denied" or permission errors

**Solution:**
1. Run AdminGUI as Administrator:
   - Right-click app ? Run as Administrator
2. Verify service is running
3. Check Windows Registry permissions:
   - HKLM\SOFTWARE\StampService
4. Check Event Viewer for detailed errors

---

## Managing Secrets

The Secret Manager provides a comprehensive interface for viewing, searching, and managing all stored secrets.

**[SCREENSHOT PLACEHOLDER: Secret Manager main view]**

### Secret Manager Interface

#### Main Components

1. **Search Bar** (Top)
   - Filter secrets by name, type, network, or address
   - Real-time search as you type
   - Case-insensitive matching
   - Keyboard shortcut: `Ctrl+F`

2. **Secrets Grid** (Center)
   - Columns: Name | Type | Network | Address | Created
   - Sortable by clicking column headers
   - Multi-select support
   - Double-click to view details

3. **Action Buttons** (Bottom)
   - **Add Secret**: Create a new secret manually
   - **View Details**: Open selected secret
   - **Delete Secret**: Remove selected secret(s)
   - **Refresh**: Reload secrets from service

**[SCREENSHOT PLACEHOLDER: Secrets grid with sample data]**

### Viewing Secret Details

**[SCREENSHOT PLACEHOLDER: Secret Details window]**

#### Opening Details
1. Select a secret in the grid
2. Click "View Details" button, OR
3. Double-click the secret row, OR
4. Press `Enter` with secret selected

#### Details Window Sections

##### Basic Information (Top)
- **Name**: Secret identifier
- **Type**: MasterMnemonic, ProxyMnemonic, PrivateKey, etc.
- **Network**: Associated blockchain network
- **Address**: Public address (if applicable)
- **Created**: Timestamp of creation

##### Secret Value (Center)
- Initially hidden with asterisks (**********)
- **Reveal Button**: Click to show actual secret
- **Hide Button**: Click to hide secret again
- Protected value in monospace font

> **Security Warning**: Only reveal secrets when necessary and ensure no one is looking over your shoulder.

**[SCREENSHOT PLACEHOLDER: Secret value revealed]**

##### Metadata (Bottom)
Displays all custom metadata key-value pairs:
- Contract Type
- Deployment Status
- Version
- Purpose
- Tags
- Custom fields

##### Action Buttons
- **Copy to Clipboard**: Copy secret value (?? sensitive!)
- **Export to File**: Save secret to encrypted file
- **Close**: Close details window

**[SCREENSHOT PLACEHOLDER: Metadata section]**

### Searching Secrets

#### Search Syntax
Enter any text to filter by:
- Secret name (e.g., "MainToken")
- Type (e.g., "Mnemonic")
- Network (e.g., "Ethereum")
- Address (e.g., "0x742d...")

#### Search Examples
- `MainToken` - Find all secrets for MainToken
- `Ethereum` - Find all Ethereum network secrets
- `Master` - Find all master contract secrets
- `0x742` - Find secret with matching address

**[SCREENSHOT PLACEHOLDER: Search in action with results]**

### Adding a New Secret

**[SCREENSHOT PLACEHOLDER: Add Secret dialog]**

#### Steps:
1. Click "Add Secret" button
2. Fill in the form:
   - **Name**: Unique identifier
   - **Type**: Select from dropdown
   - **Value**: Enter the secret (encrypted on save)
   - **Network**: Optional network association
   - **Metadata**: Add custom key-value pairs

3. Click "Save" to store

> **Note**: Secret values are automatically encrypted before storage.

### Deleting Secrets

**[SCREENSHOT PLACEHOLDER: Delete confirmation dialog]**

#### To Delete a Secret:
1. Select secret(s) in the grid
2. Click "Delete Secret" button
3. Confirm deletion in warning dialog

> **Warning**: Deletion is permanent! Ensure you have backups before deleting critical secrets.

#### Batch Deletion
- Hold `Ctrl` and click to select multiple secrets
- Hold `Shift` and click to select range
- Click "Delete Secret" to remove all selected

### Exporting Secrets

**[SCREENSHOT PLACEHOLDER: Export options dialog]**

#### Export Options:
1. **Single Secret**: From Details window, click "Export to File"
2. **Multiple Secrets**: Select secrets and choose "Export Selected"
3. **All Secrets**: Use "Export All" option

#### Export Formats:
- **Encrypted JSON**: Password-protected JSON file
- **Plain Text**: Unencrypted (?? not recommended)
- **CSV**: For spreadsheet import

---

## Backup & Recovery

The Backup & Recovery view provides tools for creating encrypted backup shares and recovering secrets from existing shares.

**[SCREENSHOT PLACEHOLDER: Backup & Recovery full view]**

### Understanding Shamir's Secret Sharing

Shamir's Secret Sharing (SSS) splits a secret into multiple shares where:
- **Total Shares**: Number of pieces to create (3-10)
- **Threshold**: Minimum shares needed to recover (2-5)
- **Security**: Any fewer than threshold shares reveals nothing

#### Example Configurations

| Shares | Threshold | Use Case |
|--------|-----------|----------|
| 3 | 2 | Personal backup (home, office, bank vault) |
| 5 | 3 | Team backup (distributed among team members) |
| 7 | 4 | Enterprise backup (high security) |

> **Best Practice**: Use 5 shares with threshold 3 for most scenarios.

---

### Creating Backup Shares

**[SCREENSHOT PLACEHOLDER: Create Backup section]**

#### Step-by-Step Process

##### 1. Configure Share Settings
- **Total Shares**: Select from dropdown (3-10)
  - Default: 5 shares
  - More shares = more distribution options
  
- **Threshold**: Select from dropdown (2 to Total-1)
  - Default: 3 shares required
  - Higher threshold = more security, less convenience

**[SCREENSHOT PLACEHOLDER: Share configuration dropdowns]**

##### 2. Select Output Folder
- Click "Select Output Folder" button
- Browse to desired backup location
- Recommended locations:
  - External USB drive
  - Network drive
  - Cloud storage folder
  - Multiple locations for different shares

**[SCREENSHOT PLACEHOLDER: Folder picker dialog]**

##### 3. Create Shares
- Click "Create Backup Shares" button
- Progress bar shows creation status
- Completion message displays total shares created

**[SCREENSHOT PLACEHOLDER: Backup creation progress]**

#### Backup Output

The backup folder contains:
```
[TokenName]_[Timestamp]/
??? TokenName_share_1.txt
??? TokenName_share_2.txt
??? TokenName_share_3.txt
??? TokenName_share_4.txt
??? TokenName_share_5.txt
??? README.txt (instructions for recovery)
```

**[SCREENSHOT PLACEHOLDER: Backup folder contents]**

Each share file contains:
- Encrypted share data
- Share number (1 of 5)
- Threshold requirement (3 shares needed)
- Creation timestamp
- Recovery instructions

#### Post-Backup Actions

After creating backups:

1. **Distribute Shares**
   - Store each share in a different location
   - Label shares clearly (Share 1 of 5, etc.)
   - Record storage locations (not on computer!)

2. **Test Recovery**
   - Select any 3 shares
   - Perform test recovery
   - Verify recovered secret matches original

3. **Secure Storage Locations**
   - Physical: Safe deposit box, fireproof safe, trusted person
   - Digital: Encrypted cloud storage, password manager
   - Hybrid: Mix of physical and digital locations

**[SCREENSHOT PLACEHOLDER: Share distribution diagram]**

---

### Recovering from Backup Shares

**[SCREENSHOT PLACEHOLDER: Recovery section]**

#### Recovery Process

##### 1. Add Share Files

**Method 1: File Picker**
- Click "Add Share Files" button
- Browse to share location
- Select multiple share files (Ctrl+Click)
- Click "Open"

**Method 2: Drag & Drop** (if enabled)
- Drag share files from Windows Explorer
- Drop onto the share list area

**[SCREENSHOT PLACEHOLDER: Add share files dialog]**

##### 2. Review Added Shares

The share list displays:
- Share file names
- File paths
- Share numbers (parsed from file)
- Status indicators

**[SCREENSHOT PLACEHOLDER: Share list with loaded files]**

##### 3. Check Status Indicator

Status bar shows:
- **Shares Provided**: Number of shares added
- **Shares Required**: Threshold needed
- **Status**: 
  - ?? "Need X more shares" (insufficient)
  - ? "Ready to recover" (sufficient)

**[SCREENSHOT PLACEHOLDER: Status indicator variations]**

##### 4. Start Recovery

- Click "Start Recovery" button (enabled when threshold met)
- Progress dialog shows recovery process
- Success message confirms recovery
- Recovered secrets are stored in service

**[SCREENSHOT PLACEHOLDER: Recovery progress dialog]**

#### Recovery Options

**Remove Share**: Remove a specific share from list
**Clear All**: Remove all shares and start over
**Refresh**: Re-scan share files for changes

#### Recovery Verification

After successful recovery:

1. **Check Secret Manager**
   - Navigate to Secret Manager
   - Search for recovered secrets
   - Verify all expected secrets present

2. **Test Functionality**
   - View secret details
   - Verify addresses match expected values
   - Test token functionality if applicable

**[SCREENSHOT PLACEHOLDER: Recovered secrets in Secret Manager]**

---

### Backup Best Practices

#### Distribution Strategy

**The 3-2-1-1 Rule**:
- **3** copies of your data (original + 2 backups)
- **2** different media types (USB + cloud)
- **1** copy offsite (not in your home/office)
- **1** copy offline (air-gapped)

#### Storage Recommendations

| Location Type | Examples | Pros | Cons |
|---------------|----------|------|------|
| Physical | Safe, deposit box | No internet risk | Fire/theft risk |
| Digital Cloud | Google Drive, Dropbox | Accessible anywhere | Internet dependent |
| Trusted Person | Family, lawyer | Redundancy | Trust required |
| Hardware | USB drive, NAS | Full control | Hardware failure |

**[SCREENSHOT PLACEHOLDER: Storage location diagram]**

#### Labeling Shares

Label each share file/container with:
- Share number (e.g., "Share 2 of 5")
- Threshold requirement (e.g., "Need 3 to recover")
- Creation date
- Token/secret identifier (if not sensitive)
- **DO NOT** include passwords or hints

#### Periodic Backup Verification

Schedule regular backup tests:
- **Monthly**: Verify share files are accessible
- **Quarterly**: Perform test recovery
- **Annually**: Refresh backups (create new shares)

#### Emergency Recovery Plan

Document your recovery process:
1. Where each share is stored
2. How to access each storage location
3. Who to contact for shares held by others
4. Recovery procedure steps
5. Emergency contact information

> **Store this plan separately from the shares!**

---

## Service Health Monitoring

The Service Health view provides real-time diagnostics and monitoring of the Stamp Service.

**[SCREENSHOT PLACEHOLDER: Service Health view]**

### Service Status Dashboard

#### Status Indicators

**[SCREENSHOT PLACEHOLDER: Status indicator panel]**

- **Service State**: Running / Stopped / Error
- **Connection Status**: Connected / Disconnected
- **IPC Status**: Named pipe connection status
- **Secret Store Status**: Registry access status

#### System Metrics

**[SCREENSHOT PLACEHOLDER: System metrics panel]**

- **Uptime**: Total service run time
- **Memory Usage**: Service RAM consumption
- **CPU Usage**: Service processor usage
- **Secret Count**: Total secrets stored
- **Operation Count**: Total operations performed

#### Recent Operations

**[SCREENSHOT PLACEHOLDER: Recent operations log]**

Real-time log of service activities:
- Timestamp
- Operation type (Store, Retrieve, Delete, List)
- Status (Success, Failed, Denied)
- Details/Error messages

### Service Controls

**[SCREENSHOT PLACEHOLDER: Service control buttons]**

#### Available Actions

1. **Restart Service**
   - Stops and starts the service
   - Clears temporary caches
   - Re-establishes IPC connections
   - Use if service becomes unresponsive

2. **Clear Cache**
   - Clears in-memory secret cache
   - Forces reload from registry
   - Use if secrets appear out of sync

3. **View Logs**
   - Opens Windows Event Viewer
   - Filters to Stamp Service events
   - Shows detailed diagnostic information

4. **Test Connection**
 - Sends ping to service
   - Verifies IPC communication
   - Displays latency information

### Diagnostics Panel

**[SCREENSHOT PLACEHOLDER: Diagnostics panel]**

#### System Information

- **Service Version**: Current service version
- **Client Version**: AdminGUI version
- **IPC Pipe Name**: Named pipe identifier
- **Registry Path**: Secret storage location
- **Installation Path**: Service executable location

#### Health Checks

Automated tests:
- ? Service is running
- ? IPC connection established
- ? Registry permissions verified
- ? Encryption provider available
- ?? Warnings (if any)
- ? Errors (if any)

### Troubleshooting Tools

**[SCREENSHOT PLACEHOLDER: Troubleshooting tools section]**

#### Connection Test
- Tests IPC named pipe connection
- Measures response latency
- Reports: Success / Timeout / Access Denied / Error

#### Permission Check
- Verifies administrator privileges
- Checks registry access rights
- Validates service permissions

#### Secret Store Integrity
- Counts secrets in registry
- Validates encryption
- Checks for corruption

---

## Settings & Configuration

The Settings window allows you to customize the AdminGUI behavior and appearance.

**[SCREENSHOT PLACEHOLDER: Settings window]**

### General Settings

**[SCREENSHOT PLACEHOLDER: General settings tab]**

#### Application Behavior

- **Start with Windows**
  - ? Launch AdminGUI on Windows startup
  - ? Start minimized to system tray

- **Confirmations**
  - ? Confirm before deleting secrets
  - ? Confirm before revealing secrets
  - ? Confirm on application exit

- **Auto-Refresh**
  - ? Enable automatic status refresh
  - Interval: `30` seconds (5-300)

#### Default Backup Settings

- **Total Shares**: `5` (3-10)
- **Threshold**: `3` (2-Total-1)
- **Output Folder**: `C:\ProgramData\StampService\Backups`

### Appearance Settings

**[SCREENSHOT PLACEHOLDER: Appearance settings tab]**

#### Theme

- ? Dark Theme
- ? Light Theme
- ?? System Theme (follows Windows)

#### Colors

- **Primary Color**: Material Design palette
- **Accent Color**: Button and highlight color
- **Preview**: Live preview of selected theme

#### Font

- **Font Family**: Segoe UI (default)
- **Font Size**: 12 pt (8-24)
- **Use Monospace for Secrets**: ?

### Security Settings

**[SCREENSHOT PLACEHOLDER: Security settings tab]**

#### Secret Display

- **Auto-Hide Secrets After**: `30` seconds (0=never)
- **Require Re-Authentication**: ? Before viewing secrets
- **Hide Secrets in Screenshots**: ? (Windows 11 only)

#### Clipboard

- **Auto-Clear Clipboard After**: `60` seconds
- **Warn When Copying Secrets**: ?
- **Disable Copy to Clipboard**: ?

#### Audit

- **Log All Secret Access**: ?
- **Log Search Queries**: ?
- **Export Audit Log**: [Button]

### Network Settings

**[SCREENSHOT PLACEHOLDER: Network settings tab]**

#### Service Connection

- **IPC Pipe Name**: `StampServicePipe` (default)
- **Connection Timeout**: `5000` ms
- **Retry Attempts**: `3`

#### Default Networks

Customize network dropdown options:
- ? Ethereum Mainnet
- ? Ethereum Testnet
- ? Binance Smart Chain
- ? Polygon
- ? Solana Mainnet
- ? Solana Devnet
- ? Custom Networks...

### Advanced Settings

**[SCREENSHOT PLACEHOLDER: Advanced settings tab]**

#### Developer Options

- ? Enable Debug Logging
- ? Show Internal IPC Messages
- ? Display Error Stack Traces

#### Performance

- **Secret Cache Size**: `100` entries
- **Cache Expiration**: `300` seconds
- **Lazy Load Secrets**: ?

#### Maintenance

- **Clear Cache**: [Button]
- **Reset to Defaults**: [Button]
- **Repair Registry Permissions**: [Button]

### Applying Settings

**[SCREENSHOT PLACEHOLDER: Settings action buttons]**

- **Save**: Save changes and close
- **Apply**: Save without closing
- **Cancel**: Discard changes and close
- **Reset**: Revert to last saved settings

---

## Keyboard Shortcuts

Master these keyboard shortcuts for efficient navigation and operation.

**[SCREENSHOT PLACEHOLDER: Keyboard shortcuts reference card]**

### Global Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+N` | Create New Token |
| `Ctrl+I` | Import Existing Mnemonic |
| `Ctrl+M` | Open Secret Manager |
| `Ctrl+B` | Open Backup & Recovery |
| `Ctrl+H` | Open Service Health |
| `Ctrl+,` | Open Settings |
| `F5` | Refresh Current View |
| `Alt+F4` | Exit Application |
| `Esc` | Close Dialog/Cancel |

### Secret Manager Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+F` | Focus Search Box |
| `Enter` | View Selected Secret |
| `Delete` | Delete Selected Secret(s) |
| `Ctrl+C` | Copy Selected Secret Name |
| `Ctrl+A` | Select All Secrets |
| `Ctrl+R` | Refresh Secret List |
| `?/?` | Navigate Secret List |

### Secret Details Window

| Shortcut | Action |
|----------|--------|
| `Ctrl+H` | Toggle Hide/Reveal Secret |
| `Ctrl+C` | Copy Secret to Clipboard |
| `Ctrl+E` | Export Secret to File |
| `Esc` | Close Window |

### Wizard Navigation

| Shortcut | Action |
|----------|--------|
| `Alt+N` | Next Step |
| `Alt+B` | Previous Step |
| `Alt+C` | Cancel Wizard |
| `Alt+F` | Finish Wizard |

### Backup & Recovery

| Shortcut | Action |
|----------|--------|
| `Ctrl+O` | Add Share Files |
| `Delete` | Remove Selected Share |
| `Ctrl+Shift+Delete` | Clear All Shares |
| `Ctrl+S` | Start Recovery |

---

## Troubleshooting

Common issues and their solutions.

### Application Won't Start

**[SCREENSHOT PLACEHOLDER: Common error dialogs]**

#### Issue: "Application requires administrator privileges"

**Solution:**
1. Right-click `StampService.AdminGUI.exe`
2. Select "Run as administrator"
3. Or: Set to always run as admin in Properties

#### Issue: ".NET Runtime not found"

**Solution:**
1. Download .NET 8.0 Desktop Runtime
2. Install from: https://dotnet.microsoft.com/download
3. Restart computer
4. Launch application

#### Issue: "Service connection failed"

**Solution:**
1. Open Services (Win+R, type `services.msc`)
2. Find "Stamp Service"
3. Ensure status is "Running"
4. If stopped, right-click and select "Start"
5. Restart AdminGUI

### Secret Manager Issues

#### Issue: Secrets not appearing

**Solution:**
1. Click "Refresh" button
2. Check Service Health status
3. Verify service is running
4. Check Windows Event Viewer for errors

#### Issue: Cannot reveal secret

**Solution:**
1. Verify you have administrator privileges
2. Check that secret exists in registry:
   - Open Registry Editor (Win+R, `regedit`)
   - Navigate to `HKLM\SOFTWARE\StampService\Secrets`
3. Try "Test Connection" in Service Health

#### Issue: Search not working

**Solution:**
1. Clear search box and try again
2. Check for typos in search term
3. Click "Refresh" to reload secrets
4. Restart application

### Backup & Recovery Issues

#### Issue: Cannot create backup shares

**Solution:**
1. Verify output folder is writable
2. Check disk space (need ~10 KB per share)
3. Try different output folder
4. Run as administrator

#### Issue: Recovery fails with "insufficient shares"

**Solution:**
1. Verify you have enough shares (check threshold)
2. Ensure share files are not corrupted
3. Check share files are from same backup set
4. Verify share file format is correct

#### Issue: Recovered secrets don't match

**Solution:**
1. Verify shares are from correct backup
2. Check share creation timestamp
3. Ensure no share files were edited
4. Try different combination of shares

### Service Health Issues

#### Issue: Service shows "Stopped"

**Solution:**
1. Open Services (`services.msc`)
2. Right-click "Stamp Service"
3. Select "Start"
4. If fails, check Event Viewer for errors

#### Issue: High memory usage

**Solution:**
1. Click "Clear Cache" in Service Health
2. Restart service
3. Check for memory leaks in Event Viewer
4. Reduce cache size in Settings

#### Issue: IPC connection timeout

**Solution:**
1. Verify named pipe name matches service
2. Check Windows Firewall isn't blocking
3. Restart service
4. Restart AdminGUI

### Performance Issues

#### Issue: Slow secret loading

**Solution:**
1. Enable "Lazy Load Secrets" in Settings
2. Increase cache size
3. Reduce auto-refresh interval
4. Clear cache and restart

#### Issue: UI freezing

**Solution:**
1. Don't reveal multiple secrets simultaneously
2. Reduce number of secrets displayed
3. Close other resource-intensive applications
4. Increase cache expiration time

### Error Messages

**[SCREENSHOT PLACEHOLDER: Common error messages]**

#### "Access Denied"

**Cause**: Insufficient permissions
**Solution**: Run as administrator, check registry permissions

#### "Service Unavailable"

**Cause**: Service not running or IPC failure
**Solution**: Start service, check Service Health

#### "Secret Not Found"

**Cause**: Secret was deleted or registry corruption
**Solution**: Refresh list, restore from backup

#### "Invalid Share File"

**Cause**: Share file corrupted or wrong format
**Solution**: Use original share file, check file integrity

---

## Best Practices

### Security Best Practices

#### 1. Access Control

- ? Only install on trusted, secured computers
- ? Lock computer when stepping away
- ? Don't reveal secrets when others can see screen
- ? Use screen privacy filters in public spaces
- ? Enable auto-hide secrets setting

#### 2. Secret Management

- ? Use descriptive, non-sensitive names
- ? Add metadata for context (without revealing secrets)
- ? Delete obsolete secrets immediately
- ? Rotate secrets periodically
- ? Never share secrets via email or chat

#### 3. Backup Strategy

- ? Create backups immediately after token creation
- ? Test recovery process before relying on backups
- ? Store shares in geographically diverse locations
- ? Update backups after any changes
- ? Document share storage locations (securely!)

#### 4. Clipboard Safety

- ? Enable auto-clear clipboard
- ? Never paste secrets in unsecured applications
- ? Clear clipboard manually after sensitive operations
- ? Be aware of clipboard monitoring software
- ? Prefer export-to-file over copy-paste

### Operational Best Practices

#### 1. Regular Maintenance

**Weekly:**
- Review recent activity log
- Verify service status
- Check disk space

**Monthly:**
- Test backup recovery
- Audit secret list (remove obsolete)
- Check for application updates

**Quarterly:**
- Refresh all backup shares
- Verify share storage locations
- Test emergency recovery procedures

#### 2. Token Creation

- ? Use naming convention (e.g., `ENV_NETWORK_PURPOSE`)
- ? Add comprehensive metadata
- ? Document token purpose and usage
- ? Create backups immediately
- ? Test token before production use

#### 2a. Mnemonic Import

- ? Only import on trusted, secure computers
- ? Verify derived address matches expected
- ? Use descriptive names (e.g., `Personal_ETH_Master`)
- ? Create backup shares immediately after import
- ? Test with small transaction before trusting
- ? Keep original mnemonic backup separately
- ? Never share or screenshot mnemonics
- ? Document import in secure external records

#### 3. Secret Organization

**Naming Conventions:**
- `PROD_ETH_Master` - Production Ethereum Master
- `TEST_SOL_Proxy` - Test Solana Proxy
- `DEV_BSC_Contract` - Development BSC Contract

**Metadata Standards:**
- `Environment`: PROD, TEST, DEV
- `Purpose`: Master, Proxy, Vault, Treasury
- `Owner`: Team or individual responsible
- `Created`: Auto-populated timestamp
- `LastUsed`: Track usage

#### 4. Documentation

Maintain external documentation:
- Token inventory spreadsheet
- Backup share location matrix
- Recovery contact list
- Network and address registry
- Change log

> **Important**: Store documentation separately from secrets!

### Disaster Recovery

#### Preparation

1. **Backup Strategy**
   - Create shares for all critical secrets
   - Distribute shares appropriately
   - Document share locations

2. **Recovery Documentation**
   - Write step-by-step recovery procedures
   - Identify required shares for each secret
   - List contact information for share holders

3. **Testing**
   - Quarterly recovery drills
   - Document test results
   - Update procedures based on lessons learned

#### Recovery Scenarios

**Scenario 1: Computer Failure**
1. Install AdminGUI on new computer
2. Collect backup shares
3. Use Recovery to restore secrets
4. Verify all secrets recovered
5. Update documentation

**Scenario 2: Secret Corruption**
1. Identify affected secrets
2. Locate corresponding backup shares
3. Recover from shares
4. Verify recovered secrets
5. Delete corrupted entries

**Scenario 3: Lost Shares**
1. Identify which shares are missing
2. Check if threshold still met with remaining shares
3. If yes: recover and create new backup
4. If no: initiate emergency procedures

#### Emergency Contacts

Maintain a list of:
- Share holders and contact information
- IT support contacts
- Crypto asset recovery services
- Legal counsel (if applicable)
- Insurance provider (if applicable)

---

## Appendix

### A. Network Information

| Network | Chain ID | RPC Endpoint | Explorer |
|---------|----------|--------------|----------|
| Ethereum Mainnet | 1 | https://mainnet.infura.io | etherscan.io |
| Ethereum Testnet | 5 | https://goerli.infura.io | goerli.etherscan.io |
| BSC Mainnet | 56 | https://bsc-dataseed.binance.org | bscscan.com |
| Polygon Mainnet | 137 | https://polygon-rpc.com | polygonscan.com |
| Solana Mainnet | - | https://api.mainnet-beta.solana.com | explorer.solana.com |

### B. File Locations

| Item | Location |
|------|----------|
| Application | `C:\Program Files\StampService\AdminGUI\` |
| Service | `C:\Program Files\StampService\` |
| Backups | `C:\ProgramData\StampService\Backups\` |
| Registry | `HKLM\SOFTWARE\StampService\Secrets` |
| Logs | Windows Event Viewer ? Application |

### C. Support Resources

- **Documentation**: `https://github.com/commentors-net/stamp-service`
- **Issue Tracker**: `https://github.com/commentors-net/stamp-service/issues`
- **Wiki**: `https://github.com/commentors-net/stamp-service/wiki`
- **Email**: [Support email if available]

### D. Glossary

- **BIP39**: Bitcoin Improvement Proposal 39, standard for mnemonic phrases
- **IPC**: Inter-Process Communication, method for processes to communicate
- **Mnemonic**: 12-word phrase for recovering private keys
- **Private Key**: Secret cryptographic key for signing transactions
- **Public Address**: Blockchain address derived from private key
- **Shamir's Secret Sharing**: Cryptographic method to split secrets
- **Threshold**: Minimum shares needed to recover original secret

### E. Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | TBD | Initial release |

---

## Quick Reference Card

**[SCREENSHOT PLACEHOLDER: Printable quick reference card]**

### Essential Shortcuts
- `Ctrl+N` - New Token
- `Ctrl+I` - Import Mnemonic
- `Ctrl+M` - Secrets
- `Ctrl+B` - Backup
- `F5` - Refresh

### Common Tasks
1. **Create Token**: Ctrl+N ? Fill form ? Next ? Next ? Finish
2. **Import Mnemonic**: Ctrl+I ? Enter name & 12 words ? Select type & network ? Next ? Finish
3. **View Secret**: Ctrl+M ? Select ? Enter ? Reveal
4. **Backup**: Ctrl+B ? Configure ? Select folder ? Create
5. **Recovery**: Ctrl+B ? Add shares ? Start recovery

### Emergency
- Check Service Health (Ctrl+H)
- Restart Service (Services.msc)
- Run as Administrator
- Collect backup shares

---

**End of User Manual**

*For additional support, consult the project documentation or submit an issue on GitHub.*
