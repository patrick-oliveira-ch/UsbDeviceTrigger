namespace UsbDeviceTrigger.Core.Models;

/// <summary>
/// Paramètres globaux de l'application
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    /// Version du schéma de configuration
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Démarrer l'application avec Windows
    /// </summary>
    public bool StartWithWindows { get; set; } = true;

    /// <summary>
    /// Minimiser vers la barre d'état système au lieu de la fermer
    /// </summary>
    public bool MinimizeToTray { get; set; } = true;

    /// <summary>
    /// Afficher les notifications lors des événements USB
    /// </summary>
    public bool ShowNotifications { get; set; } = true;

    /// <summary>
    /// Activer l'enregistrement des exécutions de commandes
    /// </summary>
    public bool LogCommandExecution { get; set; } = true;

    /// <summary>
    /// Chemin du fichier de log
    /// </summary>
    public string LogFilePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "UsbDeviceTrigger",
        "Logs",
        "app.log"
    );

    /// <summary>
    /// Liste des configurations de périphériques
    /// </summary>
    public List<DeviceConfiguration> DeviceConfigurations { get; set; } = new();

    /// <summary>
    /// Démarrer la surveillance USB automatiquement au lancement
    /// </summary>
    public bool AutoStartMonitoring { get; set; } = true;

    /// <summary>
    /// Démarrer l'application minimisée
    /// </summary>
    public bool StartMinimized { get; set; } = false;

    /// <summary>
    /// Langue de l'interface (pour future internationalisation)
    /// </summary>
    public string Language { get; set; } = "fr-FR";

    /// <summary>
    /// Ajoute une nouvelle configuration de périphérique
    /// </summary>
    public void AddDeviceConfiguration(DeviceConfiguration config)
    {
        if (config != null && !DeviceConfigurations.Any(c => c.Id == config.Id))
        {
            DeviceConfigurations.Add(config);
        }
    }

    /// <summary>
    /// Supprime une configuration de périphérique par son ID
    /// </summary>
    public bool RemoveDeviceConfiguration(Guid id)
    {
        var config = DeviceConfigurations.FirstOrDefault(c => c.Id == id);
        if (config != null)
        {
            DeviceConfigurations.Remove(config);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Met à jour une configuration existante
    /// </summary>
    public bool UpdateDeviceConfiguration(DeviceConfiguration updatedConfig)
    {
        var index = DeviceConfigurations.FindIndex(c => c.Id == updatedConfig.Id);
        if (index >= 0)
        {
            updatedConfig.ModifiedDate = DateTime.Now;
            DeviceConfigurations[index] = updatedConfig;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Récupère une configuration par ID
    /// </summary>
    public DeviceConfiguration? GetDeviceConfiguration(Guid id)
    {
        return DeviceConfigurations.FirstOrDefault(c => c.Id == id);
    }
}
