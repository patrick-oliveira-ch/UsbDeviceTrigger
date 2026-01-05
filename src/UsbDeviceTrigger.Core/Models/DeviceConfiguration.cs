namespace UsbDeviceTrigger.Core.Models;

/// <summary>
/// Configuration associant un périphérique USB à des commandes de déclenchement
/// </summary>
public class DeviceConfiguration
{
    /// <summary>
    /// Identifiant unique de la configuration
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Périphérique USB associé
    /// </summary>
    public UsbDeviceInfo Device { get; set; } = new();

    /// <summary>
    /// Commande à exécuter lors de la connexion du périphérique
    /// </summary>
    public TriggerCommand? OnConnectCommand { get; set; }

    /// <summary>
    /// Commande à exécuter lors de la déconnexion du périphérique
    /// </summary>
    public TriggerCommand? OnDisconnectCommand { get; set; }

    /// <summary>
    /// Indique si cette configuration est activée
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Notes ou description de la configuration
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Date de création de la configuration
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Date de dernière modification
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Vérifie si cette configuration correspond au périphérique donné
    /// </summary>
    public bool MatchesDevice(UsbDeviceInfo device)
    {
        return Device.IsSameDevice(device);
    }

    /// <summary>
    /// Vérifie si la configuration a au moins une commande définie
    /// </summary>
    public bool HasCommands()
    {
        return (OnConnectCommand?.IsValid() ?? false) ||
               (OnDisconnectCommand?.IsValid() ?? false);
    }
}
