; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define AppVer GetVersionNumbersString('..\Shoko.Server\bin\Release\net8.0-windows\win-x64\ShokoServer.exe')
#define AppSlug Copy(StringChange(AppVer, ".", ""), 1, Len(AppVer) - 1)
#define MyAppExeName "ShokoServer.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{0BA2D22B-A0B7-48F8-8AA1-BAAEFC2034CB}
AppName=Shoko Server
AppVersion={#AppVer}
AppVerName=Shoko Server
AppPublisher=Shoko Team
AppPublisherURL=https://ShokoAnime.com/
AppSupportURL=https://github.com/ShokoAnime/
AppUpdatesURL=https://ShokoAnime.com/downloads/
DefaultDirName={commonpf}\Shoko\Shoko Server
DefaultGroupName=Shoko Server
AllowNoIcons=yes
OutputBaseFilename=Shoko.Setup
UninstallDisplayIcon={app}\{#MyAppExeName}
SolidCompression=yes
InternalCompressLevel=max
Compression=lzma2/ultra64
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "firewall"; Description: "Add Shoko Server to Windows Firewall Exception list"
Name: "StartMenuEntry"; Description: "Launch Shoko Server on startup"
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 0,6.1

[Icons]
Name: "{group}\Shoko Server"; Filename: "{app}\ShokoServer.exe"
Name: "{group}\{cm:UninstallProgram,Shoko Server}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\Shoko Server"; Filename: "{app}\ShokoServer.exe"; Tasks: desktopicon
Name: "{commonstartup}\Shoko Server"; Filename: "{app}\ShokoServer.exe"; Tasks: StartMenuEntry;

[Run]
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""Shoko Server - Client Port"" dir=in action=allow protocol=TCP localport=8111"; Flags: runhidden; StatusMsg: "Open exception on firewall..."; Tasks: Firewall
Filename: "{app}\ShokoServer.exe"; Flags: nowait postinstall skipifsilent shellexec; Description: "{cm:LaunchProgram,Shoko Server}"
Filename: "https://docs.shokoanime.com/server/install/"; Flags: shellexec runasoriginaluser postinstall; Description: "Shoko Server Install Guide"
Filename: "https://shokoanime.com/blog/shoko-version-{#AppSlug}-released/"; Flags: shellexec runasoriginaluser postinstall; Check: BlogPostCheck; Description: "View {#AppVer} Release Notes"

[UninstallRun]
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Shoko Server - Client Port"" protocol=TCP localport=8111"; Flags: runhidden; StatusMsg: "Closing exception on firewall..."; Tasks: Firewall

[Registry]
Root: HKLM; Subkey: "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"; ValueName: "ShokoServer"; ValueType: none; Flags: deletevalue;

[Dirs]
Name: "{app}"; Permissions: users-full
Name: "{commonappdata}\ShokoServer"; Permissions: users-full

[Files]
Source: ".\FixPermissions.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Shoko.Server\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Code]

{ ///////////////////////////////////////////////////////////////////// }
function BlogPostCheck(): Boolean;
var
  WinHttpReq: Variant;
begin
  WinHttpReq := CreateOleObject('WinHttp.WinHttpRequest.5.1');
  WinHttpReq.Open('GET', 'https://shokoanime.com/blog/shoko-version-{#AppSlug}-released/', False);
  WinHttpReq.Send('');
  if WinHttpReq.Status := 200 then Result := True;
  if WinHttpReq.Status <> 200 then Result := False;
end;

{ ///////////////////////////////////////////////////////////////////// }
function GetUninstallString(): String;
var
  sUnInstPath: String;
  sUnInstallString: String;
begin
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;


{ ///////////////////////////////////////////////////////////////////// }
function IsUpgrade(): Boolean;
begin
  Result := (GetUninstallString() <> '');
end;


{ ///////////////////////////////////////////////////////////////////// }
function UnInstallOldVersion(): Integer;
var
  sUnInstallString: String;
  iResultCode: Integer;
begin
{ Return Values: }
{ 1 - uninstall string is empty }
{ 2 - error executing the UnInstallString }
{ 3 - successfully executed the UnInstallString }

  { default return value }
  Result := 0;

  { get the uninstall string of the old app }
  sUnInstallString := GetUninstallString();
  if sUnInstallString <> '' then begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if Exec(sUnInstallString, '/VERYSILENT /NORESTART /SUPPRESSMSGBOXES','', SW_HIDE, ewWaitUntilTerminated, iResultCode) then
      Result := 3
    else
      Result := 2;
  end else
    Result := 1;
end;

{ ///////////////////////////////////////////////////////////////////// }
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep=ssInstall) then
  begin
    if (IsUpgrade()) then
    begin
      UnInstallOldVersion();
    end;
  end;
end;
