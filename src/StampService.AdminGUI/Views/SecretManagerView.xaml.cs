using System.Windows;
using System.Windows.Controls;
using StampService.ClientLib;
using System.Collections.ObjectModel;
using System.Windows.Input;
using StampService.AdminGUI.Helpers;

namespace StampService.AdminGUI.Views;

public partial class SecretManagerView : Window
{
    private readonly StampServiceClient _client;
    private ObservableCollection<SecretItem> _secrets;

    public SecretManagerView()
    {
        InitializeComponent();
        _client = new StampServiceClient();
        _secrets = new ObservableCollection<SecretItem>();
        SecretsDataGrid.ItemsSource = _secrets;

        // Register keyboard shortcuts
        RegisterKeyboardShortcuts();

        Loaded += SecretManagerView_Loaded;
    }

    private void RegisterKeyboardShortcuts()
    {
        KeyboardShortcuts.RegisterShortcuts(
  this,
     onRefresh: async () => await LoadSecrets(),
      onSearch: () => SearchBox.Focus(),
     onClose: () => Close());

        // Additional shortcuts for this view
        PreviewKeyDown += (sender, e) =>
               {
                   // F1 - Show help
                   if (e.Key == Key.F1)
                   {
                       KeyboardShortcuts.ShowHelp(this);
                       e.Handled = true;
                   }
                   // Delete or Ctrl+D - Delete selected
                   else if ((e.Key == Key.Delete || (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control))
        && SecretsDataGrid.SelectedItem != null)
                   {
                       DeleteButton_Click(sender, null!);
                       e.Handled = true;
                   }
                   // Enter - View details
                   else if (e.Key == Key.Enter && SecretsDataGrid.SelectedItem != null)
                   {
                       ViewDetailsButton_Click(sender, null!);
                       e.Handled = true;
                   }
               };
    }

