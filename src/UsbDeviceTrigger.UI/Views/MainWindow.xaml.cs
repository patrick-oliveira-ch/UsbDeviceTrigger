using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using UsbDeviceTrigger.UI.ViewModels;

namespace UsbDeviceTrigger.UI.Views;

/// <summary>
/// Logique d'interaction pour MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(MainWindowViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        DataContext = _viewModel;

        // Charger la vue par défaut (Événements) au démarrage
        Loaded += (s, e) => LoadDefaultView();
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        // Minimiser vers la barre d'état système
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            // L'icône system tray reste active en arrière-plan
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Demander confirmation avant de quitter
        var result = MessageBox.Show(
            "Voulez-vous vraiment quitter l'application?\nLa surveillance USB sera arrêtée.",
            "Confirmer la fermeture",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            e.Cancel = true;
            return;
        }

        // Nettoyer l'icône system tray
        NotifyIcon?.Dispose();
    }

    private void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
    {
        ShowWindow();
    }

    private void MenuOpen_Click(object sender, RoutedEventArgs e)
    {
        ShowWindow();
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void NavDevices_Click(object sender, RoutedEventArgs e)
    {
        // Charger la vue DeviceList avec son ViewModel
        var viewModel = _serviceProvider.GetRequiredService<DeviceListViewModel>();
        var view = new DeviceListView { DataContext = viewModel };
        LoadView(view);
    }

    private void NavSettings_Click(object sender, RoutedEventArgs e)
    {
        // Charger la vue Settings avec son ViewModel
        var viewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
        var view = new SettingsView { DataContext = viewModel };
        LoadView(view);
    }

    private void NavEvents_Click(object sender, RoutedEventArgs e)
    {
        // Recharger la vue par défaut (événements)
        LoadDefaultView();
    }

    private void LoadView(UserControl view)
    {
        // Remplacer le contenu de la zone ContentArea
        ContentArea.Children.Clear();
        ContentArea.Children.Add(view);
    }

    private void LoadDefaultView()
    {
        // Charger la vue des événements
        var viewModel = _serviceProvider.GetRequiredService<EventsViewModel>();
        var view = new EventsView { DataContext = viewModel };
        LoadView(view);
    }
}
