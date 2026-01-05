using UsbDeviceTrigger.Core.Models;

namespace UsbDeviceTrigger.Core.Services;

/// <summary>
/// Interface pour le service de gestion de la configuration
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Charge la configuration depuis le fichier JSON
    /// </summary>
    Task<ApplicationSettings> LoadConfigurationAsync();

    /// <summary>
    /// Sauvegarde la configuration dans le fichier JSON
    /// </summary>
    Task SaveConfigurationAsync(ApplicationSettings settings);

    /// <summary>
    /// Ajoute une nouvelle configuration de périphérique
    /// </summary>
    Task AddDeviceConfigurationAsync(DeviceConfiguration config);

    /// <summary>
    /// Met à jour une configuration de périphérique existante
    /// </summary>
    Task UpdateDeviceConfigurationAsync(DeviceConfiguration config);

    /// <summary>
    /// Supprime une configuration de périphérique
    /// </summary>
    Task<bool> RemoveDeviceConfigurationAsync(Guid id);

    /// <summary>
    /// Récupère le chemin du fichier de configuration
    /// </summary>
    string GetConfigFilePath();

    /// <summary>
    /// Crée une sauvegarde de la configuration actuelle
    /// </summary>
    Task CreateBackupAsync();
}
