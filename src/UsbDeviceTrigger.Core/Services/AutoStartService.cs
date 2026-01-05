using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace UsbDeviceTrigger.Core.Services;

/// <summary>
/// Service de gestion du démarrage automatique avec Windows via le registre
/// </summary>
public class AutoStartService : IAutoStartService
{
    private readonly ILogger<AutoStartService>? _logger;
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApplicationName = "UsbDeviceTrigger";

    public AutoStartService(ILogger<AutoStartService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Active le démarrage automatique avec Windows
    /// </summary>
    public bool EnableAutoStart(string applicationPath, bool startMinimized = true)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(applicationPath))
            {
                _logger?.LogError("Chemin de l'application vide");
                return false;
            }

            if (!File.Exists(applicationPath))
            {
                _logger?.LogError("Fichier d'application introuvable: {Path}", applicationPath);
                return false;
            }

            _logger?.LogInformation("Activation du démarrage automatique pour: {Path}", applicationPath);

            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
            if (key == null)
            {
                _logger?.LogError("Impossible d'ouvrir la clé de registre: {Path}", RegistryKeyPath);
                return false;
            }

            // Créer la valeur avec le chemin complet, entre guillemets si nécessaire
            var commandLine = $"\"{applicationPath}\"";
            if (startMinimized)
            {
                commandLine += " --minimized";
            }

            key.SetValue(ApplicationName, commandLine, RegistryValueKind.String);

            _logger?.LogInformation("Démarrage automatique activé avec succès");
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger?.LogError(ex, "Accès refusé au registre. Droits administrateur requis?");
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de l'activation du démarrage automatique");
            return false;
        }
    }

    /// <summary>
    /// Désactive le démarrage automatique avec Windows
    /// </summary>
    public bool DisableAutoStart()
    {
        try
        {
            _logger?.LogInformation("Désactivation du démarrage automatique");

            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
            if (key == null)
            {
                _logger?.LogError("Impossible d'ouvrir la clé de registre: {Path}", RegistryKeyPath);
                return false;
            }

            // Vérifier si la valeur existe avant de la supprimer
            var currentValue = key.GetValue(ApplicationName);
            if (currentValue != null)
            {
                key.DeleteValue(ApplicationName, throwOnMissingValue: false);
                _logger?.LogInformation("Démarrage automatique désactivé avec succès");
                return true;
            }
            else
            {
                _logger?.LogInformation("Le démarrage automatique n'était pas activé");
                return true; // Considéré comme un succès car l'objectif est atteint
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger?.LogError(ex, "Accès refusé au registre. Droits administrateur requis?");
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de la désactivation du démarrage automatique");
            return false;
        }
    }

    /// <summary>
    /// Vérifie si le démarrage automatique est activé
    /// </summary>
    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
            if (key == null)
            {
                _logger?.LogWarning("Impossible d'ouvrir la clé de registre: {Path}", RegistryKeyPath);
                return false;
            }

            var value = key.GetValue(ApplicationName);
            var isEnabled = value != null;

            _logger?.LogDebug("Démarrage automatique: {Status}", isEnabled ? "Activé" : "Désactivé");

            return isEnabled;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de la vérification du démarrage automatique");
            return false;
        }
    }

    /// <summary>
    /// Récupère le chemin de l'application dans le registre
    /// </summary>
    public string? GetAutoStartPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
            if (key == null)
                return null;

            var value = key.GetValue(ApplicationName) as string;

            if (string.IsNullOrEmpty(value))
                return null;

            // Retirer les guillemets et les arguments
            value = value.Trim();
            if (value.StartsWith("\""))
            {
                var endQuote = value.IndexOf("\"", 1);
                if (endQuote > 0)
                {
                    value = value.Substring(1, endQuote - 1);
                }
            }
            else
            {
                // Prendre tout jusqu'au premier espace (si pas de guillemets)
                var spaceIndex = value.IndexOf(" ");
                if (spaceIndex > 0)
                {
                    value = value.Substring(0, spaceIndex);
                }
            }

            return value;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de la récupération du chemin du registre");
            return null;
        }
    }
}
