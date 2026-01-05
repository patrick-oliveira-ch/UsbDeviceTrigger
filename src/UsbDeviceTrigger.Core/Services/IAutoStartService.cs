namespace UsbDeviceTrigger.Core.Services;

/// <summary>
/// Interface pour le service de gestion du démarrage automatique de l'application
/// </summary>
public interface IAutoStartService
{
    /// <summary>
    /// Active le démarrage automatique avec Windows
    /// </summary>
    bool EnableAutoStart(string applicationPath, bool startMinimized = true);

    /// <summary>
    /// Désactive le démarrage automatique avec Windows
    /// </summary>
    bool DisableAutoStart();

    /// <summary>
    /// Vérifie si le démarrage automatique est activé
    /// </summary>
    bool IsAutoStartEnabled();

    /// <summary>
    /// Récupère le chemin de l'application dans le registre
    /// </summary>
    string? GetAutoStartPath();
}
