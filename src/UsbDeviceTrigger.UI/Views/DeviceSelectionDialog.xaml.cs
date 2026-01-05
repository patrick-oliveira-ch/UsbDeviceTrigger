using System.Windows;

namespace UsbDeviceTrigger.UI.Views;

/// <summary>
/// Logique d'interaction pour DeviceSelectionDialog.xaml
/// </summary>
public partial class DeviceSelectionDialog : Window
{
    public DeviceSelectionDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Ferme le dialogue avec DialogResult = true
    /// </summary>
    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Ferme le dialogue avec DialogResult = false
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
