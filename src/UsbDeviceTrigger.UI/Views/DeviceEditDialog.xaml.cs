using System.Windows;

namespace UsbDeviceTrigger.UI.Views;

/// <summary>
/// Logique d'interaction pour DeviceEditDialog.xaml
/// </summary>
public partial class DeviceEditDialog : Window
{
    public DeviceEditDialog()
    {
        InitializeComponent();
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
