; Script Inno Setup pour USB Device Trigger
; Version 1.0.0

#define MyAppName "USB Device Trigger"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "PatApps"
#define MyAppURL "https://github.com/patrick-oliveira-ch/UsbDeviceTrigger"
#define MyAppExeName "UsbDeviceTrigger.UI.exe"

[Setup]
; Informations de l'application
AppId={{8F3A2C5D-1B4E-4A7C-9D6F-E2B8C1A5F7D3}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Répertoires d'installation
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Sortie
OutputDir=..\output
OutputBaseFilename=UsbDeviceTriggerSetup-v{#MyAppVersion}
; SetupIconFile=..\src\UsbDeviceTrigger.UI\Resources\Icons\app-icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

; Compression
Compression=lzma2
SolidCompression=yes

; Privilèges
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; Interface
WizardStyle=modern
DisableWelcomePage=no

; Langue
ShowLanguageDialog=no

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Créer une icône sur le bureau"; GroupDescription: "Icônes additionnelles:"
Name: "startupicon"; Description: "Démarrer l'application avec Windows"; GroupDescription: "Démarrage automatique:"

[Files]
; Fichiers de l'application
Source: "..\src\UsbDeviceTrigger.UI\bin\Release\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Remplacer le chemin ci-dessus par le chemin réel après compilation

[Icons]
; Icône dans le menu démarrer
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Désinstaller {#MyAppName}"; Filename: "{uninstallexe}"

; Icône sur le bureau
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Démarrage automatique avec Windows (si l'utilisateur le choisit)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "UsbDeviceTrigger"; ValueData: """{app}\{#MyAppExeName}"" --minimized"; Flags: uninsdeletevalue; Tasks: startupicon

[Run]
; Lancer l'application après l'installation
Filename: "{app}\{#MyAppExeName}"; Description: "Lancer {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Supprimer les fichiers de configuration lors de la désinstallation (optionnel)
Type: filesandordirs; Name: "{userappdata}\UsbDeviceTrigger"

[Code]
// Vérification de .NET 8.0 Desktop Runtime
function IsDotNetInstalled(): Boolean;
var
  ResultCode: Integer;
  DotNetVersion: String;
begin
  // Essayer de détecter .NET 8.0 via le registre
  if RegQueryStringValue(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost', '8.0', DotNetVersion) then
  begin
    Result := True;
  end
  else if RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedhost', '8.0', DotNetVersion) then
  begin
    Result := True;
  end
  else
  begin
    Result := False;
  end;
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
  DotNetURL: String;
begin
  Result := True;

  if not IsDotNetInstalled() then
  begin
    if MsgBox('.NET 8.0 Desktop Runtime est requis pour exécuter cette application.' + #13#10 + #13#10 +
              'Voulez-vous télécharger et installer .NET 8.0 Desktop Runtime maintenant?' + #13#10 + #13#10 +
              'Note: L''installation nécessitera une connexion Internet et des privilèges administrateur.',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      DotNetURL := 'https://dotnet.microsoft.com/download/dotnet/8.0/runtime';
      ShellExec('open', DotNetURL, '', '', SW_SHOW, ewNoWait, ErrorCode);
      Result := False;
      MsgBox('Veuillez installer .NET 8.0 Desktop Runtime, puis relancer cet installateur.', mbInformation, MB_OK);
    end
    else
    begin
      Result := False;
      MsgBox('L''installation ne peut pas continuer sans .NET 8.0 Desktop Runtime.', mbError, MB_OK);
    end;
  end;
end;

// Message de bienvenue personnalisé
procedure InitializeWizard();
begin
  WizardForm.WelcomeLabel2.Caption :=
    'Cet assistant vous guidera dans l''installation de ' + '{#MyAppName}' + '.' + #13#10 + #13#10 +
    '{#MyAppName} vous permet d''exécuter automatiquement des commandes Windows ' +
    'lorsque des périphériques USB sont connectés ou déconnectés.' + #13#10 + #13#10 +
    'Cliquez sur Suivant pour continuer.';
end;

// Message après l'installation
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Actions post-installation si nécessaire
  end;
end;
