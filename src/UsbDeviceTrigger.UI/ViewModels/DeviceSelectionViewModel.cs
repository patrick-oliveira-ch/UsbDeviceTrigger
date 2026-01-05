using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UsbDeviceTrigger.Core.Models;
using UsbDeviceTrigger.Core.Services;

namespace UsbDeviceTrigger.UI.ViewModels;

/// <summary>
/// ViewModel pour le dialogue de sélection de périphérique USB
/// </summary>
public partial class DeviceSelectionViewModel : ObservableObject
{
    private readonly IUsbMonitorService _usbMonitorService;
    private List<UsbDeviceInfo> _allDevices = new();

    [ObservableProperty]
    private ObservableCollection<UsbDeviceInfo> _availableDevices = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private UsbDeviceInfo? _selectedDevice;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string _statusMessage = "Recherche des périphériques USB...";

    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>
    /// Indique si un périphérique a été sélectionné et le dialogue fermé avec succès
    /// </summary>
    public bool DeviceSelected { get; private set; }

    public DeviceSelectionViewModel(IUsbMonitorService usbMonitorService)
    {
        _usbMonitorService = usbMonitorService;
        _ = LoadDevicesAsync();

        // Filtrer la liste quand le texte de recherche change
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SearchText))
            {
                FilterDevices();
            }
        };
    }

    /// <summary>
    /// Charge la liste des périphériques USB connectés
    /// </summary>
    private async Task LoadDevicesAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Recherche des périphériques USB...";

            // GetConnectedDevices est synchrone, donc on l'exécute dans une tâche pour ne pas bloquer l'UI
            var devices = await Task.Run(() => _usbMonitorService.GetConnectedDevices());

            _allDevices = devices;

            AvailableDevices.Clear();
            foreach (var device in devices)
            {
                AvailableDevices.Add(device);
            }

            if (AvailableDevices.Count == 0)
            {
                StatusMessage = "Aucun périphérique USB détecté. Branchez un périphérique et cliquez sur Actualiser.";
            }
            else
            {
                StatusMessage = $"{AvailableDevices.Count} périphérique(s) USB détecté(s)";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erreur lors de la détection : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Filtre les périphériques selon le texte de recherche
    /// </summary>
    private void FilterDevices()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            // Aucun filtre, afficher tous les périphériques
            AvailableDevices.Clear();
            foreach (var device in _allDevices)
            {
                AvailableDevices.Add(device);
            }
            StatusMessage = $"{AvailableDevices.Count} périphérique(s) USB détecté(s)";
        }
        else
        {
            // Filtrer par nom, VID ou PID
            var searchLower = SearchText.ToLower();
            var filtered = _allDevices.Where(d =>
                (d.Name?.ToLower().Contains(searchLower) ?? false) ||
                (d.VendorId?.ToLower().Contains(searchLower) ?? false) ||
                (d.ProductId?.ToLower().Contains(searchLower) ?? false) ||
                (d.Description?.ToLower().Contains(searchLower) ?? false)
            ).ToList();

            AvailableDevices.Clear();
            foreach (var device in filtered)
            {
                AvailableDevices.Add(device);
            }

            StatusMessage = $"{AvailableDevices.Count} périphérique(s) trouvé(s) sur {_allDevices.Count}";
        }
    }

    /// <summary>
    /// Actualise la liste des périphériques
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDevicesAsync();
    }

    /// <summary>
    /// Confirme la sélection et ferme le dialogue
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        DeviceSelected = true;
    }

    private bool CanConfirm() => SelectedDevice != null;

    /// <summary>
    /// Annule la sélection et ferme le dialogue
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        DeviceSelected = false;
        SelectedDevice = null;
    }
}
