; Inno Setup Script for Secure Stamp Service
; This creates a single-file installer for non-technical users

#define MyAppName "Secure Stamp Service"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Your Organization"
#define MyAppURL "https://github.com/commentors-net/stamp-service"
#define MyAppExeName "StampService.exe"
#define MyServiceName "SecureStampService"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
AppId={{8D5F9A2B-1C4E-4F6A-9B3D-7E2C8A1F5D4B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={commonpf}\StampService
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\LICENSE.txt
InfoBeforeFile=..\Resources\INSTALLATION-INFO.txt
InfoAfterFile=..\Resources\POST-INSTALL-INFO.txt
OutputDir=..\
OutputBaseFilename=StampService-Setup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
WizardStyle=modern
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\StampService.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "createshares"; Description: "Create backup shares after installation (RECOMMENDED)"; GroupDescription: "Post-Installation:"; Flags: checkedonce

[Files]
; Service files
Source: "..\dist\StampService\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Admin CLI
Source: "..\dist\AdminCLI\*"; DestDir: "{app}\AdminCLI"; Flags: ignoreversion recursesubdirs createallsubdirs
; Scripts
Source: "..\dist\Scripts\*"; DestDir: "{app}\Scripts"; Flags: ignoreversion recursesubdirs createallsubdirs
; Documentation
Source: "..\dist\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\QUICKSTART.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\DISTRIBUTION-README.txt"; DestDir: "{app}"; Flags: ignoreversion isreadme
Source: "..\dist\version.json"; DestDir: "{app}"; Flags: ignoreversion

[Dirs]
Name: "{commonappdata}\StampService"; Permissions: users-modify
Name: "{commonappdata}\StampService\Logs"; Permissions: users-modify

[Icons]
Name: "{group}\Admin CLI"; Filename: "cmd.exe"; Parameters: "/k cd /d ""{app}\AdminCLI"""; WorkingDir: "{app}\AdminCLI"
Name: "{group}\Service Logs"; Filename: "{commonappdata}\StampService\Logs"
Name: "{group}\Documentation"; Filename: "{app}\README.md"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"

[Run]
; Stop service if it exists (for upgrades)
Filename: "sc.exe"; Parameters: "stop {#MyServiceName}"; Flags: runhidden; StatusMsg: "Stopping existing service..."; Check: ServiceExists
; Install service
Filename: "sc.exe"; Parameters: "create {#MyServiceName} binPath= ""{app}\{#MyAppExeName}"" start= auto DisplayName= ""Secure Stamp Service"""; Flags: runhidden; StatusMsg: "Installing service..."
Filename: "sc.exe"; Parameters: "description {#MyServiceName} ""Cryptographic signing service using Ed25519 with Shamir Secret Sharing backup"""; Flags: runhidden
; Start service
Filename: "sc.exe"; Parameters: "start {#MyServiceName}"; Flags: runhidden; StatusMsg: "Starting service..."
; Open Admin CLI for share creation
Filename: "cmd.exe"; Parameters: "/k cd /d ""{app}\AdminCLI"" && echo. && echo ===== IMPORTANT: CREATE BACKUP SHARES ===== && echo. && echo Run the following command to create backup shares: && echo. && echo StampService.AdminCLI.exe create-shares --total 5 --threshold 3 --output C:\Shares && echo. && echo Then distribute the shares to trusted custodians! && echo."; Description: "Open Admin CLI to create backup shares"; Flags: postinstall nowait shellexec skipifsilent; Check: IsTaskSelected('createshares')
; View README
Filename: "{app}\DISTRIBUTION-README.txt"; Description: "View installation guide"; Flags: postinstall shellexec skipifsilent unchecked

[UninstallRun]
; Stop and remove service
Filename: "sc.exe"; Parameters: "stop {#MyServiceName}"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete {#MyServiceName}"; Flags: runhidden

[UninstallDelete]
Type: filesandordirs; Name: "{commonappdata}\StampService"

[Code]
function ServiceExists: Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec('sc.exe', 'query {#MyServiceName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

function IsTaskSelected(TaskName: String): Boolean;
begin
  Result := WizardIsTaskSelected(TaskName);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    // Verify service installation
    if not ServiceExists then
    begin
      MsgBox('Warning: Service installation may have failed. Please check Event Viewer.', mbError, MB_OK);
    end;
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsAdmin then
  begin
    MsgBox('This installer requires Administrator privileges.', mbError, MB_OK);
    Result := False;
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectTasks then
  begin
    WizardForm.TasksList.ItemCaption[0] := 'Create backup shares after installation (CRITICAL - Required for key recovery)';
  end;
end;
