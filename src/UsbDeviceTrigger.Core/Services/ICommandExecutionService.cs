using UsbDeviceTrigger.Core.Models;

namespace UsbDeviceTrigger.Core.Services;

/// <summary>
/// Résultat de l'exécution d'une commande
/// </summary>
public class CommandExecutionResult
{
    public bool Success { get; set; }
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public TimeSpan ExecutionTime { get; set; }
}

/// <summary>
/// Interface pour le service d'exécution de commandes
/// </summary>
public interface ICommandExecutionService
{
    /// <summary>
    /// Exécute une commande de manière asynchrone
    /// </summary>
    Task<CommandExecutionResult> ExecuteCommandAsync(TriggerCommand command);

    /// <summary>
    /// Valide qu'une commande peut être exécutée
    /// </summary>
    bool ValidateCommand(TriggerCommand command);

    /// <summary>
    /// Teste l'exécution d'une commande (mode dry-run)
    /// </summary>
    Task<CommandExecutionResult> TestCommandAsync(TriggerCommand command);
}
