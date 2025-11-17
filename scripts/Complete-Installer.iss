; Unified Inno Setup Script for Complete Stamp Service Distribution
; Installs: Windows Service + AdminCLI + AdminGUI in one installer

#define MyAppName "Stamp Service"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Your Organization"
#define MyAppURL "https://github.com/commentors-net/stamp-service"
#define ServiceExeName "StampService.exe"
#define AdminCLIExeName "StampService.AdminCLI.exe"
#define AdminGUIExeName "StampService.AdminGUI.exe"

[Setup]
; Unique App ID
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\StampService
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\
OutputBaseFilename=StampService-Complete-Setup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
DisableProgramGroupPage=no
UninstallDisplayIcon={app}\AdminGUI\{#AdminGUIExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Types]
Name: "full"; Description: "Full installation (Service + AdminCLI + AdminGUI)"
Name: "server"; Description: "Server installation (Service + AdminCLI only)"
Name: "custom"; Description: "Custom installation"; Flags: iscustom

[Components]
Name: "service"; Description: "Stamp Service (Windows Service)"; Types: full server custom; Flags: fixed
Name: "admincli"; Description: "AdminCLI (Command-line administration tool)"; Types: full server custom; Flags: fixed
Name: "admingui"; Description: "AdminGUI (Desktop administration tool)"; Types: full custom

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon} (AdminGUI)"; GroupDescription: "{cm:AdditionalIcons}"; Components: admingui
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon} (AdminGUI)"; GroupDescription: "{cm:AdditionalIcons}"; Components: admingui

[Files]
; Windows Service
Source: "..\dist\StampService\*"; DestDir: "{app}\Service"; Components: service; Flags: ignoreversion recursesubdirs createallsubdirs

; AdminCLI
Source: "..\dist\AdminCLI\*"; DestDir: "{app}\AdminCLI"; Components: admincli; Flags: ignoreversion recursesubdirs createallsubdirs

; AdminGUI
Source: "..\dist\AdminGUI\*"; DestDir: "{app}\AdminGUI"; Components: admingui; Flags: ignoreversion recursesubdirs createallsubdirs

; Scripts
Source: "..\dist\Scripts\*"; DestDir: "{app}\Scripts"; Flags: ignoreversion recursesubdirs createallsubdirs

; Documentation
Source: "..\dist\Documentation\*"; DestDir: "{app}\Documentation"; Flags: ignoreversion recursesubdirs createallsubdirs

; Version info
Source: "..\dist\version.json"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Start Menu shortcuts - Service
Name: "{group}\Service\Service Status"; Filename: "{app}\AdminCLI\{#AdminCLIExeName}"; Parameters: "status"; WorkingDir: "{app}\AdminCLI"; Comment: "Check service status"
Name: "{group}\Service\Test Stamp"; Filename: "{app}\AdminCLI\{#AdminCLIExeName}"; Parameters: "test-stamp"; WorkingDir: "{app}\AdminCLI"; Comment: "Test service"
Name: "{group}\Service\View Logs"; Filename: "{commonappdata}\StampService\Logs"; Comment: "View service logs"

; Start Menu shortcuts - AdminGUI
Name: "{group}\Admin GUI"; Filename: "{app}\AdminGUI\{#AdminGUIExeName}"; WorkingDir: "{app}\AdminGUI"; Components: admingui; Comment: "Stamp Service Admin GUI"
Name: "{group}\Documentation"; Filename: "{app}\Documentation"; Comment: "View documentation"

; Start Menu shortcuts - Uninstaller
Name: "{group}\Complete Uninstaller"; Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\Scripts\Complete-Uninstaller.ps1"""; WorkingDir: "{app}\Scripts"; Comment: "Remove all components"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

; Desktop shortcut
Name: "{autodesktop}\{#MyAppName} Admin"; Filename: "{app}\AdminGUI\{#AdminGUIExeName}"; Tasks: desktopicon; WorkingDir: "{app}\AdminGUI"; Components: admingui; Comment: "Stamp Service Administration"

; Quick Launch shortcut
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName} Admin"; Filename: "{app}\AdminGUI\{#AdminGUIExeName}"; Tasks: quicklaunchicon; WorkingDir: "{app}\AdminGUI"; Components: admingui

[Run]
; Install the Windows Service
Filename: "sc.exe"; Parameters: "create SecureStampService binPath= ""{app}\Service\{#ServiceExeName}"" start= auto"; StatusMsg: "Installing Windows Service..."; Flags: runhidden; Components: service
Filename: "sc.exe"; Parameters: "description SecureStampService ""Secure cryptographic signing service with HSM-like functionality"""; Flags: runhidden; Components: service
Filename: "sc.exe"; Parameters: "start SecureStampService"; StatusMsg: "Starting service..."; Flags: runhidden; Components: service

