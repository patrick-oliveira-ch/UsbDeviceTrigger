using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UsbDeviceTrigger.Core.Models;

namespace UsbDeviceTrigger.UI.ViewModels;

/// <summary>
/// ViewModel pour le dialogue d'édition de configuration de périphérique
/// </summary>
public partial class DeviceEditViewModel : ObservableObject
{
    [ObservableProperty]
    private string _deviceName = string.Empty;

    [ObservableProperty]
    private string _onConnectCommand = string.Empty;

    [ObservableProperty]
    private string _onConnectArguments = string.Empty;

    [ObservableProperty]
    private string _onDisconnectCommand = string.Empty;

    [ObservableProperty]
    private string _onDisconnectArguments = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private string _onConnectWindowStyle = "Normal";

    [ObservableProperty]
    private string _onDisconnectWindowStyle = "Normal";

    public List<string> WindowStyles { get; } = new() { "Normal", "Minimized", "Maximized", "Hidden" };

    /// <summary>
    /// Indique si les modifications ont été confirmées
    /// </summary>
    public bool IsConfirmed { get; private set; }

    public DeviceEditViewModel()
    {
    }

    /// <summary>
    /// Initialise le ViewModel avec une configuration existante
    /// </summary>
    public void LoadConfiguration(DeviceConfiguration config)
    {
        DeviceName = config.Device.Name ?? "Périphérique USB";
        OnConnectCommand = config.OnConnectCommand?.Command ?? string.Empty;
        OnConnectArguments = config.OnConnectCommand?.Arguments ?? string.Empty;
        OnDisconnectCommand = config.OnDisconnectCommand?.Command ?? string.Empty;
        OnDisconnectArguments = config.OnDisconnectCommand?.Arguments ?? string.Empty;
        Notes = config.Notes ?? string.Empty;
        IsEnabled = config.IsEnabled;
    }

    /// <summary>
    /// Applique les modifications à une configuration
    /// </summary>
    public void ApplyToConfiguration(DeviceConfiguration config)
    {
        var (connectCmd, connectArgs) = BuildCommand(OnConnectCommand, OnConnectArguments, OnConnectWindowStyle);
        var (disconnectCmd, disconnectArgs) = BuildCommand(OnDisconnectCommand, OnDisconnectArguments, OnDisconnectWindowStyle);

        config.OnConnectCommand = new TriggerCommand
        {
            Command = connectCmd,
            Arguments = connectArgs,
            WaitForExit = false // Ne pas attendre la fin pour ne pas bloquer
        };

        config.OnDisconnectCommand = new TriggerCommand
        {
            Command = disconnectCmd,
            Arguments = disconnectArgs,
            WaitForExit = false
        };

        config.Notes = Notes;
        config.IsEnabled = IsEnabled;
    }

    private (string? command, string? arguments) BuildCommand(string command, string arguments, string windowStyle)
    {
        if (string.IsNullOrWhiteSpace(command))
            return (null, null);

        // Si c'est un fichier .exe, on utilise cmd /c start pour avoir le contrôle sur la fenêtre
        if (command.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            var windowFlag = windowStyle switch
            {
                "Minimized" => "/MIN",
                "Maximized" => "/MAX",
                "Hidden" => "/B", // Background = hidden
                _ => ""
            };

            // Utiliser cmd /c start pour lancer l'exe avec le style de fenêtre
            var startCommand = string.IsNullOrWhiteSpace(windowFlag)
                ? $"start \"\" \"{command}\""
                : $"start {windowFlag} \"\" \"{command}\"";

            if (!string.IsNullOrWhiteSpace(arguments))
            {
                startCommand += $" {arguments}";
            }

            return ("cmd", $"/c {startCommand}");
        }
        else
        {
            // Pour les commandes comme powershell, cmd, etc.
            return (command, arguments);
        }
    }

    [RelayCommand]
    private void BrowseConnectExecutable()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Sélectionner un exécutable",
            Filter = "Exécutables (*.exe)|*.exe|Tous les fichiers (*.*)|*.*",
            DefaultExt = ".exe"
        };

        if (dialog.ShowDialog() == true)
        {
            OnConnectCommand = dialog.FileName;
        }
    }

    [RelayCommand]
    private void BrowseDisconnectExecutable()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Sélectionner un exécutable",
            Filter = "Exécutables (*.exe)|*.exe|Tous les fichiers (*.*)|*.*",
            DefaultExt = ".exe"
        };

        if (dialog.ShowDialog() == true)
        {
            OnDisconnectCommand = dialog.FileName;
        }
    }

    [RelayCommand]
    private void Confirm()
    {
        IsConfirmed = true;
    }

    [RelayCommand]
    private void Cancel()
    {
        IsConfirmed = false;
    }
}
