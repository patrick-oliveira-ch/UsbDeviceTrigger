using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace UsbDeviceTrigger.UI.ViewModels;

/// <summary>
/// ViewModel pour la vue des événements récents
/// </summary>
public partial class EventsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    [ObservableProperty]
    private string _lastEvent = "Aucun événement";

    [ObservableProperty]
    private ObservableCollection<string> _recentEvents = new();

    public EventsViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;

        // Lier aux événements du MainWindowViewModel
        _lastEvent = _mainWindowViewModel.LastEvent;
        _recentEvents = _mainWindowViewModel.RecentEvents;

        // S'abonner aux changements pour garder la vue synchronisée
        _mainWindowViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.LastEvent))
            {
                LastEvent = _mainWindowViewModel.LastEvent;
            }
        };
    }

    [RelayCommand]
    private void CopyEvents()
    {
        try
        {
            if (RecentEvents.Count == 0)
            {
                MessageBox.Show("Aucun événement à copier.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var allEvents = string.Join(Environment.NewLine, RecentEvents);
            Clipboard.SetText(allEvents);

            MessageBox.Show($"{RecentEvents.Count} événement(s) copié(s) dans le presse-papiers!", "Succès",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de la copie:\n{ex.Message}", "Erreur",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
