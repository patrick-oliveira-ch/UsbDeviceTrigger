using System.Windows;
using System.Windows.Controls;

namespace UsbDeviceTrigger.UI.Views;

/// <summary>
/// Logique d'interaction pour EventsView.xaml
/// </summary>
public partial class EventsView : UserControl
{
    public EventsView()
    {
        InitializeComponent();
    }

    private void CopySelectedEvent_Click(object sender, RoutedEventArgs e)
    {
        if (EventsListBox.SelectedItem is string selectedEvent)
        {
            try
            {
                Clipboard.SetText(selectedEvent);
                MessageBox.Show("Événement copié dans le presse-papiers!", "Succès",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la copie:\n{ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
