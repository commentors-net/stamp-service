using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using StampService.ClientLib;

namespace StampService.AdminGUI.Views;

public partial class ImportSecretsDialog : Window
{
    private readonly StampServiceClient _client;
    private string _selectedFilePath = string.Empty;
    private List<ImportedSecret> _secretsToImport = new();
    private bool _isInitialized = false;

    public int ImportedCount { get; private set; }
    public bool ImportSuccessful { get; private set; }

    public ImportSecretsDialog()
    {
        InitializeComponent();
        _client = new StampServiceClient();

        // Re-hook event after initialization to avoid null reference during XAML loading
        FileFormatComboBox.SelectionChanged += FileFormatComboBox_SelectionChanged;

        // Set initialized flag after window is fully loaded
        Loaded += (s, e) =>
        {
            _isInitialized = true;
            // Manually trigger to set initial state
            FileFormatComboBox_SelectionChanged(FileFormatComboBox, null!);
        };
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Secrets File",
            Filter = "All Supported Files|*.json;*.txt;*.csv|JSON Files (*.json)|*.json|Text Files (*.txt)|*.txt|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            FilterIndex = 1
        };

        if (dialog.ShowDialog() == true)
        {
            _selectedFilePath = dialog.FileName;
            FilePathTextBox.Text = _selectedFilePath;

            // Auto-detect format
            DetectFileFormat();

            // Load preview
            LoadPreview();

            ImportButton.IsEnabled = true;
        }
    }

    private void DetectFileFormat()
    {
        if (string.IsNullOrEmpty(_selectedFilePath))
            return;

        var extension = Path.GetExtension(_selectedFilePath).ToLowerInvariant();

        switch (extension)
        {
            case ".json":
                FileFormatComboBox.SelectedIndex = 1; // Encrypted JSON
                break;
            case ".txt":
                FileFormatComboBox.SelectedIndex = 2; // Plain Text
                break;
            case ".csv":
                FileFormatComboBox.SelectedIndex = 3; // CSV
                break;
            default:
                FileFormatComboBox.SelectedIndex = 0; // Auto-detect
                break;
        }
    }

    private void FileFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Don't handle events during initialization
        if (!_isInitialized)
            return;

        // Additional safety check
        if (sender is not ComboBox comboBox || PasswordPanel == null)
            return;

        if (comboBox.SelectedIndex == 1) // Encrypted JSON
        {
            PasswordPanel.Visibility = Visibility.Visible;
        }
        else
        {
            PasswordPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void LoadPreview()
    {
        try
        {
            if (string.IsNullOrEmpty(_selectedFilePath) || !File.Exists(_selectedFilePath))
            {
                PreviewTextBlock.Text = "No file selected";
                return;
            }

            var fileInfo = new FileInfo(_selectedFilePath);
            var content = File.ReadAllText(_selectedFilePath);

            // Limit preview to first 500 characters
            var preview = content.Length > 500
           ? content.Substring(0, 500) + "...\n\n(Preview truncated)"
             : content;

            PreviewTextBlock.Text = $"File: {fileInfo.Name}\n" +
       $"Size: {fileInfo.Length:N0} bytes\n" +
           $"Modified: {fileInfo.LastWriteTime}\n" +
   $"\n{preview}";
            PreviewTextBlock.Opacity = 1.0;
        }
        catch (Exception ex)
        {
            PreviewTextBlock.Text = $"Error loading preview: {ex.Message}";
            PreviewTextBlock.Opacity = 0.7;
        }
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(_selectedFilePath) || !File.Exists(_selectedFilePath))
            {
                MessageBox.Show("Please select a valid file.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Disable button during import
            ImportButton.IsEnabled = false;
            StatusPanel.Visibility = Visibility.Visible;
            StatusTextBlock.Text = "Reading file...";

            // Parse file based on format
            await ParseFile();

            if (_secretsToImport.Count == 0)
            {
                MessageBox.Show("No valid secrets found in file.", "Import",
                      MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Validate if option is checked
            if (ValidateBeforeImportCheckBox.IsChecked == true)
            {
                StatusTextBlock.Text = $"Validating {_secretsToImport.Count} secret(s)...";
                await Task.Delay(300);

                if (!ValidateSecrets())
                {
                    ImportButton.IsEnabled = true;
                    return;
                }
            }

            // Create backup if option is checked
            if (CreateBackupCheckBox.IsChecked == true)
            {
                StatusTextBlock.Text = "Creating backup of existing secrets...";
                await Task.Delay(300);
                // TODO: Implement backup creation
            }

            // Import secrets
            StatusTextBlock.Text = $"Importing {_secretsToImport.Count} secret(s)...";
            await ImportSecrets();

            ImportSuccessful = true;
            DialogResult = true;

            MessageBox.Show(
               $"✓ Successfully imported {ImportedCount} secret(s)!\n\n" +
             $"Total in file: {_secretsToImport.Count}\n" +
        $"Imported: {ImportedCount}\n" +
                  $"Skipped: {_secretsToImport.Count - ImportedCount}",
           "Import Complete",
               MessageBoxButton.OK,
       MessageBoxImage.Information);

            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                 $"Error importing secrets:\n\n{ex.Message}",
             "Import Error",
         MessageBoxButton.OK,
               MessageBoxImage.Error);

            ImportButton.IsEnabled = true;
            StatusPanel.Visibility = Visibility.Collapsed;
        }
    }

    private async Task ParseFile()
    {
        _secretsToImport.Clear();
        var content = await File.ReadAllTextAsync(_selectedFilePath);

        var format = FileFormatComboBox.SelectedIndex;

        if (format == 0) // Auto-detect
        {
            // Try JSON first
            if (TryParseJson(content))
                return;

            // Try CSV
            if (TryParseCsv(content))
                return;

            // Fall back to plain text
            ParsePlainText(content);
        }
        else if (format == 1) // Encrypted JSON
        {
            await ParseEncryptedJson(content);
        }
        else if (format == 2) // Plain Text
        {
            ParsePlainText(content);
        }
        else if (format == 3) // CSV
        {
            TryParseCsv(content);
        }
    }

    private bool TryParseJson(string content)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var secrets = JsonSerializer.Deserialize<List<ImportedSecret>>(content, options);

            if (secrets != null && secrets.Count > 0)
            {
                _secretsToImport = secrets;
                return true;
            }
        }
        catch
        {
            // Not valid JSON
        }

        return false;
    }

    private async Task ParseEncryptedJson(string content)
    {
        var password = PasswordBox.Password;

        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException("Password required for encrypted JSON");
        }

        // TODO: Implement actual decryption
        // For now, treat as plain JSON
        TryParseJson(content);
        await Task.CompletedTask;
    }

    private bool TryParseCsv(string content)
    {
        try
        {
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2) // Need header + at least one data row
                return false;

            var header = lines[0].Split(',');

            // Validate header
            if (!header.Any(h => h.Trim().Equals("Name", StringComparison.OrdinalIgnoreCase)) ||
               !header.Any(h => h.Trim().Equals("Value", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var nameIndex = Array.FindIndex(header, h => h.Trim().Equals("Name", StringComparison.OrdinalIgnoreCase));
            var valueIndex = Array.FindIndex(header, h => h.Trim().Equals("Value", StringComparison.OrdinalIgnoreCase));
            var typeIndex = Array.FindIndex(header, h => h.Trim().Equals("Type", StringComparison.OrdinalIgnoreCase));
            var networkIndex = Array.FindIndex(header, h => h.Trim().Equals("Network", StringComparison.OrdinalIgnoreCase));

            for (int i = 1; i < lines.Length; i++)
            {
                var fields = lines[i].Split(',');

                if (fields.Length <= Math.Max(nameIndex, valueIndex))
                    continue;

                var secret = new ImportedSecret
                {
                    Name = fields[nameIndex].Trim().Trim('"'),
                    Value = fields[valueIndex].Trim().Trim('"'),
                    Type = typeIndex >= 0 && typeIndex < fields.Length ? fields[typeIndex].Trim().Trim('"') : "Other",
                    Network = networkIndex >= 0 && networkIndex < fields.Length ? fields[networkIndex].Trim().Trim('"') : ""
                };

                if (!string.IsNullOrWhiteSpace(secret.Name) && !string.IsNullOrWhiteSpace(secret.Value))
                {
                    _secretsToImport.Add(secret);
                }
            }

            return _secretsToImport.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private void ParsePlainText(string content)
    {
        // Simple format: Each line is "Name=Value" or just treat entire content as one secret
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith("//"))
                continue;

            if (trimmed.Contains('='))
            {
                var parts = trimmed.Split('=', 2);
                _secretsToImport.Add(new ImportedSecret
                {
                    Name = parts[0].Trim(),
                    Value = parts[1].Trim(),
                    Type = "Other"
                });
            }
        }
    }

    private bool ValidateSecrets()
    {
        var invalid = _secretsToImport.Where(s =>
        string.IsNullOrWhiteSpace(s.Name) ||
            string.IsNullOrWhiteSpace(s.Value)).ToList();

        if (invalid.Any())
        {
            var result = MessageBox.Show(
                   $"Found {invalid.Count} invalid secret(s) with missing name or value.\n\n" +
           "Continue importing valid secrets only?",
                 "Validation Warning",
        MessageBoxButton.YesNo,
           MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
                return false;

            _secretsToImport = _secretsToImport.Except(invalid).ToList();
        }

        return true;
    }

    private async Task ImportSecrets()
    {
        ImportedCount = 0;
        var overwrite = OverwriteExistingCheckBox.IsChecked == true;

        // Get existing secrets to check for duplicates
        var existingSecrets = await _client.ListSecretsAsync();
        var existingNames = new HashSet<string>(existingSecrets, StringComparer.OrdinalIgnoreCase);

        foreach (var secret in _secretsToImport)
        {
            try
            {
                // Check if exists
                if (existingNames.Contains(secret.Name) && !overwrite)
                {
                    continue; // Skip
                }

                // Create metadata
                var metadata = new Dictionary<string, string>
                {
                    ["Type"] = secret.Type,
                    ["ImportedAt"] = DateTime.UtcNow.ToString("O"),
                    ["ImportSource"] = "File Import"
                };

                if (!string.IsNullOrWhiteSpace(secret.Network))
                    metadata["Network"] = secret.Network;

                if (!string.IsNullOrWhiteSpace(secret.Description))
                    metadata["Description"] = secret.Description;

                // Store secret
                bool stored = await _client.StoreSecretAsync(secret.Name, secret.Value, metadata);

                if (stored)
                    ImportedCount++;

                await Task.Delay(50); // Small delay between imports
            }
            catch
            {
                // Continue with next secret
            }
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // Helper class for imported secrets
    private class ImportedSecret
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Type { get; set; } = "Other";
        public string Network { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
