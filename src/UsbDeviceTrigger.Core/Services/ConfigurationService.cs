using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UsbDeviceTrigger.Core.Models;

namespace UsbDeviceTrigger.Core.Services;

/// <summary>
/// Service de gestion de la configuration avec persistance JSON
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService>? _logger;
    private readonly string _configFilePath;
    private readonly string _configDirectory;
    private ApplicationSettings? _currentSettings;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public ConfigurationService(ILogger<ConfigurationService>? logger = null)
    {
        _logger = logger;

        // Définir le répertoire de configuration dans %APPDATA%
        _configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "UsbDeviceTrigger"
        );

        _configFilePath = Path.Combine(_configDirectory, "config.json");

        // Créer le répertoire s'il n'existe pas
        Directory.CreateDirectory(_configDirectory);
    }

    /// <summary>
    /// Charge la configuration depuis le fichier JSON
    /// </summary>
    public async Task<ApplicationSettings> LoadConfigurationAsync()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                _logger?.LogInformation("Chargement de la configuration depuis {Path}", _configFilePath);

                var json = await File.ReadAllTextAsync(_configFilePath);
                var settings = JsonConvert.DeserializeObject<ApplicationSettings>(json);

                if (settings != null)
                {
                    _currentSettings = settings;
                    _logger?.LogInformation("Configuration chargée avec succès. {Count} périphériques configurés.",
                        settings.DeviceConfigurations.Count);
                    return settings;
                }
            }

            _logger?.LogWarning("Fichier de configuration introuvable. Création d'une nouvelle configuration.");
            _currentSettings = new ApplicationSettings();
            await SaveConfigurationAsync(_currentSettings);
            return _currentSettings;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors du chargement de la configuration");
            _currentSettings = new ApplicationSettings();
            return _currentSettings;
        }
    }

    /// <summary>
    /// Sauvegarde la configuration dans le fichier JSON
    /// </summary>
    public async Task SaveConfigurationAsync(ApplicationSettings settings)
    {
        await _saveLock.WaitAsync();
        try
        {
            // Créer une sauvegarde avant de sauvegarder
            if (File.Exists(_configFilePath))
            {
                await CreateBackupAsync();
            }

            _logger?.LogInformation("Sauvegarde de la configuration vers {Path}", _configFilePath);

            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            await File.WriteAllTextAsync(_configFilePath, json);

            _currentSettings = settings;
            _logger?.LogInformation("Configuration sauvegardée avec succès.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de la sauvegarde de la configuration");
            throw;
        }
        finally
        {
            _saveLock.Release();
        }
    }

    /// <summary>
    /// Ajoute une nouvelle configuration de périphérique
    /// </summary>
    public async Task AddDeviceConfigurationAsync(DeviceConfiguration config)
    {
        try
        {
            var settings = _currentSettings ?? await LoadConfigurationAsync();

            config.CreatedDate = DateTime.Now;
            config.ModifiedDate = DateTime.Now;

            settings.AddDeviceConfiguration(config);
            await SaveConfigurationAsync(settings);

            _logger?.LogInformation("Configuration ajoutée pour le périphérique {Device}", config.Device.Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de l'ajout de la configuration du périphérique");
            throw;
        }
    }

    /// <summary>
    /// Met à jour une configuration de périphérique existante
    /// </summary>
    public async Task UpdateDeviceConfigurationAsync(DeviceConfiguration config)
    {
        try
        {
            var settings = _currentSettings ?? await LoadConfigurationAsync();

            config.ModifiedDate = DateTime.Now;

            if (settings.UpdateDeviceConfiguration(config))
            {
                await SaveConfigurationAsync(settings);
                _logger?.LogInformation("Configuration mise à jour pour le périphérique {Device}", config.Device.Name);
            }
            else
            {
                _logger?.LogWarning("Configuration introuvable pour l'ID {Id}", config.Id);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de la mise à jour de la configuration");
            throw;
        }
    }

    /// <summary>
    /// Supprime une configuration de périphérique
    /// </summary>
    public async Task<bool> RemoveDeviceConfigurationAsync(Guid id)
    {
        try
        {
            var settings = _currentSettings ?? await LoadConfigurationAsync();

            if (settings.RemoveDeviceConfiguration(id))
            {
                await SaveConfigurationAsync(settings);
                _logger?.LogInformation("Configuration supprimée pour l'ID {Id}", id);
                return true;
            }

            _logger?.LogWarning("Configuration introuvable pour l'ID {Id}", id);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de la suppression de la configuration");
            throw;
        }
    }

    /// <summary>
    /// Récupère le chemin du fichier de configuration
    /// </summary>
    public string GetConfigFilePath()
    {
        return _configFilePath;
    }

    /// <summary>
    /// Crée une sauvegarde de la configuration actuelle
    /// </summary>
    public async Task CreateBackupAsync()
    {
        try
        {
            if (!File.Exists(_configFilePath))
                return;

            var backupPath = Path.Combine(_configDirectory, "config.json.bak");

            // Copier le fichier de configuration actuel vers la sauvegarde
            await Task.Run(() => File.Copy(_configFilePath, backupPath, overwrite: true));

            _logger?.LogDebug("Sauvegarde créée : {BackupPath}", backupPath);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Impossible de créer une sauvegarde de la configuration");
        }
    }
}
