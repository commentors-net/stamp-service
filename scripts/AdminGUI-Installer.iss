; Inno Setup Script for Stamp Service Admin GUI
; Creates a professional installer for the AdminGUI application

#define MyAppName "Stamp Service Admin GUI"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Your Organization"
#define MyAppURL "https://github.com/commentors-net/stamp-service"
#define MyAppExeName "StampService.AdminGUI.exe"

[Setup]
; Unique App ID
AppId={{B2F8E4A3-9D1C-4E7B-8F2A-5C3D9E1A6F4B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\StampService\AdminGUI
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\LICENSE.txt
OutputDir=..\
OutputBaseFilename=StampService-AdminGUI-Setup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
DisableProgramGroupPage=no
UninstallDisplayIcon={app}\{#MyAppExeName}
; Removed non-existent image files - using defaults
; SetupIconFile=..\src\StampService.AdminGUI\Resources\icon.ico
; WizardImageFile=compiler:WizModernImage-IS.bmp
; WizardSmallImageFile=compiler:WizModernSmallImage-IS.bmp

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Main AdminGUI executable and dependencies
Source: "..\src\StampService.AdminGUI\bin\Release\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Scripts
Source: "..\scripts\Complete-Uninstaller.ps1"; DestDir: "{app}\Scripts"; Flags: ignoreversion
; Documentation
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Resources\QUICKSTART.md"; DestDir: "{app}\Docs"; Flags: ignoreversion
Source: "..\Resources\ADMINGUI-COMPLETE-SUMMARY.md"; DestDir: "{app}\Docs"; Flags: ignoreversion
Source: "..\Resources\POLISH-QUICK-REF.md"; DestDir: "{app}\Docs"; Flags: ignoreversion

[Icons]
; Start Menu shortcuts
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; Comment: "Manage Stamp Service"
Name: "{group}\Documentation"; Filename: "{app}\Docs"; Comment: "View documentation"
Name: "{group}\Logs Folder"; Filename: "{commonappdata}\StampService\Logs"; Comment: "View service logs"
Name: "{group}\Complete Uninstaller"; Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\Scripts\Complete-Uninstaller.ps1"""; WorkingDir: "{app}\Scripts"; IconIndex: 0; Comment: "Remove all Stamp Service components"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

; Desktop shortcut
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; Comment: "Manage Stamp Service"

; Quick Launch shortcut
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon; WorkingDir: "{app}"

[Run]
; Option to run the application after installation
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser

[UninstallDelete]
; Clean up any user-generated files
Type: filesandordirs; Name: "{app}\Docs"

[Code]
// Check if the StampService is installed
function IsStampServiceInstalled: Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec('sc.exe', 'query "SecureStampService"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

// Display a message if the service is not installed
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if not IsStampServiceInstalled then
    begin
      MsgBox('Note: The Stamp Service does not appear to be installed.' + #13#10 + #13#10 +
     'The Admin GUI requires the Stamp Service to be installed and running.' + #13#10 + #13#10 +
     'Please install the Stamp Service before using the Admin GUI.' + #13#10 + #13#10 +
     'You can still use this application to connect to a remote service.',
      mbInformation, MB_OK);
    end;
  end;
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

// Show information after installation
procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpFinished then
  begin
    // Additional post-install messages can be added here
  end;
end;

[Messages]
; Custom messages
WelcomeLabel1=Welcome to the [name] Setup Wizard
WelcomeLabel2=This will install the Stamp Service Admin GUI on your computer.%n%nThe Admin GUI provides a professional interface for managing the Stamp Service, including:%n%n• Service health monitoring%n• Secret management%n• Backup and recovery operations%n• Token creation wizards%n%nIt is recommended that you close all other applications before continuing.
FinishedHeadingLabel=Completing the [name] Setup Wizard
FinishedLabel=The Admin GUI has been installed on your computer.%n%nYou can launch the application from the Start Menu or Desktop shortcut.%n%nNote: The Admin GUI requires Administrator privileges to function correctly.
