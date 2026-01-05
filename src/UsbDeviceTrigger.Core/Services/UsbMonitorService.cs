using System.Management;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using UsbDeviceTrigger.Core.Models;

namespace UsbDeviceTrigger.Core.Services;

/// <summary>
/// Service de surveillance des événements USB via WMI
/// </summary>
public class UsbMonitorService : IUsbMonitorService
{
    private readonly ILogger<UsbMonitorService>? _logger;
    private ManagementEventWatcher? _insertionWatcher;
    private ManagementEventWatcher? _removalWatcher;
    private bool _isMonitoring;
    private bool _disposed;

    public event EventHandler<UsbDeviceInfo>? DeviceConnected;
    public event EventHandler<UsbDeviceInfo>? DeviceDisconnected;

    public bool IsMonitoring => _isMonitoring;

    public UsbMonitorService(ILogger<UsbMonitorService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Démarre la surveillance des événements USB
    /// </summary>
    public void StartMonitoring()
    {
        if (_isMonitoring)
        {
            _logger?.LogWarning("La surveillance USB est déjà active");
            return;
        }

        try
        {
            _logger?.LogInformation("Démarrage de la surveillance USB...");

            // Surveillance de l'insertion de périphériques
            var insertQuery = new WqlEventQuery(
                "SELECT * FROM __InstanceCreationEvent WITHIN 2 " +
                "WHERE TargetInstance ISA 'Win32_PnPEntity' " +
                "AND TargetInstance.DeviceID LIKE 'USB%'"
            );

            _insertionWatcher = new ManagementEventWatcher(insertQuery);
            _insertionWatcher.EventArrived += OnDeviceInserted;
            _insertionWatcher.Start();

            // Surveillance de la suppression de périphériques
            var removalQuery = new WqlEventQuery(
                "SELECT * FROM __InstanceDeletionEvent WITHIN 2 " +
                "WHERE TargetInstance ISA 'Win32_PnPEntity' " +
                "AND TargetInstance.DeviceID LIKE 'USB%'"
            );

            _removalWatcher = new ManagementEventWatcher(removalQuery);
            _removalWatcher.EventArrived += OnDeviceRemoved;
            _removalWatcher.Start();

            _isMonitoring = true;
            _logger?.LogInformation("Surveillance USB démarrée avec succès");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors du démarrage de la surveillance USB");
            throw;
        }
    }

    /// <summary>
    /// Arrête la surveillance des événements USB
    /// </summary>
    public void StopMonitoring()
    {
        if (!_isMonitoring)
        {
            _logger?.LogWarning("La surveillance USB n'est pas active");
            return;
        }

        try
        {
            _logger?.LogInformation("Arrêt de la surveillance USB...");

            if (_insertionWatcher != null)
            {
                _insertionWatcher.Stop();
                _insertionWatcher.EventArrived -= OnDeviceInserted;
                _insertionWatcher.Dispose();
                _insertionWatcher = null;
            }

            if (_removalWatcher != null)
            {
                _removalWatcher.Stop();
                _removalWatcher.EventArrived -= OnDeviceRemoved;
                _removalWatcher.Dispose();
                _removalWatcher = null;
            }

            _isMonitoring = false;
            _logger?.LogInformation("Surveillance USB arrêtée");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de l'arrêt de la surveillance USB");
        }
    }

    /// <summary>
    /// Récupère la liste des périphériques USB actuellement connectés
    /// </summary>
    public List<UsbDeviceInfo> GetConnectedDevices()
    {
        var devices = new List<UsbDeviceInfo>();

        try
        {
            _logger?.LogDebug("Recherche des périphériques USB connectés...");

            var query = new SelectQuery("SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%'");
            using var searcher = new ManagementObjectSearcher(query);

            foreach (var device in searcher.Get())
            {
                try
                {
                    var deviceInfo = ParseDeviceInfo(device);
                    if (deviceInfo != null)
                    {
                        deviceInfo.IsConnected = true;
                        devices.Add(deviceInfo);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Erreur lors du parsing d'un périphérique USB");
                }
            }

            _logger?.LogInformation("{Count} périphériques USB détectés", devices.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de la récupération des périphériques USB");
        }

        return devices;
    }

    /// <summary>
    /// Gestionnaire d'événement pour l'insertion de périphérique
    /// </summary>
    private void OnDeviceInserted(object sender, EventArrivedEventArgs e)
    {
        try
        {
            var targetInstance = e.NewEvent["TargetInstance"] as ManagementBaseObject;
            if (targetInstance != null)
            {
                var deviceInfo = ParseDeviceInfo(targetInstance);
                if (deviceInfo != null)
                {
                    deviceInfo.IsConnected = true;
                    _logger?.LogInformation("Périphérique USB connecté : {Device}", deviceInfo.ToString());
                    DeviceConnected?.Invoke(this, deviceInfo);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors du traitement de l'insertion de périphérique");
        }
    }

    /// <summary>
    /// Gestionnaire d'événement pour la suppression de périphérique
    /// </summary>
    private void OnDeviceRemoved(object sender, EventArrivedEventArgs e)
    {
        try
        {
            var targetInstance = e.NewEvent["TargetInstance"] as ManagementBaseObject;
            if (targetInstance != null)
            {
                var deviceInfo = ParseDeviceInfo(targetInstance);
                if (deviceInfo != null)
                {
                    deviceInfo.IsConnected = false;
                    _logger?.LogInformation("Périphérique USB déconnecté : {Device}", deviceInfo.ToString());
                    DeviceDisconnected?.Invoke(this, deviceInfo);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors du traitement de la suppression de périphérique");
        }
    }

    /// <summary>
    /// Parse les informations d'un périphérique depuis ManagementBaseObject
    /// </summary>
    private UsbDeviceInfo? ParseDeviceInfo(ManagementBaseObject device)
    {
        try
        {
            var deviceId = device["DeviceID"]?.ToString();
            if (string.IsNullOrEmpty(deviceId))
                return null;

            var deviceInfo = new UsbDeviceInfo
            {
                DeviceId = deviceId,
                Name = device["Name"]?.ToString() ?? "Périphérique USB inconnu",
                Description = device["Description"]?.ToString() ?? string.Empty,
                LastSeen = DateTime.Now
            };

            // Extraire VID et PID du DeviceID
            // Format typique : USB\VID_046D&PID_C52B\5&2A8EFA92&0&2
            var vidPidPattern = @"VID_([0-9A-F]{4})&PID_([0-9A-F]{4})";
            var match = Regex.Match(deviceId, vidPidPattern, RegexOptions.IgnoreCase);

            if (match.Success)
            {
                deviceInfo.VendorId = match.Groups[1].Value.ToUpper();
                deviceInfo.ProductId = match.Groups[2].Value.ToUpper();
            }

            // Essayer d'extraire le numéro de série (après le dernier backslash)
            var parts = deviceId.Split('\\');
            if (parts.Length > 2)
            {
                // Le numéro de série est généralement dans la 3ème partie
                var serialPart = parts[2];
                // Vérifier que ce n'est pas juste un ID généré par Windows
                if (!serialPart.Contains("&") && serialPart.Length > 1)
                {
                    deviceInfo.SerialNumber = serialPart;
                }
            }

            return deviceInfo;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Erreur lors du parsing des informations du périphérique");
            return null;
        }
    }

    /// <summary>
    /// Libère les ressources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        StopMonitoring();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
