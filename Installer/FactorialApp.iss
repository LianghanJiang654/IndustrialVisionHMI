#define MyAppName "Industrial Machine Vision HMI"
#define MyAppVersion "1.0.0"
#define MyAppExeName "FactorialApp.exe"
[Setup]
AppId={{A9BBCE9D-827B-4F7D-A2C1-98AA07FBF33B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\IndustrialVisionHMI
DefaultGroupName={#MyAppName}
OutputDir=output
OutputBaseFilename=IndustrialVisionHMI_Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
