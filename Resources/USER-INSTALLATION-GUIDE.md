# Secure Stamp Service - Installation Guide for End Users

## What is This?

Secure Stamp Service is a Windows service that provides cryptographic signing for your applications. Think of it as a secure digital signature service that runs in the background on your computer.

## Installation (EASY - Just 5 Minutes!)

### What You Need

- ? Windows 10, Windows 11, or Windows Server 2019 or newer
- ? Administrator rights on your computer
- ? 5 trusted people to hold backup keys (friends, family, coworkers)

### Step 1: Download the Installer

Your IT administrator will provide you with a file named:
```
StampService-Setup-1.0.0.exe
```

Save this file to your Downloads folder or Desktop.

### Step 2: Run the Installer

1. **Find the installer file** you downloaded
2. **Double-click** on `StampService-Setup-1.0.0.exe`
3. **Windows may show a security warning**:
   - Click "More info"
   - Then click "Run anyway"
4. **Click "Yes"** when asked "Do you want to allow this app to make changes?"

### Step 3: Follow the Wizard

The installer will guide you through these screens:

1. **Welcome Screen** - Click "Next"
2. **Important Information** - Read it, then click "Next"
3. **License Agreement** - Select "I accept the agreement", click "Next"
4. **Installation Location** - Keep the default, click "Next"
5. **Create Backup Shares** - **KEEP THIS CHECKED!** This is critical! Click "Next"
6. **Ready to Install** - Click "Install"

The installation takes about 30 seconds.

### Step 4: Create Backup Shares (CRITICAL!)

After installation, a black command window will open with instructions.

**Type this command exactly** (or copy/paste):
```
StampService.AdminCLI.exe create-shares --total 5 --threshold 3 --output C:\Shares
```

Press Enter.

This creates 5 files in `C:\Shares\`:
- `share-1-of-5.json`
- `share-2-of-5.json`
- `share-3-of-5.json`
- `share-4-of-5.json`
- `share-5-of-5.json`

### Step 5: Distribute the Backup Shares

**This is VERY important!**

1. **Give each file to a different trusted person**:
   - Share 1 ? Alice
   - Share 2 ? Bob
   - Share 3 ? Carol
   - Share 4 ? Dan
   - Share 5 ? Eve

2. **How to give them the shares**:
   - Copy to USB drive and hand it to them
   - OR print the file contents on paper
   - OR save to encrypted email (less secure)

3. **Tell each person**:
   - "Keep this file safe"
   - "Don't share it with anyone"
   - "Store it offline (not in cloud)"
   - "If I lose my computer, I'll need 3 people to help me recover"

4. **Write down who has what**:
   ```
   Share 1: Alice (alice@example.com)
   Share 2: Bob (bob@example.com)
   Share 3: Carol (carol@example.com)
   Share 4: Dan (dan@example.com)
   Share 5: Eve (eve@example.com)
   ```

### Step 6: Verify Installation (Optional)

Close the command window and open a new one:

1. Press `Windows + X`
2. Select "Windows PowerShell" or "Terminal"
3. Type:
   ```
   Get-Service SecureStampService
   ```

You should see:
```
Status   Name  DisplayName
------   ----  -----------
Running  SecureStampService      Secure Stamp Service
```

If you see "Running", you're all set! ?

## What Happens Now?

- The service runs automatically in the background
- It starts when Windows starts
- Your applications can use it to sign data
- The backup shares protect you if something goes wrong

## Common Questions

### Why do I need 5 people?

The service uses a technology called "Shamir Secret Sharing". Your encryption key is split into 5 pieces. Any 3 pieces can recreate the key, but 1 or 2 pieces are useless.

This means:
- If 2 people lose their shares, you can still recover (using the other 3)
- If someone steals 1 share, they can't do anything with it
- You need 3 people to cooperate to recover the key

### What if I lose my computer?

If your computer dies or you need to move to a new one:

1. Install the service on the new computer
2. Contact 3 of your share holders
3. Run the recovery command with their shares
4. Your key is restored!

See the "Recovery Instructions" section below.

### Can I use more or fewer shares?

Yes! When creating shares, you can change the numbers:

```
# 7 shares, need any 4 to recover
StampService.AdminCLI.exe create-shares --total 7 --threshold 4 --output C:\Shares