; Option to run AdminGUI after installation
Filename: "{app}\AdminGUI\{#AdminGUIExeName}"; Description: "{cm:LaunchProgram,Admin GUI}"; Flags: nowait postinstall skipifsilent runascurrentuser; Components: admingui

[UninstallRun]
; Stop and remove the Windows Service
Filename: "sc.exe"; Parameters: "stop SecureStampService"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete SecureStampService"; Flags: runhidden

[UninstallDelete]
; Clean up generated files
Type: filesandordirs; Name: "{app}\Documentation"
Type: filesandordirs; Name: "{app}\Scripts"

[Code]
var
  DataDirPage: TInputDirWizardPage;
  PreserveDataCheckbox: TNewCheckBox;

// Create custom page for data directory
procedure InitializeWizard;
begin
  DataDirPage := CreateInputDirPage(wpSelectDir,
    'Select Data Directory',
    'Where should service data be stored?',
    'The service will store logs, keys, and configuration in this directory.' + #13#10 + #13#10 +
    'Default location: C:\ProgramData\StampService',
    False, 'StampService');
  DataDirPage.Add('');
  DataDirPage.Values[0] := ExpandConstant('{commonappdata}\StampService');
  
  // Add checkbox for preserving data on uninstall
  PreserveDataCheckbox := TNewCheckBox.Create(DataDirPage);
  PreserveDataCheckbox.Parent := DataDirPage.Surface;
  PreserveDataCheckbox.Caption := 'Preserve data directory during uninstall (recommended)';
  PreserveDataCheckbox.Checked := True;
  PreserveDataCheckbox.Top := DataDirPage.Edits[0].Top + DataDirPage.Edits[0].Height + ScaleY(20);
  PreserveDataCheckbox.Width := DataDirPage.SurfaceWidth;
end;

// Check for admin privileges
function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsAdmin then
  begin
    MsgBox('This installer requires Administrator privileges.' + #13#10 + #13#10 +
  'Please run the installer as Administrator.',
      mbError, MB_OK);
    Result := False;
  end;
end;

// Create data directory and set permissions
procedure CurStepChanged(CurStep: TSetupStep);
var
  DataPath: String;
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    DataPath := DataDirPage.Values[0];
    
    // Create data directory
    if not DirExists(DataPath) then
      CreateDir(DataPath);
    
    // Create Logs subdirectory
    if not DirExists(DataPath + '\Logs') then
      CreateDir(DataPath + '\Logs');
    
    // Set permissions (grant SYSTEM and Administrators full control)
    Exec('icacls.exe', '"' + DataPath + '" /grant *S-1-5-18:(OI)(CI)F /T', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec('icacls.exe', '"' + DataPath + '" /grant *S-1-5-32-544:(OI)(CI)F /T', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    
 // Update service configuration with data path
// This would normally update appsettings.json, but we'll keep default paths
  end;
end;

// Check if uninstall should preserve data
function ShouldPreserveData(): Boolean;
begin
  Result := PreserveDataCheckbox.Checked;
end;

[Messages]
; Custom messages
WelcomeLabel1=Welcome to the [name] Setup Wizard
WelcomeLabel2=This will install the complete Stamp Service on your computer.%n%nComponents to be installed:%n%n• Windows Service (cryptographic signing service)%n• AdminCLI (command-line administration tool)%n• AdminGUI (desktop administration interface)%n%nIt is recommended that you close all other applications before continuing.
FinishedHeadingLabel=Completing the [name] Setup Wizard
FinishedLabel=The Stamp Service has been installed on your computer.%n%nService installed as: SecureStampService%n%nYou can manage the service using:%n• AdminGUI from the Start Menu or Desktop%n• AdminCLI from the command line%n%nIMPORTANT: Create backup shares immediately after installation!
