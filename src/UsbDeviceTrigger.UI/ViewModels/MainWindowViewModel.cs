using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using UsbDeviceTrigger.Core.Models;
using UsbDeviceTrigger.Core.Services;

namespace UsbDeviceTrigger.UI.ViewModels;

/// <summary>
/// ViewModel principal de l'application
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IUsbMonitorService _usbMonitorService;
    private readonly IConfigurationService _configurationService;
    private readonly ICommandExecutionService _commandExecutionService;
    private readonly IAutoStartService _autoStartService;
    private readonly ILogger<MainWindowViewModel>? _logger;

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private string _statusMessage = "Prêt";

    [ObservableProperty]
    private string _lastEvent = "Aucun événement";

    [ObservableProperty]
    private ObservableCollection<string> _recentEvents = new();

    [ObservableProperty]
    private ApplicationSettings? _settings;

    [ObservableProperty]
    private object? _currentView;

    /// <summary>
    /// Action pour afficher les notifications (configurée par la vue)
    /// </summary>
    public Action<string, string>? ShowNotificationAction { get; set; }

    public MainWindowViewModel(
        IUsbMonitorService usbMonitorService,
        IConfigurationService configurationService,
        ICommandExecutionService commandExecutionService,
        IAutoStartService autoStartService,
        ILogger<MainWindowViewModel>? logger = null)
    {
        try
        {
            _logger = logger;
            _logger?.LogInformation("Construction de MainWindowViewModel...");

            _usbMonitorService = usbMonitorService ?? throw new ArgumentNullException(nameof(usbMonitorService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _commandExecutionService = commandExecutionService ?? throw new ArgumentNullException(nameof(commandExecutionService));
            _autoStartService = autoStartService ?? throw new ArgumentNullException(nameof(autoStartService));

            // S'abonner aux événements USB
            _usbMonitorService.DeviceConnected += OnDeviceConnected;
            _usbMonitorService.DeviceDisconnected += OnDeviceDisconnected;

            // Charger la configuration et démarrer la surveillance
            _ = InitializeAsync();

            _logger?.LogInformation("MainWindowViewModel construit");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur fatale dans le constructeur de MainWindowViewModel");
            StatusMessage = "Erreur d'initialisation";
            Settings = new ApplicationSettings();
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            _logger?.LogInformation("Initialisation de MainWindowViewModel...");

            // Charger la configuration
            Settings = await _configurationService.LoadConfigurationAsync();

            if (Settings == null)
            {
                _logger?.LogWarning("Settings est null, création d'une nouvelle instance");
                Settings = new ApplicationSettings();
            }

            // Démarrer la surveillance si configuré
            if (Settings.AutoStartMonitoring)
            {
                StartMonitoring();
            }

            UpdateStatusMessage();

            _logger?.LogInformation("MainWindowViewModel initialisé avec succès");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de l'initialisation: {Message}", ex.Message);
            StatusMessage = "Erreur lors de l'initialisation";
            Settings = new ApplicationSettings(); // Valeur par défaut
        }
    }

    [RelayCommand]
    private void StartMonitoring()
    {
        try
        {
            if (!IsMonitoring)
            {
                _usbMonitorService.StartMonitoring();
                IsMonitoring = true;
                StatusMessage = "Surveillance active";
                AddRecentEvent("Surveillance USB démarrée");
                _logger?.LogInformation("Surveillance USB démarrée");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors du démarrage de la surveillance");
            MessageBox.Show(
                $"Impossible de démarrer la surveillance USB:\n{ex.Message}",
                "Erreur",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void StopMonitoring()
    {
        try
        {
            if (IsMonitoring)
            {
                _usbMonitorService.StopMonitoring();
                IsMonitoring = false;
                StatusMessage = "Surveillance arrêtée";
                AddRecentEvent("Surveillance USB arrêtée");
                _logger?.LogInformation("Surveillance USB arrêtée");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de l'arrêt de la surveillance");
        }
    }

    [RelayCommand]
    private void ToggleMonitoring()
    {
        if (IsMonitoring)
            StopMonitoring();
        else
            StartMonitoring();
    }

    private async void OnDeviceConnected(object? sender, UsbDeviceInfo device)
    {
        // Exécuter sur le thread UI
        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                var eventMessage = $"Connecté: {device.Name}";
                LastEvent = eventMessage;
                AddRecentEvent(eventMessage);
                _logger?.LogInformation("Périphérique connecté: {Device}", device);

                // Chercher une configuration correspondante
                var config = Settings?.DeviceConfigurations.FirstOrDefault(c =>
                    c.IsEnabled && c.MatchesDevice(device));

                if (config?.OnConnectCommand != null)
                {
                    _logger?.LogInformation("Exécution de la commande de connexion pour {Device}", device.Name);
                    var result = await _commandExecutionService.ExecuteCommandAsync(config.OnConnectCommand);

                    if (result.Success)
                    {
                        AddRecentEvent($"✓ Commande exécutée: {config.OnConnectCommand.Command}");
                        if (Settings?.ShowNotifications ?? false)
                        {
                            ShowNotification("USB Connecté", $"{device.Name}\nCommande exécutée avec succès");
                        }
                    }
                    else
                    {
                        AddRecentEvent($"✗ Erreur commande: {result.ErrorMessage}");
                        if (Settings?.ShowNotifications ?? false)
                        {
                            ShowNotification("Erreur", $"Échec de la commande:\n{result.ErrorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erreur lors du traitement de la connexion");
            }
        });
    }

    private async void OnDeviceDisconnected(object? sender, UsbDeviceInfo device)
    {
        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                var eventMessage = $"Déconnecté: {device.Name}";
                LastEvent = eventMessage;
                AddRecentEvent(eventMessage);
                _logger?.LogInformation("Périphérique déconnecté: {Device}", device);

                // Chercher une configuration correspondante
                var config = Settings?.DeviceConfigurations.FirstOrDefault(c =>
                    c.IsEnabled && c.MatchesDevice(device));

                if (config?.OnDisconnectCommand != null)
                {
                    _logger?.LogInformation("Exécution de la commande de déconnexion pour {Device}", device.Name);
                    var result = await _commandExecutionService.ExecuteCommandAsync(config.OnDisconnectCommand);

                    if (result.Success)
                    {
                        AddRecentEvent($"✓ Commande exécutée: {config.OnDisconnectCommand.Command}");
                        if (Settings?.ShowNotifications ?? false)
                        {
                            ShowNotification("USB Déconnecté", $"{device.Name}\nCommande exécutée avec succès");
                        }
                    }
                    else
                    {
                        AddRecentEvent($"✗ Erreur commande: {result.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erreur lors du traitement de la déconnexion");
            }
        });
    }

    private void AddRecentEvent(string eventMessage)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var fullMessage = $"[{timestamp}] {eventMessage}";

        RecentEvents.Insert(0, fullMessage);

        // Limiter à 50 événements
        while (RecentEvents.Count > 50)
        {
            RecentEvents.RemoveAt(RecentEvents.Count - 1);
        }
    }

    private void UpdateStatusMessage()
    {
        try
        {
            if (IsMonitoring)
            {
                var deviceCount = Settings?.DeviceConfigurations?.Count(c => c.IsEnabled) ?? 0;
                StatusMessage = $"Surveillance active - {deviceCount} périphérique(s) configuré(s)";
            }
            else
            {
                StatusMessage = "Surveillance arrêtée";
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de la mise à jour du statut");
            StatusMessage = "Prêt";
        }
    }

    private void ShowNotification(string title, string message)
    {
        // Utiliser les notifications balloon du system tray
        ShowNotificationAction?.Invoke(title, message);
    }

    [RelayCommand]
    private void ExitApplication()
    {
        Application.Current.Shutdown();
    }
}