# 3 shares, need all 3 to recover (less safe!)
StampService.AdminCLI.exe create-shares --total 3 --threshold 3 --output C:\Shares
```

**Recommended**: 5 shares, 3 needed (good balance of security and convenience)

### Where are the log files?

All activity is logged to:
```
C:\ProgramData\StampService\Logs\
```

You can open this folder in Windows Explorer.

### How do I uninstall?

1. Open Windows Settings
2. Go to "Apps"
3. Find "Secure Stamp Service"
4. Click "Uninstall"

OR

1. Press `Windows + X`
2. Select "Programs and Features"
3. Find "Secure Stamp Service"
4. Click "Uninstall"

**Note**: This keeps your backup shares safe. The shares are stored separately.

## Recovery Instructions

If you need to recover your key on a new computer:

### Step 1: Install on New Computer

Run the installer on the new computer (same as above).

### Step 2: Start Recovery

Open Command Prompt or PowerShell as Administrator:

```powershell
cd "C:\Program Files\StampService\AdminCLI"
.\StampService.AdminCLI.exe recover start --threshold 3
```

### Step 3: Add Shares

Contact 3 of your share holders and get their share files.

For each share file, run:
```powershell
.\StampService.AdminCLI.exe recover add-share C:\path\to\share-1-of-5.json
.\StampService.AdminCLI.exe recover add-share C:\path\to\share-3-of-5.json
.\StampService.AdminCLI.exe recover add-share C:\path\to\share-5-of-5.json
```

### Step 4: Verify Recovery

```powershell
.\StampService.AdminCLI.exe status
.\StampService.AdminCLI.exe test-stamp
```

If the test passes, your key is recovered! ?

The public key should match your original installation.

## Troubleshooting

### Installer won't run

**Problem**: "Windows protected your PC" message

**Solution**:
1. Click "More info"
2. Click "Run anyway"

If you still can't run it, your IT administrator may need to sign the installer with a certificate.

### Service won't start

**Problem**: Service shows "Stopped" instead of "Running"

**Solution**:
1. Open Windows Event Viewer
2. Go to Windows Logs ? Application
3. Look for errors from "SecureStampService"
4. Contact your IT administrator with the error message

### Can't create shares

**Problem**: Command gives an error

**Solution**:
1. Make sure the command window says "Administrator" in the title
2. Check that the service is running:
   ```
   Get-Service SecureStampService
   ```
3. Try a different output folder:
   ```
   StampService.AdminCLI.exe create-shares --total 5 --threshold 3 --output D:\MyShares
   ```

### Need more help?

Contact your IT administrator or system administrator. Provide them with:
- The error message you're seeing
- Contents of: `C:\ProgramData\StampService\Logs\service-YYYYMMDD.log`
- What step you were on when the error occurred

## Security Tips

### DO:
- ? Create backup shares immediately after installation
- ? Give shares to 5 different trusted people
- ? Store shares offline (USB drive, paper, safe)
- ? Keep a list of who has which share
- ? Test recovery process once to make sure it works

### DON'T:
- ? Give all shares to one person
- ? Store all shares together
- ? Email shares without encryption
- ? Store shares in cloud drives (Dropbox, Google Drive, etc.)
- ? Forget who has your shares

## Technical Support

For technical assistance:
- Check the full documentation: `C:\Program Files\StampService\README.md`
- View logs: `C:\ProgramData\StampService\Logs\`
- Contact your system administrator

---

**Remember**: The most important step is creating and distributing backup shares! Don't skip this step!

**Installation takes 5 minutes. Recovery without shares is impossible!**
