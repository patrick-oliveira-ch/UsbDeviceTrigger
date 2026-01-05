using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using UsbDeviceTrigger.Core.Models;
using UsbDeviceTrigger.Core.Services;

namespace UsbDeviceTrigger.UI.ViewModels;

/// <summary>
/// ViewModel pour les paramètres de l'application
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IConfigurationService _configurationService;
    private readonly IAutoStartService _autoStartService;
    private readonly ILogger<SettingsViewModel>? _logger;

    [ObservableProperty]
    private ApplicationSettings? _settings;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _minimizeToTray;

    [ObservableProperty]
    private bool _showNotifications;

    [ObservableProperty]
    private bool _logCommandExecution;

    [ObservableProperty]
    private string _logFilePath = string.Empty;

    [ObservableProperty]
    private bool _autoStartMonitoring;

    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private string _configFilePath = string.Empty;

    public SettingsViewModel(
        IConfigurationService configurationService,
        IAutoStartService autoStartService,
        ILogger<SettingsViewModel>? logger = null)
    {
        _configurationService = configurationService;
        _autoStartService = autoStartService;
        _logger = logger;

        ConfigFilePath = _configurationService.GetConfigFilePath();

        _ = LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            Settings = await _configurationService.LoadConfigurationAsync();

            // Charger les valeurs dans les propriétés
            StartWithWindows = Settings.StartWithWindows && _autoStartService.IsAutoStartEnabled();
            MinimizeToTray = Settings.MinimizeToTray;
            ShowNotifications = Settings.ShowNotifications;
            LogCommandExecution = Settings.LogCommandExecution;
            LogFilePath = Settings.LogFilePath;
            AutoStartMonitoring = Settings.AutoStartMonitoring;
            StartMinimized = Settings.StartMinimized;

            _logger?.LogInformation("Paramètres chargés");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors du chargement des paramètres");
            MessageBox.Show($"Erreur lors du chargement des paramètres:\n{ex.Message}",
                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (Settings == null) return;

        try
        {
            // Mettre à jour les valeurs depuis les propriétés
            Settings.StartWithWindows = StartWithWindows;
            Settings.MinimizeToTray = MinimizeToTray;
            Settings.ShowNotifications = ShowNotifications;
            Settings.LogCommandExecution = LogCommandExecution;
            Settings.LogFilePath = LogFilePath;
            Settings.AutoStartMonitoring = AutoStartMonitoring;
            Settings.StartMinimized = StartMinimized;

            // Sauvegarder la configuration
            await _configurationService.SaveConfigurationAsync(Settings);

            // Gérer le démarrage automatique avec Windows
            var appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(appPath))
            {
                if (StartWithWindows)
                {
                    _autoStartService.EnableAutoStart(appPath, StartMinimized);
                }
                else
                {
                    _autoStartService.DisableAutoStart();
                }
            }

            _logger?.LogInformation("Paramètres sauvegardés avec succès");

            MessageBox.Show("Paramètres sauvegardés avec succès!",
                "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de la sauvegarde des paramètres");
            MessageBox.Show($"Erreur lors de la sauvegarde:\n{ex.Message}",
                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void BrowseLogPath()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Choisir l'emplacement du fichier de log",
            Filter = "Fichiers log (*.log)|*.log|Tous les fichiers (*.*)|*.*",
            FileName = "app.log",
            DefaultExt = ".log"
        };

        if (dialog.ShowDialog() == true)
        {
            LogFilePath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void OpenConfigFolder()
    {
        try
        {
            var configDir = System.IO.Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(configDir) && System.IO.Directory.Exists(configDir))
            {
                System.Diagnostics.Process.Start("explorer.exe", configDir);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de l'ouverture du dossier de configuration");
            MessageBox.Show($"Impossible d'ouvrir le dossier:\n{ex.Message}",
                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ResetSettingsAsync()
    {
        var result = MessageBox.Show(
            "Voulez-vous vraiment réinitialiser tous les paramètres?\nCette action ne peut pas être annulée.",
            "Confirmer la réinitialisation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                Settings = new ApplicationSettings();
                await _configurationService.SaveConfigurationAsync(Settings);
                await LoadSettingsAsync();

                MessageBox.Show("Paramètres réinitialisés avec succès!",
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erreur lors de la réinitialisation");
                MessageBox.Show($"Erreur lors de la réinitialisation:\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
