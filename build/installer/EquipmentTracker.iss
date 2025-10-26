; Equipment Tracker Inno Setup Script
#define AppName "Equipment Tracker"
#define AppVersion GetEnv("GITHUB_REF_NAME")
#ifndef AppVersion
  #define AppVersion "v1.0.4"
#endif
#define AppPublisher "kalantariamin1369-ux"
#define AppExeName "EquipmentTracker.exe"

[Setup]
AppId={{B3B6A2A0-9F4E-4C58-9D34-1A9A7E1F7E31}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\EquipmentTracker
DisableDirPage=no
DefaultGroupName=Equipment Tracker
OutputDir=dist
OutputBaseFilename=EquipmentTracker-Setup-x64-{#AppVersion}
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
CloseApplications=force
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; Use absolute path resolved by Actions before ISCC call via /D switch
Source: "{#BuildPayload}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\Equipment Tracker"; Filename: "{app}\{#AppExeName}"
Name: "{commondesktop}\Equipment Tracker"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch Equipment Tracker"; Flags: nowait postinstall skipifsilent
