using UsbDeviceTrigger.Core.Models;

namespace UsbDeviceTrigger.Core.Services;

/// <summary>
/// Interface pour le service de surveillance des périphériques USB
/// </summary>
public interface IUsbMonitorService : IDisposable
{
    /// <summary>
    /// Événement déclenché lors de la connexion d'un périphérique USB
    /// </summary>
    event EventHandler<UsbDeviceInfo>? DeviceConnected;

    /// <summary>
    /// Événement déclenché lors de la déconnexion d'un périphérique USB
    /// </summary>
    event EventHandler<UsbDeviceInfo>? DeviceDisconnected;

    /// <summary>
    /// Indique si la surveillance est active
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Démarre la surveillance des événements USB
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Arrête la surveillance des événements USB
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Récupère la liste des périphériques USB actuellement connectés
    /// </summary>
    List<UsbDeviceInfo> GetConnectedDevices();
}
