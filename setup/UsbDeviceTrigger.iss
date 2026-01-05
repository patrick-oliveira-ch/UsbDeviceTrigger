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
const
  DotNetRuntimeURL = 'https://download.visualstudio.microsoft.com/download/pr/9d6b6b34-44b5-4cf4-b924-79a00deb9795/2f17c30bdf42b6a8950a8552438cf8c1/windowsdesktop-runtime-8.0.11-win-x64.exe';
  DotNetRuntimeFile = 'windowsdesktop-runtime-8.0.11-win-x64.exe';

// Vérification de .NET 8.0 Desktop Runtime via dotnet.exe
function IsDotNetInstalled(): Boolean;
var
  ResultCode: Integer;
  Output: AnsiString;
  TempFile: String;
begin
  Result := False;

  // Méthode 1: Essayer d'exécuter dotnet --list-runtimes
  TempFile := ExpandConstant('{tmp}\dotnet-check.txt');
  if Exec('cmd.exe', '/c dotnet --list-runtimes > "' + TempFile + '" 2>&1', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if LoadStringFromFile(TempFile, Output) then
    begin
      // Chercher "Microsoft.WindowsDesktop.App 8."
      if Pos('Microsoft.WindowsDesktop.App 8.', Output) > 0 then
      begin
        Result := True;
        DeleteFile(TempFile);
        Exit;
      end;
    end;
    DeleteFile(TempFile);
  end;

  // Méthode 2: Vérifier les dossiers d'installation
  if DirExists(ExpandConstant('{pf}\dotnet\shared\Microsoft.WindowsDesktop.App\8.0')) or
     DirExists(ExpandConstant('{pf}\dotnet\shared\Microsoft.WindowsDesktop.App')) then
  begin
    Result := True;
  end;
end;

// Télécharger .NET Runtime
function DownloadDotNetRuntime(): Boolean;
var
  DownloadPage: TDownloadWizardPage;
begin
  DownloadPage := CreateDownloadPage('Téléchargement de .NET 8.0', 'Téléchargement du .NET 8.0 Desktop Runtime...', nil);
  DownloadPage.Clear;
  DownloadPage.Add(DotNetRuntimeURL, DotNetRuntimeFile, '');

  DownloadPage.Show;
  try
    try
      DownloadPage.Download;
      Result := True;
    except
      if DownloadPage.AbortedByUser then
        Log('Download aborted by user.')
      else
        SuppressibleMsgBox(AddPeriod(GetExceptionMessage), mbCriticalError, MB_OK, IDOK);
      Result := False;
    end;
  finally
    DownloadPage.Hide;
  end;
end;

// Installer .NET Runtime
function InstallDotNetRuntime(): Boolean;
var
  ResultCode: Integer;
  InstallerPath: String;
begin
  InstallerPath := ExpandConstant('{tmp}\' + DotNetRuntimeFile);

  if not FileExists(InstallerPath) then
  begin
    MsgBox('Le fichier d''installation de .NET n''a pas été téléchargé correctement.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  if MsgBox('L''installation de .NET 8.0 Desktop Runtime va maintenant commencer.' + #13#10 + #13#10 +
            'Cela peut prendre quelques minutes. Voulez-vous continuer ?',
            mbConfirmation, MB_YESNO) = IDNO then
  begin
    Result := False;
    Exit;
  end;

  // Installer silencieusement
  if Exec(InstallerPath, '/install /quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
  begin
    if ResultCode = 0 then
    begin
      Result := True;
      MsgBox('.NET 8.0 Desktop Runtime a été installé avec succès !', mbInformation, MB_OK);
    end
    else
    begin
      MsgBox('L''installation de .NET 8.0 a échoué (Code: ' + IntToStr(ResultCode) + ').' + #13#10 +
             'Veuillez l''installer manuellement depuis https://dotnet.microsoft.com', mbError, MB_OK);
      Result := False;
    end;
  end
  else
  begin
    MsgBox('Impossible de lancer l''installateur .NET.', mbError, MB_OK);
    Result := False;
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;

  if not IsDotNetInstalled() then
  begin
    if MsgBox('.NET 8.0 Desktop Runtime est requis pour exécuter cette application.' + #13#10 + #13#10 +
              'Voulez-vous télécharger et installer .NET 8.0 Desktop Runtime maintenant?' + #13#10 + #13#10 +
              'Taille du téléchargement: ~55 MB' + #13#10 +
              'Une connexion Internet est requise.',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      // Télécharger
      if not DownloadDotNetRuntime() then
      begin
        Result := False;
        Exit;
      end;

      // Installer
      if not InstallDotNetRuntime() then
      begin
        Result := False;
        Exit;
      end;

      // Vérifier à nouveau
      if not IsDotNetInstalled() then
      begin
        MsgBox('L''installation de .NET semble avoir échoué. L''application pourrait ne pas fonctionner correctement.', mbError, MB_OK);
      end;
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