    private async void SecretManagerView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadSecrets();
    }

    private async Task LoadSecrets()
    {
        try
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            _secrets.Clear();

            var secretNames = await _client.ListSecretsAsync();

            foreach (var name in secretNames)
            {
                var result = await _client.RetrieveSecretAsync(name);
                if (result.HasValue)
                {
                    var (value, createdAt, metadata) = result.Value;

                    var secretItem = new SecretItem
                    {
                        Name = name,
                        Type = metadata.GetValueOrDefault("KeyType", "Unknown"),
                        CreatedAt = createdAt,
                        Address = metadata.GetValueOrDefault("Address", "N/A"),
                        Network = metadata.GetValueOrDefault("Network", "N/A"),
                        Value = value,
                        Metadata = metadata
                    };

                    _secrets.Add(secretItem);
                }
            }

            SecretCountText.Text = $"{_secrets.Count} secrets found";
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;

            // Better error message
            var errorMessage = $"Error loading secrets from service.\n\n" +
                 $"Error: {ex.Message}\n\n" +
               "Possible causes:\n" +
                     "• Service is not responding\n" +
              "• Insufficient permissions\n" +
                "• Service is restarting\n\n" +
             "Please check service status and try again.";

            MessageBox.Show(errorMessage, "Error Loading Secrets",
        MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = SearchBox.Text.ToLower();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            SecretsDataGrid.Items.Filter = null;
        }
        else
        {
            SecretsDataGrid.Items.Filter = item =>
           {
               if (item is SecretItem secret)
               {
                   return secret.Name.ToLower().Contains(searchText) ||
               secret.Type.ToLower().Contains(searchText) ||
              secret.Address.ToLower().Contains(searchText) ||
                secret.Network.ToLower().Contains(searchText);
               }
               return false;
           };
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadSecrets();
    }

    private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        if (SecretsDataGrid.SelectedItem is SecretItem secret)
        {
            var detailsWindow = new SecretDetailsWindow(secret);
            detailsWindow.Owner = this;
            detailsWindow.ShowDialog();
        }
        else
        {
            MessageBox.Show("Please select a secret first.", "Selection Required",
             MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (SecretsDataGrid.SelectedItem is SecretItem secret)
        {
            var result = MessageBox.Show(
            $"Are you sure you want to delete '{secret.Name}'?\n\n" +
           "?? This action cannot be undone!",
            "Confirm Deletion",
           MessageBoxButton.YesNo,
                     MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var deleted = await _client.DeleteSecretAsync(secret.Name);
                    if (deleted)
                    {
                        MessageBox.Show($"Secret '{secret.Name}' deleted successfully.",
                           "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadSecrets();
                    }
                    else
                    {
                        MessageBox.Show($"Failed to delete secret '{secret.Name}'.\n\nThe secret may not exist or the service returned an error.",
                      "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Error deleting secret '{secret.Name}'.\n\n" +
                         $"Error: {ex.Message}\n\n" +
                     "Please check service status and try again.";

                    MessageBox.Show(errorMessage, "Error",
               MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("Please select a secret first.", "Selection Required",
            MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void DeleteAndRecreateButton_Click(object sender, RoutedEventArgs e)
    {
        if (SecretsDataGrid.SelectedItem is SecretItem secret)
        {
            var result = MessageBox.Show(
                $"Delete and Recreate '{secret.Name}'?\n\n" +
            "This will:\n" +
                 "• Delete the current secret\n" +
                 "• Generate a new key/mnemonic\n" +
               "• Store with updated metadata\n\n" +
               "?? The old value will be permanently lost!",
           "Confirm Delete & Recreate",
           MessageBoxButton.YesNo,
             MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    // Delete the old secret
                    var deleted = await _client.DeleteSecretAsync(secret.Name);
                    if (!deleted)
                    {
                        LoadingOverlay.Visibility = Visibility.Collapsed;
                        MessageBox.Show("Failed to delete secret.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Generate new key based on type
                    string newValue;
                    if (secret.Type.Contains("Contract") || secret.Type.Contains("Mnemonic"))
                    {
                        // Generate new mnemonic
                        var mnemonic = new NBitcoin.Mnemonic(NBitcoin.Wordlist.English, NBitcoin.WordCount.Twelve);
                        newValue = mnemonic.ToString();
                    }
                    else
                    {
                        // For other types, ask user
                        var inputDialog = new InputDialog("Enter new value:", secret.Name);
                        inputDialog.Owner = this;
                        if (inputDialog.ShowDialog() != true || string.IsNullOrEmpty(inputDialog.ResponseText))
                        {
                            LoadingOverlay.Visibility = Visibility.Collapsed;
                            return;
                        }
                        newValue = inputDialog.ResponseText;
                    }

                    // Update metadata with new timestamp
                    var newMetadata = new Dictionary<string, string>(secret.Metadata)
                    {
                        ["GeneratedAt"] = DateTime.UtcNow.ToString("O"),
                        ["RecreatedAt"] = DateTime.UtcNow.ToString("O")
                    };

                    // If it's a mnemonic, update the address and private key
                    if (newValue.Split(' ').Length == 12)
                    {
                        var wallet = new Nethereum.HdWallet.Wallet(newValue, "");
                        var account = wallet.GetAccount(0);
                        newMetadata["Address"] = account.Address;
                        newMetadata["PrivateKey"] = account.PrivateKey;
                    }

                    // Store the new secret
                    var stored = await _client.StoreSecretAsync(secret.Name, newValue, newMetadata);

                    LoadingOverlay.Visibility = Visibility.Collapsed;

                    if (stored)
                    {
                        MessageBox.Show(
                                $"? Secret '{secret.Name}' recreated successfully!\n\n" +
                           "New key generated and stored.",
                        "Success",
                             MessageBoxButton.OK,
                         MessageBoxImage.Information);

                        await LoadSecrets();
                    }
                }
                catch (Exception ex)
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;

                    var errorMessage = $"Error recreating secret '{secret.Name}'.\n\n" +
                     $"Error: {ex.Message}\n\n" +
                 "The secret may have been deleted but not recreated.\n" +
                     "Please check service status.";

                    MessageBox.Show(errorMessage, "Error",
                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("Please select a secret first.", "Selection Required",
            MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void NewSecretButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddSecretDialog();
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            _ = LoadSecrets(); // Refresh list
        }
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ImportSecretsDialog();
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && dialog.ImportSuccessful)
        {
            MessageBox.Show(
         $"Successfully imported {dialog.ImportedCount} secret(s)!",
                    "Import Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

            _ = LoadSecrets(); // Refresh list
        }
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedSecrets = SecretsDataGrid.SelectedItems.Cast<SecretItem>().ToList();

        if (selectedSecrets.Count == 0)
        {
            var result = MessageBox.Show(
         "No secrets selected.\n\n" +
            "Do you want to export ALL secrets?",
          "Export All?",
           MessageBoxButton.YesNoCancel,
         MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                selectedSecrets = _secrets.ToList();
            }
            else
            {
                return;
            }
        }

        var exportDialog = new ExportSecretsDialog
        {
            Owner = this,
            SecretsToExport = selectedSecrets,
            ExportAll = selectedSecrets.Count == _secrets.Count
        };

        if (exportDialog.ShowDialog() == true)
        {
            // Export completed successfully
            // Optionally refresh or show confirmation
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

public class SecretItem
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();

    public string CreatedAtFormatted => CreatedAt.ToString("yyyy-MM-dd HH:mm");
}
