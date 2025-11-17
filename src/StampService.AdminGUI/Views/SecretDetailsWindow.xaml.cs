using System.Windows;
using System.Windows.Controls;

namespace StampService.AdminGUI.Views;

public partial class SecretDetailsWindow : Window
{
    private readonly SecretItem _secret;
    private bool _isRevealed = false;

    public SecretDetailsWindow(SecretItem secret)
    {
    InitializeComponent();
        _secret = secret;
        LoadDetails();
    }

    private void LoadDetails()
    {
        SecretNameText.Text = _secret.Name;
   TypeText.Text = _secret.Type;
   CreatedAtText.Text = _secret.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
  NetworkText.Text = _secret.Network;
        AddressText.Text = _secret.Address;

        // Load metadata
  MetadataList.Items.Clear();
      foreach (var (key, value) in _secret.Metadata)
  {
     if (key != "PrivateKey") // Don't show private key in metadata
     {
    var item = new ListBoxItem
       {
     Content = $"{key}: {value}",
       FontSize = 12
    };
 MetadataList.Items.Add(item);
      }
        }

   // Hide value initially
        ValueTextBox.Text = new string('*', 50);
    RevealButton.Content = "?? Reveal Secret";
    }

    private void RevealButton_Click(object sender, RoutedEventArgs e)
    {
      if (_isRevealed)
        {
      // Hide
     ValueTextBox.Text = new string('*', 50);
    RevealButton.Content = "?? Reveal Secret";
      _isRevealed = false;
        }
   else
        {
      // Show with confirmation
var result = MessageBox.Show(
       "?? This will display the secret value in plain text.\n\n" +
      "Make sure no one is watching your screen!",
        "Confirm Reveal",
         MessageBoxButton.YesNo,
        MessageBoxImage.Warning);

  if (result == MessageBoxResult.Yes)
 {
    ValueTextBox.Text = _secret.Value;
RevealButton.Content = "?? Hide Secret";
   _isRevealed = true;
     }
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRevealed)
        {
   Clipboard.SetText(_secret.Value);
            MessageBox.Show("Secret copied to clipboard!", "Copied", 
    MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
   MessageBox.Show("Please reveal the secret first before copying.", "Cannot Copy", 
 MessageBoxButton.OK, MessageBoxImage.Information);
      }
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Export functionality coming soon!", "Coming Soon", 
      MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
