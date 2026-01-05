using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UsbDeviceTrigger.Core.Services;
using UsbDeviceTrigger.UI.ViewModels;
using UsbDeviceTrigger.UI.Views;

namespace UsbDeviceTrigger.UI;

/// <summary>
/// Logique d'interaction pour App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public App()
    {
        // Gestion globale des exceptions non gérées
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void OnStartup(object sender, StartupEventArgs e)
    {
        // Configurer l'injection de dépendances
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Vérifier si l'application doit démarrer minimisée
        bool startMinimized = e.Args.Contains("--minimized");

        // Créer et afficher la fenêtre principale
        var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        var mainWindow = new MainWindow(viewModel, _serviceProvider);

        if (startMinimized)
        {
            mainWindow.WindowState = WindowState.Minimized;
            mainWindow.Hide(); // Cacher la fenêtre si démarrage minimisé
        }
        else
        {
            mainWindow.Show();
        }

        MainWindow = mainWindow;
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Configuration du logging
        services.AddLogging(configure =>
        {
            configure.AddDebug();
            configure.AddConsole();
            configure.SetMinimumLevel(LogLevel.Information);
        });

        // Enregistrer les services Core
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IUsbMonitorService, UsbMonitorService>();
        services.AddSingleton<ICommandExecutionService, CommandExecutionService>();
        services.AddSingleton<IAutoStartService, AutoStartService>();

        // Enregistrer les ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<DeviceListViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<DeviceSelectionViewModel>();
        services.AddTransient<EventsViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Nettoyer les ressources
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var errorLog = LogError(e.Exception);

        MessageBox.Show(
            $"Une erreur inattendue s'est produite:\n\n{e.Exception.Message}\n\nDétails sauvegardés dans:\n{errorLog}",
            "Erreur - USB Device Trigger",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }

    private string LogError(Exception ex)
    {
        try
        {
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UsbDeviceTrigger", "Logs");
            Directory.CreateDirectory(logDir);

            var logFile = Path.Combine(logDir, $"error_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");

            var errorText = $"=== Erreur {DateTime.Now} ===\n\n" +
                           $"Message: {ex.Message}\n\n" +
                           $"Type: {ex.GetType().FullName}\n\n" +
                           $"Stack Trace:\n{ex.StackTrace}\n\n";

            if (ex.InnerException != null)
            {
                errorText += $"Inner Exception: {ex.InnerException.Message}\n\n" +
                            $"Inner Stack Trace:\n{ex.InnerException.StackTrace}\n";
            }

            File.WriteAllText(logFile, errorText);
            return logFile;
        }
        catch
        {
            return "Impossible de créer le fichier log";
        }
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        if (exception != null)
        {
            var errorLog = LogError(exception);
            MessageBox.Show(
                $"Une erreur fatale s'est produite:\n\n{exception.Message}\n\nDétails dans:\n{errorLog}",
                "Erreur fatale - USB Device Trigger",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
