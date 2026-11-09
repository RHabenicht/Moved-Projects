; NSRCG II — Windows x64 per-user installer (Inno Setup 6.3+)
; Run from the repository root after publish-win-x64.ps1.
; This project is an early functional rewrite; installation does not imply full SR3 rules completeness.

#define MyAppName "NSRCG II"
#define MyAppVersion "0.1.0"
#define MyAppExeName "NSRCG-II.exe"

[Setup]
AppId={{D3F19B60-486B-42D3-9CDA-A9BB5A9B4A47}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=NightLife
AppVerName={#MyAppName} {#MyAppVersion}
DefaultDirName={localappdata}\Programs\NSRCG II
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\installer-output
OutputBaseFilename=NSRCG-II-Setup-{#MyAppVersion}-win-x64
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\NSRCG II"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\NSRCG II"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch NSRCG II"; Flags: nowait postinstall skipifsilent
