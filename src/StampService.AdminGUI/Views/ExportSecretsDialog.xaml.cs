using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace StampService.AdminGUI.Views;

public partial class ExportSecretsDialog : Window
{
    public List<SecretItem> SecretsToExport { get; set; } = new();
    public bool ExportAll { get; set; }
    private bool _isInitialized = false;

    public ExportSecretsDialog()
    {
        InitializeComponent();
        
        // Re-hook event after initialization to avoid null reference during XAML loading
        FormatComboBox.SelectionChanged += FormatComboBox_SelectionChanged;
     
        // Trigger initial state based on current selection
        Loaded += (s, e) =>
        {
            _isInitialized = true;
            // Manually trigger to set initial state
            FormatComboBox_SelectionChanged(FormatComboBox, null!);
        };
    }

    private void FormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Don't handle events during initialization
        if (!_isInitialized)
            return;

        // Additional safety check
        if (FormatComboBox?.SelectedItem is not ComboBoxItem item ||
            item.Tag is not string tag ||
  PasswordPanel == null ||
        SecurityWarningText == null)
            return;

        // Show password fields only for encrypted format
        PasswordPanel.Visibility = tag == "EncryptedJson" ? Visibility.Visible : Visibility.Collapsed;

        // Update security warning
        if (tag == "EncryptedJson")
        {
            SecurityWarningText.Text = "Encrypted exports protect your secrets with AES-256 encryption. Use a strong password.";
        }
        else
        {
            SecurityWarningText.Text = "⚠️ WARNING: This format stores secrets in PLAIN TEXT! Only use on trusted, secure computers. Anyone with access to the file can read your secrets.";
        }
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate based on format
        if (FormatComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            if (tag == "EncryptedJson")
            {
                // Validate password
                var password = PasswordBox.Password;
                var confirmPassword = ConfirmPasswordBox.Password;

                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Please enter a password for encryption.", "Password Required",
                             MessageBoxButton.OK, MessageBoxImage.Warning);
                    PasswordBox.Focus();
                    return;
                }

                if (password.Length < 8)
                {
                    MessageBox.Show("Password must be at least 8 characters long.", "Weak Password",
                 MessageBoxButton.OK, MessageBoxImage.Warning);
                    PasswordBox.Focus();
                    return;
                }

                if (password != confirmPassword)
                {
                    MessageBox.Show("Passwords do not match.", "Password Mismatch",
                      MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConfirmPasswordBox.Focus();
                    return;
                }
            }
            else
            {
                // Confirm plain text export
                var result = MessageBox.Show(
         "?? WARNING: Plain Text Export\n\n" +
        "You are about to export secrets in PLAIN TEXT format.\n" +
           "This is NOT SECURE and should only be done on trusted computers.\n\n" +
        "Anyone with access to the exported file can read your secrets.\n\n" +
         "Are you absolutely sure you want to continue?",
         "Confirm Insecure Export",
           MessageBoxButton.YesNo,
           MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }
        }

        // Prompt for save location
        var saveDialog = new SaveFileDialog();

        var formatTag = ((ComboBoxItem)FormatComboBox.SelectedItem).Tag as string;
        switch (formatTag)
        {
            case "EncryptedJson":
                saveDialog.Filter = "Encrypted Secrets (*.encrypted)|*.encrypted|All Files (*.*)|*.*";
                saveDialog.DefaultExt = ".encrypted";
                break;
            case "PlainJson":
                saveDialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
                saveDialog.DefaultExt = ".json";
                break;
            case "Csv":
                saveDialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                saveDialog.DefaultExt = ".csv";
                break;
        }

        saveDialog.FileName = $"StampService_Secrets_Export_{DateTime.Now:yyyy-MM-dd_HHmmss}";

        if (saveDialog.ShowDialog() != true)
            return;

        ExportButton.IsEnabled = false;
        ExportButton.Content = "EXPORTING...";

        try
        {
            var filePath = saveDialog.FileName;

            // Perform export based on format
            switch (formatTag)
            {
                case "EncryptedJson":
                    await ExportEncryptedJson(filePath, PasswordBox.Password);
                    break;
                case "PlainJson":
                    await ExportPlainJson(filePath);
                    break;
                case "Csv":
                    await ExportCsv(filePath);
                    break;
            }

            MessageBox.Show(
            $"? Secrets exported successfully!\n\n" +
               $"File: {filePath}\n" +
            $"Format: {((ComboBoxItem)FormatComboBox.SelectedItem).Content}\n" +
            $"Secrets exported: {SecretsToExport.Count}",
              "Export Successful",
                    MessageBoxButton.OK,
              MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
       $"Error exporting secrets:\n\n{ex.Message}",
         "Export Failed",
    MessageBoxButton.OK,
     MessageBoxImage.Error);
        }
        finally
        {
            ExportButton.IsEnabled = true;
            ExportButton.Content = "EXPORT";
        }
    }

    private async Task ExportEncryptedJson(string filePath, string password)
    {
        // Create export data
        var exportData = new
        {
            Version = "1.0",
            ExportedAt = DateTime.UtcNow,
            SecretCount = SecretsToExport.Count,
            IncludeMetadata = IncludeMetadataCheckBox.IsChecked == true,
            IncludeTimestamps = IncludeTimestampsCheckBox.IsChecked == true,
            Secrets = SecretsToExport.Select(s => new
            {
                s.Name,
                s.Type,
                s.Value,
                s.Network,
                s.Address,
                Metadata = IncludeMetadataCheckBox.IsChecked == true ? s.Metadata : null,
                CreatedAt = IncludeTimestampsCheckBox.IsChecked == true ? s.CreatedAt : default(DateTime?)
            })
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
        var plaintext = Encoding.UTF8.GetBytes(json);

        // Encrypt with AES-256
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Derive key from password using PBKDF2
        var salt = RandomNumberGenerator.GetBytes(32);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        aes.Key = pbkdf2.GetBytes(32);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        // Write to file: [Salt][IV][Ciphertext]
        await using var fs = new FileStream(filePath, FileMode.Create);
        await fs.WriteAsync(salt);
        await fs.WriteAsync(aes.IV);
        await fs.WriteAsync(ciphertext);
    }

    private async Task ExportPlainJson(string filePath)
    {
        var exportData = new
        {
            Version = "1.0",
            ExportedAt = DateTime.UtcNow,
            SecretCount = SecretsToExport.Count,
            Warning = "This file contains UNENCRYPTED secrets. Store securely!",
            Secrets = SecretsToExport.Select(s => new
            {
                s.Name,
                s.Type,
                s.Value,
                s.Network,
                s.Address,
                Metadata = IncludeMetadataCheckBox.IsChecked == true ? s.Metadata : null,
                CreatedAt = IncludeTimestampsCheckBox.IsChecked == true ? s.CreatedAt : default(DateTime?)
            })
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    private async Task ExportCsv(string filePath)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Name,Type,Network,Address,Value,CreatedAt");

        // Data
        foreach (var secret in SecretsToExport)
        {
            sb.AppendLine($"\"{secret.Name}\",\"{secret.Type}\",\"{secret.Network}\",\"{secret.Address}\",\"{secret.Value}\",\"{secret.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
        }

        await File.WriteAllTextAsync(filePath, sb.ToString());
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
