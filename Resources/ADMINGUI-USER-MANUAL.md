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
9. [Audit Log Viewer](#audit-log-viewer)
10. [Settings & Configuration](#settings--configuration)
11. [Keyboard Shortcuts](#keyboard-shortcuts)
12. [Known Limitations](#known-limitations)
13. [Troubleshooting](#troubleshooting)
14. [Best Practices](#best-practices)

---

## Introduction

### What is Stamp Service AdminGUI?

Stamp Service AdminGUI is a Windows desktop application that provides a user-friendly interface for managing cryptocurrency tokens, private keys, and sensitive data. It connects to the Stamp Service backend to securely store, retrieve, and manage secrets using industry-standard encryption.

### Key Features

- **Token Creation**: Generate new blockchain tokens with automatic key generation
- **Mnemonic Import**: Import existing 12-word recovery phrases securely
- **Secret Management**: Store, view, search, and delete sensitive information
- **Export/Import**: Transfer secrets between systems with encryption options
- **Backup & Recovery**: Create encrypted backup shares using Shamir's Secret Sharing
- **Audit Logging**: Track all operations with comprehensive audit logs
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

### First Launch

When you first launch the application:

1. The Stamp Service will automatically start in the background
2. The main dashboard will display the service status
3. You'll see an empty activity feed (no tokens created yet)

### Administrator Privileges

The application requires administrator privileges to:
- Access the Windows service
- Store secrets in the Windows Registry
- Manage named pipes for inter-process communication
- Create backup files in protected directories

If the application is not running as administrator, you will see a warning message prompting you to restart with elevated privileges.

---

## Main Dashboard

The main dashboard is your central hub for monitoring and managing the Stamp Service.

<img width="979" height="686" alt="image" src="https://github.com/user-attachments/assets/289cf7dd-694c-45d2-8c5a-93ae65b14e29" />

### Dashboard Sections

#### 1. Service Status Panel (Top)
Displays real-time information about the Stamp Service:

- **Status Indicator**: Green (Running) or Red (Stopped)
- **Uptime**: How long the service has been running
- **Secrets Count**: Total number of stored secrets
- **Last Activity**: Timestamp of the most recent operation

<img width="908" height="201" alt="image" src="https://github.com/user-attachments/assets/849488ae-1096-4b0a-81e5-a9b73d214642" />


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

<img width="894" height="407" alt="image" src="https://github.com/user-attachments/assets/55eee8a4-b061-4a43-9bf0-e6c4d2983bd2" />

#### 3. Recent Activity Feed (Bottom)
Shows the 10 most recent operations:

- Timestamp of each activity
- Description of the action performed
- Color-coded by operation type

<img width="905" height="131" alt="image" src="https://github.com/user-attachments/assets/5413d5ab-85be-45fc-9ae3-0226ad7c5b12" />

### Dashboard Actions

#### Refreshing Status
- **Auto Refresh**: Status updates automatically every 30 seconds
- **Keyboard Shortcut**: Press `F5` to refresh

#### Navigating from Dashboard
Click any of the quick action buttons to navigate to the respective feature area. You can always return to the dashboard using the navigation menu or by closing the current view.

---

## Creating Tokens

The Create Token wizard guides you through generating a new blockchain token with automatic key generation and secure storage.

### Step 1: Token Details

<img width="831" height="685" alt="image" src="https://github.com/user-attachments/assets/65c9af23-7309-4a7a-a44c-d33923e4b9b5" />

#### Fields:
1. **Token Name** (Required)
   - Enter a unique, descriptive name
   - Examples: "MainToken", "PROD_ETH_Token", "Test_Token_1"
   - Must be unique across all stored secrets

2. **Network** (Required)
   - Select the blockchain network from dropdown:
     - Ethereum Mainnet
     - Ethereum Testnet (Sepolia)

3. **Description** (Optional)
   - Add notes about the token's purpose
   - Example: "Production master contract for NFT marketplace"

#### Actions:
- **Next**: Proceed to key generation (requires Name and Network)
- **Cancel**: Exit wizard and return to dashboard

### Step 2: Key Generation

<img width="684" height="586" alt="image" src="https://github.com/user-attachments/assets/14b8d506-23cf-490e-b829-e57f0a50b624" />

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

<img width="680" height="592" alt="image" src="https://github.com/user-attachments/assets/a30dcea0-a6f8-4d69-bfe6-edc383866790" />

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

<img width="683" height="595" alt="image" src="https://github.com/user-attachments/assets/6528e5b2-40ba-4c67-8cb7-3bdb2fe1fdc8" />

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

<img width="677" height="593" alt="image" src="https://github.com/user-attachments/assets/7440fbe6-5dad-4332-8867-f72ffa22f30d" />

### When to Use Import

Use the Import wizard when you:
- Already have existing mnemonic phrases that need secure storage
- Want to migrate keys from another wallet or system
- Need to store recovery phrases for existing blockchain accounts
- Want to centralize key management in Stamp Service

> **Security Note**: Only import mnemonics on trusted, secure computers. Never share your mnemonics or enter them on untrusted websites.

### Accessing the Import Wizard

Two ways to open:
1. **Dashboard Button**: Click "Import Existing Key" in Quick Actions
2. **Keyboard Shortcut**: Press `Ctrl+I`


---

### Step 1: Mnemonic Input

<img width="678" height="595" alt="image" src="https://github.com/user-attachments/assets/8a3562f3-d8d8-4081-a788-28957285ec50" />

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

<img width="681" height="585" alt="image" src="https://github.com/user-attachments/assets/2f56fb62-7cce-4a39-a94d-b374524bcf9b" />

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

<img width="686" height="586" alt="image" src="https://github.com/user-attachments/assets/18b3b3b7-aeee-409a-8b0c-6a7336cfde68" />

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

<img width="626" height="144" alt="image" src="https://github.com/user-attachments/assets/1ddb68ee-4e30-41cd-9933-d6c4e4ebfb8d" />


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

<img width="979" height="681" alt="image" src="https://github.com/user-attachments/assets/f5cfba07-609d-4831-bc24-39d20bc716b2" />

### Viewing Secret Details

<img width="584" height="598" alt="image" src="https://github.com/user-attachments/assets/7c5894e9-5437-40ef-866b-45d412923eb5" />

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

<img width="578" height="583" alt="image" src="https://github.com/user-attachments/assets/c63875f5-4d1b-4182-a389-205582d69a4b" />

<img width="581" height="596" alt="image" src="https://github.com/user-attachments/assets/d50bc07d-a987-4d65-bac3-f540532659d5" />


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

<img width="977" height="681" alt="image" src="https://github.com/user-attachments/assets/d4e3bce7-eb8c-47e8-95d5-905d0cff6f5f" />

### Adding a New Secret

You can manually add secrets to the service using the Add Secret dialog.

#### Steps to Add a Secret:

1. Click "**+ New Secret**" button in Secret Manager toolbar
2. The Add Secret dialog opens with the following fields:

**Required Fields:**
- **Secret Name**: Unique identifier for the secret
  - Must be unique across all secrets
  - Example: "Personal_API_Key", "Backup_Wallet_Key"
  
- **Secret Type**: Select from dropdown
  - Mnemonic (12-word recovery phrase)
  - Private Key (hexadecimal key)
  - API Key (service authentication)
  - Password (login credentials)
  - Other (custom secret type)
  
- **Secret Value**: The actual secret data
  - Entered in a secure text box
  - Masked during entry
  - Automatically encrypted before storage

**Optional Fields:**
- **Network**: Blockchain network (Ethereum, Solana, etc.)

- **Description/Notes**: Additional context or documentation

3. Click "**SAVE**" to store the secret
4. If a secret with the same name exists, you'll be prompted to:
   - Overwrite the existing secret
   - Cancel and choose a different name

**Security Features:**
- Values are never stored in plain text
- Encrypted using Windows DPAPI before storage
- Only accessible with administrator privileges
- Activity is logged to audit trail

### Exporting Secrets

Export secrets to transfer them between systems or create external backups.

#### Export Options:

**From Secret Manager:**
1. Select secrets in the grid (single or multiple)
2. Click "**Export**" button in the toolbar
3. Choose export scope:
   - **Selected Secrets Only**: Export just the selected items
   - **All Secrets**: Export entire secret store

**Export Formats:**
1. **Encrypted JSON (Recommended)**
   - Password-protected with AES-256 encryption
   - Includes metadata (name, type, network, timestamps)
   - File extension: `.encrypted`
   - **Security**: Requires strong password to decrypt
   - **Use case**: Transferring secrets securely

2. **Plain Text JSON (Not Secure)**
   - Unencrypted JSON format
   - Human-readable structure
   - File extension: `.json`
   - **?? WARNING**: Anyone with file access can read secrets
   - **Use case**: Only on trusted, encrypted drives

3. **CSV Spreadsheet (Not Secure)**
   - Comma-separated values format
   - Compatible with Excel, Google Sheets
   - File extension: `.csv`
   - **?? WARNING**: Secrets stored in plain text
   - **Use case**: Importing into other tools (use with caution)

#### Export Process:

1. **Select Format**: Choose Encrypted JSON, Plain JSON, or CSV
2. **Configure Encryption** (if Encrypted JSON):
   - Enter strong password (minimum 8 characters)
   - Confirm password
   - Password complexity indicators shown
   
3. **Choose Options**:
   - ?? Include metadata (type, network, timestamps)
   - ?? Include creation timestamps
   
4. **Security Warning**:
   - For plain text exports, a warning is displayed
   - Confirm you understand the security implications
   
5. **Save Location**: Choose where to save the export file
6. **Export Complete**: File is created and path is displayed

**Post-Export:**
- Store encrypted exports securely
- Never email or share unencrypted exports
- Delete plain text exports after use
- Document export location for recovery purposes

### Importing Secrets from File

Import previously exported secrets back into the service.

#### Import Process:

1. Click "**Import**" button in Secret Manager toolbar
2. The Import Secrets dialog opens

**Step 1: Select File**
- Click "**BROWSE**" to select a secrets file
- Supported formats:
  - Encrypted JSON (`.encrypted`, `.json`)
  - Plain Text JSON (`.json`, `.txt`)
  - CSV (`.csv`)
- File path is displayed after selection
- Preview shows file metadata and first few lines

**Step 2: Choose Format**
- **Auto-detect**: Automatically determines file format
- **Encrypted JSON**: Requires password
- **Plain Text**: No password needed
- **CSV**: Column mapping is automatic

**Step 3: Enter Password** (if encrypted)
- Password panel appears for encrypted files
- Enter the decryption password
- Incorrect password will show error message

**Step 4: Configure Import Options**
- **Overwrite existing secrets**: Replace secrets with same name
- **Validate before importing**: Check for errors before importing
- **Create backup first**: Create backup shares of existing secrets

**Step 5: Preview**
- File information displayed:
  - Filename, size, last modified
  - Number of secrets detected
  - Sample of secrets (names only, not values)
  
**Step 6: Import**
- Click "**IMPORT SECRETS**" to begin import
- Progress indicator shows import status
- Success summary displays:
  - Total secrets in file
  - Successfully imported
  - Skipped (duplicates without overwrite)
  - Failed (errors)

**Post-Import:**
- Secret Manager refreshes automatically
- Imported secrets appear in the list
- Search for imported secrets to verify
- Check audit log for import activity

**Import Validation:**
- Duplicate names: Prompts for overwrite confirmation
- Invalid format: Shows detailed error message
- Missing required fields: Skips invalid entries
- Metadata mismatch: Warns but continues import

---

## Audit Log Viewer

The Audit Log Viewer provides a comprehensive interface for viewing, searching, and exporting all administrative actions and security events.

### Opening the Audit Log Viewer

**From Main Window:**
- Click "**View Logs**" button in the header toolbar
- **Keyboard Shortcut**: `Ctrl+L`

### Audit Log Interface

The Audit Log Viewer displays a searchable, filterable grid of all logged events:

#### Main Components

1. **Filter Panel** (Top)
   - **Search Box**: Filter logs by text search
   - **Event Type Filter**: Filter by operation type
     - All Events
     - Sign Operations
     - Share Creation
     - Recovery Events
     - Auth Failures
     - Security Events
   - **Date Range**: Filter by time period
     - Today
  - Last 7 Days
     - Last 30 Days
     - All Time
   - **Apply Filter Button**: Refresh view with current filters

2. **Summary Bar**
   - Displays: "Showing X of Y log entries"
   - Updates in real-time as filters change

3. **Log Grid** (Center)
   - Columns:
  - **Timestamp**: Date and time of event
     - **Level**: Information, Warning, Error
     - **Event Type**: Category of operation
     - **Details**: Description of action
   - **Sorting**: Click column headers to sort
   - **Double-click**: View full details in popup

4. **Action Buttons** (Top-right)
   - **Open Log Folder**: Opens log directory in File Explorer
   - **Export**: Export filtered logs to file
   - **Refresh**: Reload logs from disk

5. **Footer Bar**
   - Shows log file location path
   - **Close** button to exit viewer

### Using the Audit Log Viewer

#### Searching Logs
1. Enter text in search box (searches all columns)
2. Results filter in real-time as you type
3. Case-insensitive matching
4. Searches: event type, details, and raw message

#### Filtering by Event Type
1. Click Event Type dropdown
2. Select category:
   - **Sign Operations**: Token signing events
   - **Share Creation**: Backup share generation
   - **Recovery Events**: Secret recovery from shares
   - **Auth Failures**: Failed authentication attempts
   - **Security Events**: Access control events
3. Grid updates to show only selected type

#### Filtering by Date Range
1. Click Date Range dropdown
2. Select time period (Today, Last 7 Days, etc.)
3. Grid shows events within selected range

#### Viewing Full Details
1. Double-click any log entry
2. Popup shows:
   - Full timestamp
   - Log level
   - Event type
   - Complete message details
3. Click OK to close

#### Exporting Logs
1. Apply desired filters (date, type, search)
2. Click "**Export**" button
3. Choose export format:
   - **CSV**: Spreadsheet format
   - **JSON**: Structured data format
   - **TXT**: Plain text report
4. Select save location
5. Confirmation message shows export path

#### Opening Log Folder
1. Click "**Open Log Folder**" button
2. Windows Explorer opens to: `C:\ProgramData\StampService\Logs\`
3. View all log files directly

### Log File Details

#### Log Location
- Path: `C:\ProgramData\StampService\Logs\`
- Files: `audit.log`, `service.log`, `stamp-service.log`
- Format: Serilog structured logging

#### Log Rotation
- Logs rotate daily
- Archives: `audit-20250104.log`
- Retention: 30 days by default
- Max size: 10 MB per file

#### What Gets Logged

**Security Events:**
- Secret access (create, read, delete)
- Authentication attempts
- Permission changes
- Configuration changes

**Operational Events:**
- Service start/stop
- Token creation/import
- Backup/recovery operations
- Health checks

**Error Events:**
- Failed operations
- Service errors
- Connection issues
- Validation failures

### Security and Compliance

The audit log provides:
- **Non-repudiation**: Timestamps and user tracking
- **Forensic trail**: Complete operation history
- **Compliance**: Meets regulatory logging requirements
- **Tamper detection**: Logs are append-only

**Best Practices:**
- Review logs weekly for suspicious activity
- Export logs monthly for off-system retention
- Monitor for failed authentication attempts
- Track secret access patterns
- Archive logs for compliance requirements (if applicable)

---

## Settings & Configuration

The Settings window allows you to customize the AdminGUI behavior and appearance.

**?? Note**: Settings functionality is currently basic. Most settings are saved but not all features are fully implemented yet.

### Opening Settings

- Click the **Settings** button (gear icon) in the main window header
- **Keyboard Shortcut**: `Ctrl+,`

### Available Settings

#### General Settings

**Application Behavior:**
- **Auto-Refresh**: Automatically update status displays
  - Toggle: Enable/Disable
  - Refresh interval: 15-300 seconds (slider)
  - Default: Every 30 seconds

**Confirmations:**
- **Confirm before deleting secrets**: Show warning dialog
  - Recommended: ?? Enabled
  - Prevents accidental deletion

**Notifications:**
- **Show operation completion notifications**
  - Toast notifications for import, export, backup operations

#### Appearance Settings

**Theme:**
- Light Theme (Current default)
- Dark Theme (Selection saves but theme doesn't change yet)
- System Theme (Follows Windows theme - not yet implemented)

**?? Known Limitation**: Theme selection saves to settings but doesn't actually change the application theme yet. This feature is planned for a future update.

#### Security Settings (Planned)

The following settings are shown in the UI but not yet fully functional:

- Auto-hide secrets after X seconds
- Require re-authentication before viewing secrets
- Auto-clear clipboard after copying
- Warn when copying secrets to clipboard

These will be implemented in future versions.

### Saving Settings

1. Adjust settings as desired
2. Click "**Save**" to apply and close
3. Click "**Cancel**" to discard changes
4. Click "**Reset to Defaults**" to restore original settings

**Settings Persistence:**
- Settings are saved to: `%APPDATA%\StampService\settings.json`
- Loaded automatically on application startup
- Backed up with each save

### Current Limitations

The following settings features are planned but not yet implemented:

- ? **Theme Switching**: Selection saves but doesn't apply visually
- ? **Default Backup Settings**: Not connected to backup wizard yet
- ? **Advanced Security Options**: Clipboard security, auto-hide, etc.
- ? **Network Configuration**: Custom networks cannot be added yet
- ? **Audit Log Settings**: Export audit log, log filtering

These features are on the roadmap and will be added in upcoming releases.

---

## Known Limitations

This section documents current limitations and features planned for future releases.

### Feature Status

**? Fully Implemented:**
- Main Dashboard and status display
- Create Token wizard (4 steps, all functional)
- Import Mnemonic wizard (3 steps, all functional)
- Secret Manager (view, search, delete, add)
- Secret Details (view, reveal, copy)
- Export Secrets (Encrypted JSON, Plain JSON, CSV)
- Import Secrets (from file, with validation)
- Backup & Recovery (real Shamir Secret Sharing implementation)
- Service Health Monitoring
- Audit Log Viewer (comprehensive logging and export)
- Keyboard shortcuts
- Add Secret dialog

**?? Partially Implemented:**
- Settings Window (saves settings but limited feature application)
- Theme selection (saves preference but doesn't change UI yet)

**? Planned for Future Releases:**
- Dynamic theme switching (Light/Dark/System)
- Clipboard auto-clear with timer
- Advanced security settings (auto-hide secrets, re-authentication)
- Custom network configuration
- Enhanced help system (context-sensitive, searchable)
- Application custom icon

### Workarounds for Limited Features

**Theme Preference:**
- Current: Application uses light theme by default
- Workaround: Settings save your preference for when feature is implemented
- Planned: v1.1 release

**Clipboard Security:**
- Current: Clipboard doesn't auto-clear
- Workaround: Manually clear clipboard after copying secrets
- Best Practice: Copy > Use > Manually paste something else to clear

**Custom Networks:**
- Current: Predefined networks only (Ethereum, Solana, etc.)
- Workaround: Use "Other" network type and document in metadata
- Planned: Network configuration dialog in v1.2

### Known Issues

**None currently reported.**

If you encounter issues, please report them via GitHub Issues with:
- Steps to reproduce
- Expected vs actual behavior
- Screenshots if applicable
- Log files from Audit Log Viewer

---

## Troubleshooting

Common issues and their solutions.

### Application Won't Start


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

#### 4. **Export/Import Safety**

**When Exporting:**
- ? Use Encrypted JSON format with strong passwords
- ? Never export to cloud storage in plain text
- ? Delete plain text exports immediately after use
- ? Document export locations securely
- ? Test encrypted exports can be decrypted

**When Importing:**
- ? Verify source file is trusted
- ? Use "Validate before importing" option
- ? Check for duplicate names before overwrite
- ? Create backup before bulk imports
- ? Review import summary for errors

#### 5. **Audit Log Monitoring**

- ? Review audit logs weekly for suspicious activity
- ? Export logs monthly for off-system retention
- ? Monitor for repeated failed operations
- ? Track secret access patterns
- ? Archive logs for compliance (if required)

### Operational Best Practices

#### 1. Regular Maintenance

**Weekly:**
- Review recent activity log (Dashboard and Audit Logs)
- Verify service status
- Check disk space
- Review failed operations in audit log

**Monthly:**
- Test backup recovery
- Audit secret list (remove obsolete)
- Export audit logs for retention
- Check for application updates

**Quarterly:**
- Refresh all backup shares
- Verify share storage locations
- Test emergency recovery procedures
- Review and update documentation

#### 2. Token Creation

- ? Use naming convention (e.g., `ENV_NETWORK_PURPOSE`)
- ? Add comprehensive metadata
- ? Document token purpose and usage
- ? Create backups immediately
- ? Test token before production use

#### 2a. **Mnemonic Import**

- ? Only import on trusted, secure computers
- ? Verify derived address matches expected
- ? Use descriptive names (e.g., `Personal_ETH_Master`)
- ? Create backup shares immediately after import
- ? Test with small transaction before trusting
- ? Keep original mnemonic backup separately
- ? Never share or screenshot mnemonics
- ? Document import in secure external records

#### 2b. **Secret Import from Files**

**Planning:**
- ? Review file format before import
- ? Check for naming conflicts
- ? Create backup before bulk import
- ? Test with small subset first

**Execution:**
- ? Use encrypted format when transferring between systems
- ? Validate before importing (enable option)
- ? Review import summary for errors
- ? Verify imported secrets in Secret Manager
- ? Delete import file after successful import

**Post-Import:**
- ? Create backup shares of critical imported secrets
- ? Update external documentation
- ? Test imported secrets work correctly
- ? Archive original import file securely (if needed)

#### 3. **Secret Organization**

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

#### 4. **Documentation**

Maintain external documentation:
- Token inventory spreadsheet
- Backup share location matrix
- Recovery contact list
- Network and address registry
- Change log
- **Export/Import tracking**: Log file transfers between systems
- **Audit log archives**: Off-system log storage locations

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
| Logs | `C:\ProgramData\StampService\Logs\` |
| Audit Logs | `C:\ProgramData\StampService\Logs\audit.log` |
| Settings | `%APPDATA%\StampService\settings.json` |
| Registry | `HKLM\SOFTWARE\StampService\Secrets` |

### C. Support Resources

- **Documentation**: `https://github.com/commentors-net/stamp-service`
- **Issue Tracker**: `https://github.com/commentors-net/stamp-service/issues`
- **Wiki**: `https://github.com/commentors-net/stamp-service/wiki`

### D. Glossary

- **BIP39**: Bitcoin Improvement Proposal 39, standard for mnemonic phrases
- **IPC**: Inter-Process Communication, method for processes to communicate
- **Mnemonic**: 12-word phrase for recovering private keys
- **Private Key**: Secret cryptographic key for signing transactions
- **Public Address**: Blockchain address derived from private key
- **Shamir's Secret Sharing**: Cryptographic method to split secrets
- **Threshold**: Minimum shares needed to recover original secret
- **Audit Log**: Tamper-evident record of all administrative actions
- **CSV**: Comma-Separated Values, spreadsheet data format
- **Export**: Save secrets to external file
- **Import**: Load secrets from external file

### E. Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-01-04 | Initial release with all core features |
|  |  | - Token Creation wizard |
|  |  | - Mnemonic Import wizard |
|  |  | - Secret Manager with Add/Export/Import |
|  |  | - Real Shamir Secret Sharing (Backup/Recovery) |
|  |  | - Audit Log Viewer |
|  |  | - Service Health monitoring |
|  |  | - Basic Settings (saves preferences) |

---

## Feature Roadmap

### Current Version (1.0.0)
All core features implemented and functional.

### Planned (v1.1 - Q1 2025)
- ? Dynamic theme switching (Light/Dark/System)
- ? Enhanced settings persistence and application
- ? Clipboard auto-clear security
- ? Advanced help system (searchable, context-sensitive)

### Future Versions (v1.2+)
- ? Custom network configuration
- ? Batch operations (bulk delete, bulk export)
- ? Secret templates and presets
- ? Multi-user support with role-based access
- ? Integration with hardware security modules (HSM)

---

**End of User Manual**
