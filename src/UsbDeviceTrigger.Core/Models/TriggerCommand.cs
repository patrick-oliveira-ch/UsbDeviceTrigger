namespace UsbDeviceTrigger.Core.Models;

/// <summary>
/// Représente une commande à exécuter lors d'un événement USB
/// </summary>
public class TriggerCommand
{
    /// <summary>
    /// Identifiant unique de la commande
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Commande à exécuter (ex: "powershell", "cmd", "notepad")
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Arguments de la commande
    /// </summary>
    public string Arguments { get; set; } = string.Empty;

    /// <summary>
    /// Répertoire de travail pour l'exécution (optionnel)
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Indique si la commande nécessite des privilèges administrateur
    /// </summary>
    public bool RunAsAdministrator { get; set; }

    /// <summary>
    /// Indique si le processus doit attendre la fin de l'exécution
    /// </summary>
    public bool WaitForExit { get; set; }

    /// <summary>
    /// Timeout en secondes pour l'exécution (0 = aucun timeout)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Retourne une représentation lisible de la commande
    /// </summary>
    public override string ToString()
    {
        var cmd = $"{Command}";
        if (!string.IsNullOrWhiteSpace(Arguments))
            cmd += $" {Arguments}";
        return cmd;
    }

    /// <summary>
    /// Vérifie si la commande est valide
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Command);
    }
}
