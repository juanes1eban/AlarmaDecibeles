; Instalador de AlarmaDecibeles (Inno Setup 6).
; El workflow .github/workflows/release.yml lo compila con:
;   ISCC.exe /DAppVersion=1.0.2 /DPublishDir=publish installer.iss

; La versión no se repite aquí: viene del tag (o del <Version> del .csproj si se compila a mano).
#ifndef AppVersion
  #error Falta /DAppVersion=X.Y.Z
#endif
#ifndef PublishDir
  #define PublishDir "publish"
#endif

[Setup]
; No cambiar el AppId: así Windows reconoce las versiones nuevas como actualización y no como otro programa.
AppId={{653857D8-3F8C-419C-BE7C-94A5C84E3B61}
AppName=Alarma Decibeles
AppVersion={#AppVersion}
AppPublisher=Juan Esteban Valenzuela
AppPublisherURL=https://github.com/juanes1eban/AlarmaDecibeles
AppSupportURL=https://github.com/juanes1eban/AlarmaDecibeles/issues
DefaultDirName={autopf}\AlarmaDecibeles
DefaultGroupName=Alarma Decibeles
DisableProgramGroupPage=yes
; Instala por usuario sin pedir permisos de administrador (se puede cambiar a "todos los usuarios" en el asistente).
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=mic.ico
UninstallDisplayIcon={app}\AlarmaDecibeles.exe
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
OutputDir=Output
OutputBaseFilename=AlarmaDecibeles-Setup
; Cierra la app si está abierta al actualizar.
CloseApplications=yes

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Alarma Decibeles"; Filename: "{app}\AlarmaDecibeles.exe"
Name: "{autodesktop}\Alarma Decibeles"; Filename: "{app}\AlarmaDecibeles.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\AlarmaDecibeles.exe"; Description: "{cm:LaunchProgram,Alarma Decibeles}"; Flags: nowait postinstall skipifsilent
