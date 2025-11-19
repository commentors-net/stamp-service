using System.Windows;
using StampService.ClientLib;

namespace StampService.AdminGUI.Views;

public partial class AddSecretDialog : Window
{
    private readonly StampServiceClient _client;
  public bool SecretAdded { get; private set; }
    public string AddedSecretName { get; private set; } = string.Empty;

    public AddSecretDialog()
    {
        InitializeComponent();
        _client = new StampServiceClient();
        SecretAdded = false;
    }

  private async void SaveButton_Click(object sender, RoutedEventArgs e)
  {
        // Validate inputs
        var secretName = SecretNameTextBox.Text.Trim();
  var secretValue = SecretValueTextBox.Text.Trim();

        if (string.IsNullOrEmpty(secretName))
        {
            MessageBox.Show("Please enter a secret name.", "Validation Error",
        MessageBoxButton.OK, MessageBoxImage.Warning);
SecretNameTextBox.Focus();
    return;
      }

        if (string.IsNullOrEmpty(secretValue))
        {
    MessageBox.Show("Please enter the secret value.", "Validation Error",
   MessageBoxButton.OK, MessageBoxImage.Warning);
  SecretValueTextBox.Focus();
          return;
        }

        // Check if secret already exists
        try
        {
     var exists = await _client.SecretExistsAsync(secretName);
 if (exists)
            {
     var result = MessageBox.Show(
       $"A secret named '{secretName}' already exists.\n\n" +
    "Do you want to overwrite it?",
  "Secret Exists",
MessageBoxButton.YesNo,
          MessageBoxImage.Question);

       if (result != MessageBoxResult.Yes)
    return;
        }
        }
        catch (Exception ex)
    {
            MessageBox.Show(
         $"Error checking if secret exists:\n\n{ex.Message}",
              "Error",
        MessageBoxButton.OK,
  MessageBoxImage.Error);
   return;
        }

// Prepare metadata
        var metadata = new Dictionary<string, string>();

        var secretType = ((System.Windows.Controls.ComboBoxItem)SecretTypeComboBox.SelectedItem).Content.ToString();
    if (!string.IsNullOrEmpty(secretType))
        {
   metadata["Type"] = secretType;
}

    var network = ((System.Windows.Controls.ComboBoxItem)NetworkComboBox.SelectedItem).Content.ToString();
        if (!string.IsNullOrEmpty(network) && network != "(None)")
     {
   metadata["Network"] = network;
    }

        var description = DescriptionTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(description))
        {
  metadata["Description"] = description;
        }

        metadata["CreatedBy"] = Environment.UserName;
        metadata["CreatedAt"] = DateTime.UtcNow.ToString("O");
        metadata["Source"] = "AdminGUI-ManualEntry";

        // Disable button while saving
      SaveButton.IsEnabled = false;
 SaveButton.Content = "SAVING...";

        try
        {
      // Store the secret
      var stored = await _client.StoreSecretAsync(secretName, secretValue, metadata);

  if (!stored)
 {
      MessageBox.Show(
          "Failed to store secret.\n\n" +
        "The service returned an error. Please check service logs.",
    "Storage Failed",
           MessageBoxButton.OK,
    MessageBoxImage.Error);
  return;
     }

     // Success!
            SecretAdded = true;
         AddedSecretName = secretName;

            MessageBox.Show(
         $"? Secret '{secretName}' stored successfully!\n\n" +
            "The secret has been encrypted with DPAPI and stored securely in the Windows Registry.\n\n" +
   "Next steps:\n" +
        "• View in Secret Manager\n" +
                "• Create backup shares\n" +
         "• Test retrieval",
   "Secret Stored",
      MessageBoxButton.OK,
  MessageBoxImage.Information);

    DialogResult = true;
            Close();
 }
   catch (TimeoutException)
   {
            MessageBox.Show(
    "Service request timed out.\n\n" +
         "The service may be busy or unresponsive. Please try again.",
     "Timeout",
 MessageBoxButton.OK,
             MessageBoxImage.Warning);
        }
catch (System.IO.IOException ex)
        {
        MessageBox.Show(
          "Cannot connect to StampService.\n\n" +
       $"Error: {ex.Message}\n\n" +
        "Please ensure the service is running.",
       "Connection Error",
    MessageBoxButton.OK,
            MessageBoxImage.Error);
      }
  catch (Exception ex)
        {
            MessageBox.Show(
      $"Error storing secret:\n\n{ex.Message}",
       "Error",
         MessageBoxButton.OK,
     MessageBoxImage.Error);
        }
 finally
        {
        SaveButton.IsEnabled = true;
  SaveButton.Content = "SAVE SECRET";
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
   Close();
    }
}
