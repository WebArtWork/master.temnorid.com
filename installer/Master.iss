; Inno Setup script for the Windows installer: Temnorid-Master-Setup.exe
; Build the player first (Unity: Temnorid > Master > Build Windows Player), then compile this with Inno Setup:
;   "ISCC.exe" installer\Master.iss
; The installer is written to Builds\Installer.

#define AppName "Temnorid Master"
#define AppVersion "0.2.0"
#define AppExe "TemnoridMaster.exe"

[Setup]
AppId={{A41C9E07-52D8-4B63-8F1A-7E3D6C2B9051}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Temnorid
AppPublisherURL=https://master.temnorid.com
DefaultDirName={autopf}\Temnorid Master
DefaultGroupName=Temnorid Master
DisableProgramGroupPage=yes
OutputDir=..\Builds\Installer
OutputBaseFilename=Temnorid-Master-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayIcon={app}\{#AppExe}
WizardStyle=modern

[Files]
Source: "..\Builds\Windows\TemnoridMaster\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Run]
; No skipifsilent: when the game updates itself the installer runs silently and must start the game again.
Filename: "{app}\{#AppExe}"; Description: "Start {#AppName}"; Flags: nowait postinstall
