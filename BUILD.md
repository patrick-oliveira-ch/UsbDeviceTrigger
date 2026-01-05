# Guide de compilation - USB Device Trigger

Ce guide explique comment compiler et créer l'installateur pour USB Device Trigger.

## Prérequis

1. **Visual Studio 2022** (ou supérieur) avec:
   - Charge de travail "Développement .NET Desktop"
   - .NET 8.0 SDK

2. **Inno Setup 6** (pour créer l'installateur)
   - Télécharger depuis: https://jrsoftware.org/isinfo.php

3. **Git** (optionnel, pour le versioning)

## Installation des prérequis

### 1. Installer .NET 8.0 SDK

Si vous n'avez pas déjà Visual Studio 2022 avec .NET 8.0:

```powershell
# Télécharger et installer .NET 8.0 SDK
# URL: https://dotnet.microsoft.com/download/dotnet/8.0
```

Vérifiez l'installation:
```powershell
dotnet --version
# Devrait afficher: 8.0.x
```

### 2. Installer Inno Setup

1. Téléchargez Inno Setup 6 depuis https://jrsoftware.org/isdl.php
2. Exécutez l'installateur
3. Ajoutez Inno Setup au PATH (optionnel)

## Compilation de l'application

### Méthode 1: Utiliser Visual Studio

1. Ouvrez `UsbDeviceTrigger.sln` dans Visual Studio 2022

2. Restaurez les packages NuGet:
   - Clic droit sur la solution → "Restaurer les packages NuGet"
   - Ou: `Tools > NuGet Package Manager > Package Manager Console`
   ```
   Update-Package -reinstall
   ```

3. Changez la configuration en **Release**:
   - Menu: `Build > Configuration Manager`
   - Sélectionnez "Release" dans "Active solution configuration"

4. Compilez la solution:
   - Menu: `Build > Build Solution` (Ctrl+Shift+B)
   - Ou clic droit sur la solution → "Build Solution"

5. Les fichiers compilés seront dans:
   ```
   src\UsbDeviceTrigger.UI\bin\Release\net8.0-windows\
   ```

### Méthode 2: Utiliser la ligne de commande

Ouvrez PowerShell dans le dossier du projet:

```powershell
# Se déplacer vers le dossier du projet
cd C:\Users\Patri\Softwares\UsbDeviceTrigger

# Restaurer les packages NuGet
dotnet restore

# Compiler en mode Release
dotnet build --configuration Release

# Ou publier (crée un package autonome)
dotnet publish src\UsbDeviceTrigger.UI\UsbDeviceTrigger.UI.csproj `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -o publish\
```

## Création de l'installateur

### 1. Vérifier les chemins dans le script Inno Setup

Ouvrez `setup\UsbDeviceTrigger.iss` et vérifiez la ligne:

```ini
Source: "..\src\UsbDeviceTrigger.UI\bin\Release\net8.0-windows\*"; ...
```

Assurez-vous que ce chemin correspond à votre dossier de build.

### 2. Compiler avec Inno Setup

#### Méthode GUI:
1. Ouvrez Inno Setup Compiler
2. Ouvrez le fichier `setup\UsbDeviceTrigger.iss`
3. Menu: `Build > Compile` (Ctrl+F9)

#### Méthode ligne de commande:
```powershell
# Si Inno Setup est dans le PATH
iscc setup\UsbDeviceTrigger.iss

# Sinon, utilisez le chemin complet
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" setup\UsbDeviceTrigger.iss
```

### 3. Récupérer l'installateur

L'installateur sera créé dans:
```
output\UsbDeviceTriggerSetup-v1.0.0.exe
```

## Tests avant distribution

### 1. Test sur machine de développement

```powershell
# Exécuter l'application directement
.\src\UsbDeviceTrigger.UI\bin\Release\net8.0-windows\UsbDeviceTrigger.UI.exe
```

Vérifiez:
- ✓ L'application démarre sans erreur
- ✓ La surveillance USB fonctionne
- ✓ Les périphériques sont détectés
- ✓ Les commandes s'exécutent

### 2. Test de l'installateur

1. Installez sur une machine de test (ou VM)
2. Vérifiez:
   - ✓ L'installation se termine sans erreur
   - ✓ L'icône du bureau est créée
   - ✓ L'application démarre
   - ✓ Le démarrage automatique fonctionne
   - ✓ La désinstallation fonctionne

### 3. Test sur machine propre

Testez sur une machine Windows propre sans .NET 8.0:
- ✓ L'installateur détecte l'absence de .NET 8.0
- ✓ Le message d'erreur approprié s'affiche
- ✓ Le lien de téléchargement fonctionne

## Dépannage de compilation

### Erreur: "SDK not found"

```powershell
# Vérifier que .NET 8.0 SDK est installé
dotnet --list-sdks

# Si absent, réinstaller .NET 8.0 SDK
```

### Erreur: "Package restore failed"

```powershell
# Nettoyer et restaurer
dotnet clean
dotnet restore --force
```

### Erreur: "Could not load file MaterialDesignThemes"

```powershell
# Réinstaller les packages NuGet
dotnet restore
dotnet build --no-restore
```

### Erreur Inno Setup: "Source file not found"

Vérifiez que:
1. La compilation a réussi
2. Le chemin dans `[Files]` du script .iss est correct
3. Les fichiers existent dans `bin\Release\net8.0-windows\`

## Publication pour distribution

### 1. Créer une release sur GitHub

```bash
# Tag de version
git tag -a v1.0.0 -m "Version 1.0.0"
git push origin v1.0.0

# Créer une release sur GitHub avec l'installateur
```

### 2. Fichiers à distribuer

- `UsbDeviceTriggerSetup-v1.0.0.exe` - Installateur principal
- `README.md` - Documentation
- `LICENSE` - Licence (si applicable)

### 3. Informations à fournir

Dans la release, incluez:
- Notes de version (changelog)
- Prérequis (.NET 8.0 Desktop Runtime)
- Instructions d'installation
- Lien vers la documentation

## Build automatisé (CI/CD)

Pour mettre en place un build automatisé avec GitHub Actions:

Créez `.github/workflows/build.yml`:

```yaml
name: Build

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --configuration Release --no-restore

    - name: Create installer with Inno Setup
      run: |
        choco install innosetup
        iscc setup\UsbDeviceTrigger.iss

    - name: Upload artifact
      uses: actions/upload-artifact@v3
      with:
        name: installer
        path: output\UsbDeviceTriggerSetup-v*.exe
```

## Conseils pour la distribution

1. **Signature de code**: Signez l'exécutable et l'installateur avec un certificat
2. **Tests**: Testez sur plusieurs versions de Windows (10, 11)
3. **Antivirus**: Soumettez à VirusTotal pour éviter les faux positifs
4. **Documentation**: Mettez à jour le README avec des captures d'écran
5. **Support**: Préparez une FAQ pour les problèmes courants

## Checklist de release

- [ ] Code compilé sans warnings
- [ ] Tests passés sur Windows 10 et 11
- [ ] Installateur créé et testé
- [ ] README mis à jour
- [ ] Changelog mis à jour
- [ ] Version incrementée dans le code
- [ ] Tag Git créé
- [ ] Release GitHub créée
- [ ] Documentation à jour
