using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using UsbDeviceTrigger.Core.Models;
using UsbDeviceTrigger.Core.Services;
using UsbDeviceTrigger.UI.Views;

namespace UsbDeviceTrigger.UI.ViewModels;

/// <summary>
/// ViewModel pour la liste des périphériques configurés
/// </summary>
public partial class DeviceListViewModel : ViewModelBase
{
    private readonly IConfigurationService _configurationService;
    private readonly IUsbMonitorService _usbMonitorService;
    private readonly ILogger<DeviceListViewModel>? _logger;

    [ObservableProperty]
    private ObservableCollection<DeviceConfiguration> _deviceConfigurations = new();

    [ObservableProperty]
    private ObservableCollection<UsbDeviceInfo> _connectedDevices = new();

    [ObservableProperty]
    private DeviceConfiguration? _selectedConfiguration;

    public DeviceListViewModel(
        IConfigurationService configurationService,
        IUsbMonitorService usbMonitorService,
        ILogger<DeviceListViewModel>? logger = null)
    {
        _configurationService = configurationService;
        _usbMonitorService = usbMonitorService;
        _logger = logger;

        _logger?.LogInformation("DeviceListViewModel créé");

        // Charger les données de manière asynchrone après la création
        Task.Run(async () =>
        {
            await Task.Delay(100); // Petit délai pour laisser l'UI se charger
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await RefreshAsync();
            });
        });
    }

    private async Task LoadDataAsync()
    {
        try
        {
            _logger?.LogInformation("Rechargement des données...");

            // Charger les configurations
            var settings = await _configurationService.LoadConfigurationAsync();

            DeviceConfigurations.Clear();
            if (settings?.DeviceConfigurations != null)
            {
                foreach (var config in settings.DeviceConfigurations)
                {
                    DeviceConfigurations.Add(config);
                }
            }

            // Charger les périphériques connectés
            var devices = _usbMonitorService.GetConnectedDevices();

            ConnectedDevices.Clear();
            if (devices != null)
            {
                foreach (var device in devices)
                {
                    ConnectedDevices.Add(device);
                }
            }

            _logger?.LogInformation("Données rechargées: {ConfigCount} configurations, {DeviceCount} périphériques connectés",
                DeviceConfigurations.Count, ConnectedDevices.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors du rechargement: {Message}", ex.Message);
            MessageBox.Show($"Erreur lors du rechargement:\n{ex.Message}",
                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task AddDeviceAsync()
    {
        try
        {
            _logger?.LogInformation("Ouverture du dialogue de sélection de périphérique");

            // Créer le ViewModel pour le dialogue
            var dialogViewModel = new DeviceSelectionViewModel(_usbMonitorService);

            // Créer et afficher le dialogue
            var dialog = new DeviceSelectionDialog
            {
                DataContext = dialogViewModel,
                Owner = Application.Current.MainWindow
            };

            var result = dialog.ShowDialog();

            // Si l'utilisateur a sélectionné un périphérique
            if (result == true && dialogViewModel.SelectedDevice != null)
            {
                var selectedDevice = dialogViewModel.SelectedDevice;
                _logger?.LogInformation("Périphérique sélectionné: {DeviceName} (VID: {VID}, PID: {PID})",
                    selectedDevice.Name, selectedDevice.VendorId, selectedDevice.ProductId);

                // Créer une nouvelle configuration avec le périphérique sélectionné
                var newConfig = new DeviceConfiguration
                {
                    Device = new UsbDeviceInfo
                    {
                        DeviceId = selectedDevice.DeviceId,
                        Name = selectedDevice.Name,
                        Description = selectedDevice.Description,
                        VendorId = selectedDevice.VendorId,
                        ProductId = selectedDevice.ProductId,
                        SerialNumber = selectedDevice.SerialNumber
                    },
                    OnConnectCommand = new TriggerCommand
                    {
                        Command = "powershell",
                        Arguments = "-Command \"Write-Host 'Périphérique connecté'\""
                    },
                    IsEnabled = true
                };

                await _configurationService.AddDeviceConfigurationAsync(newConfig);
                DeviceConfigurations.Add(newConfig);

                MessageBox.Show(
                    $"Périphérique '{selectedDevice.Name}' ajouté avec succès!\n\n" +
                    $"Vous pouvez maintenant configurer les commandes à exécuter.",
                    "Succès",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _logger?.LogInformation("Configuration créée pour le périphérique {DeviceName}", selectedDevice.Name);
            }
            else
            {
                _logger?.LogInformation("Sélection de périphérique annulée");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de l'ajout du périphérique");
            MessageBox.Show($"Erreur lors de l'ajout:\n{ex.Message}",
                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task DeleteDeviceAsync(DeviceConfiguration config)
    {
        if (config == null) return;

        var result = MessageBox.Show(
            $"Voulez-vous vraiment supprimer la configuration pour '{config.Device.Name}'?",
            "Confirmer la suppression",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _configurationService.RemoveDeviceConfigurationAsync(config.Id);
                DeviceConfigurations.Remove(config);

                MessageBox.Show("Configuration supprimée avec succès!", "Succès",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erreur lors de la suppression");
                MessageBox.Show($"Erreur lors de la suppression:\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task ToggleEnabledAsync(DeviceConfiguration config)
    {
        if (config == null) return;

        try
        {
            config.IsEnabled = !config.IsEnabled;
            await _configurationService.UpdateDeviceConfigurationAsync(config);

            _logger?.LogInformation("Configuration {Status} pour {Device}",
                config.IsEnabled ? "activée" : "désactivée", config.Device.Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de la mise à jour");
            MessageBox.Show($"Erreur:\n{ex.Message}", "Erreur",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task EditDeviceAsync(DeviceConfiguration config)
    {
        if (config == null) return;

        try
        {
            _logger?.LogInformation("Ouverture du dialogue d'édition pour {DeviceName}", config.Device.Name);

            // Créer le ViewModel pour le dialogue
            var dialogViewModel = new DeviceEditViewModel();
            dialogViewModel.LoadConfiguration(config);

            // Créer et afficher le dialogue
            var dialog = new DeviceEditDialog
            {
                DataContext = dialogViewModel,
                Owner = Application.Current.MainWindow
            };

            var result = dialog.ShowDialog();

            // Si l'utilisateur a confirmé les modifications
            if (result == true && dialogViewModel.IsConfirmed)
            {
                // Appliquer les modifications
                dialogViewModel.ApplyToConfiguration(config);

                // Sauvegarder la configuration
                await _configurationService.UpdateDeviceConfigurationAsync(config);

                // Rafraîchir l'affichage
                await RefreshAsync();

                MessageBox.Show("Configuration mise à jour avec succès!", "Succès",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _logger?.LogInformation("Configuration mise à jour pour {DeviceName}", config.Device.Name);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de l'édition");
            MessageBox.Show($"Erreur lors de la mise à jour:\n{ex.Message}",
                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }
}
