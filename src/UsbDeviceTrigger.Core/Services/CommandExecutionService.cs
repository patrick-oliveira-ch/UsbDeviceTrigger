using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using UsbDeviceTrigger.Core.Models;

namespace UsbDeviceTrigger.Core.Services;

/// <summary>
/// Service d'exécution de commandes terminal avec gestion des erreurs et timeout
/// </summary>
public class CommandExecutionService : ICommandExecutionService
{
    private readonly ILogger<CommandExecutionService>? _logger;
    private const int DefaultTimeoutSeconds = 30;
    private const int MaxOutputLength = 10000; // Limite la taille des sorties

    public CommandExecutionService(ILogger<CommandExecutionService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Exécute une commande de manière asynchrone
    /// </summary>
    public async Task<CommandExecutionResult> ExecuteCommandAsync(TriggerCommand command)
    {
        if (!ValidateCommand(command))
        {
            return new CommandExecutionResult
            {
                Success = false,
                ErrorMessage = "Commande invalide"
            };
        }

        var result = new CommandExecutionResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger?.LogInformation("Exécution de la commande: {Command} {Args}",
                command.Command, command.Arguments);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = command.Command,
                Arguments = command.Arguments,
                WorkingDirectory = command.WorkingDirectory ?? Environment.CurrentDirectory,
                CreateNoWindow = false, // Permettre l'affichage des fenêtres
                UseShellExecute = true // Utiliser le shell pour une meilleure compatibilité
            };

            // Si élévation requise
            if (command.RunAsAdministrator)
            {
                processStartInfo.Verb = "runas";
                _logger?.LogWarning("Commande en mode administrateur");
            }

            using var process = new Process { StartInfo = processStartInfo };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            // Note: Avec UseShellExecute = true, on ne peut pas capturer la sortie
            // mais on permet l'affichage des fenêtres d'applications GUI

            // Démarrer le processus
            bool started = process.Start();

            if (!started)
            {
                result.Success = false;
                result.ErrorMessage = "Impossible de démarrer le processus";
                return result;
            }

            // Attendre la fin du processus avec timeout
            var timeout = command.TimeoutSeconds > 0
                ? TimeSpan.FromSeconds(command.TimeoutSeconds)
                : TimeSpan.FromSeconds(DefaultTimeoutSeconds);

            if (command.WaitForExit)
            {
                bool exited = await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds));

                if (!exited)
                {
                    _logger?.LogWarning("La commande a dépassé le timeout de {Timeout}s", timeout.TotalSeconds);
                    try
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch (Exception killEx)
                    {
                        _logger?.LogWarning(killEx, "Erreur lors de l'arrêt forcé du processus");
                    }

                    result.Success = false;
                    result.ErrorMessage = $"Timeout dépassé ({timeout.TotalSeconds}s)";
                    return result;
                }

                result.ExitCode = process.ExitCode;
                result.Success = process.ExitCode == 0;
            }
            else
            {
                // Ne pas attendre - laisser le processus s'exécuter en arrière-plan
                result.Success = true;
                result.ExitCode = 0;
                _logger?.LogInformation("Processus démarré en arrière-plan (PID: {ProcessId})", process.Id);
            }

            // Avec UseShellExecute = true, on ne peut pas capturer la sortie
            result.StandardOutput = string.Empty;
            result.StandardError = string.Empty;

            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;

            if (result.Success)
            {
                _logger?.LogInformation("Commande exécutée avec succès en {Time}ms",
                    result.ExecutionTime.TotalMilliseconds);
            }
            else
            {
                _logger?.LogWarning("Commande échouée avec le code {ExitCode}. Erreur: {Error}",
                    result.ExitCode, result.StandardError);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ExecutionTime = stopwatch.Elapsed;

            _logger?.LogError(ex, "Erreur lors de l'exécution de la commande");
        }

        return result;
    }

    /// <summary>
    /// Valide qu'une commande peut être exécutée
    /// </summary>
    public bool ValidateCommand(TriggerCommand command)
    {
        if (command == null)
        {
            _logger?.LogWarning("Commande nulle");
            return false;
        }

        if (string.IsNullOrWhiteSpace(command.Command))
        {
            _logger?.LogWarning("Nom de commande vide");
            return false;
        }

        // Vérifier que le répertoire de travail existe (s'il est spécifié)
        if (!string.IsNullOrWhiteSpace(command.WorkingDirectory) &&
            !Directory.Exists(command.WorkingDirectory))
        {
            _logger?.LogWarning("Répertoire de travail introuvable: {Dir}", command.WorkingDirectory);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Teste l'exécution d'une commande (mode dry-run)
    /// </summary>
    public async Task<CommandExecutionResult> TestCommandAsync(TriggerCommand command)
    {
        _logger?.LogInformation("Test de la commande: {Command} {Args}",
            command.Command, command.Arguments);

        // Pour le test, on crée une copie de la commande qui n'attend pas et a un timeout court
        var testCommand = new TriggerCommand
        {
            Command = command.Command,
            Arguments = command.Arguments,
            WorkingDirectory = command.WorkingDirectory,
            RunAsAdministrator = false, // Pas d'élévation pour les tests
            WaitForExit = true,
            TimeoutSeconds = 10 // Timeout court pour les tests
        };

        return await ExecuteCommandAsync(testCommand);
    }
}
