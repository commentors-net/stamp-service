using System.Windows;
using System.Configuration;

namespace StampService.AdminGUI.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
  {
    InitializeComponent();
        LoadSettings();
  }

    private void LoadSettings()
    {
        // Load current settings (these would be persisted in a real app)
     ThemeComboBox.SelectedIndex = 0; // Light theme
  AutoRefreshCheckBox.IsChecked = true;
 ShowNotificationsCheckBox.IsChecked = true;
     ConfirmDeletionsCheckBox.IsChecked = true;
  RefreshIntervalSlider.Value = 30;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
   {
    // In a real app, save to app.config or user settings
      // For now, just show confirmation
 
        var theme = ThemeComboBox.SelectedIndex == 0 ? "Light" : "Dark";
   var autoRefresh = AutoRefreshCheckBox.IsChecked == true;
    var showNotifications = ShowNotificationsCheckBox.IsChecked == true;
   var confirmDeletions = ConfirmDeletionsCheckBox.IsChecked == true;
            var refreshInterval = (int)RefreshIntervalSlider.Value;

  MessageBox.Show(
     $"Settings saved successfully!\n\n" +
    $"Theme: {theme}\n" +
   $"Auto-refresh: {autoRefresh}\n" +
    $"Refresh interval: {refreshInterval}s\n" +
      $"Show notifications: {showNotifications}\n" +
         $"Confirm deletions: {confirmDeletions}\n\n" +
         "Note: Some settings require application restart.",
      "Settings Saved",
   MessageBoxButton.OK,
     MessageBoxImage.Information);

      DialogResult = true;
 Close();
  }
        catch (Exception ex)
 {
         MessageBox.Show(
$"Error saving settings: {ex.Message}",
              "Error",
MessageBoxButton.OK,
  MessageBoxImage.Error);
    }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
 DialogResult = false;
        Close();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
  var result = MessageBox.Show(
     "Reset all settings to default values?",
 "Confirm Reset",
   MessageBoxButton.YesNo,
   MessageBoxImage.Question);

     if (result == MessageBoxResult.Yes)
 {
      ThemeComboBox.SelectedIndex = 0;
   AutoRefreshCheckBox.IsChecked = true;
  ShowNotificationsCheckBox.IsChecked = true;
   ConfirmDeletionsCheckBox.IsChecked = true;
 RefreshIntervalSlider.Value = 30;
        
   MessageBox.Show("Settings reset to defaults.", "Reset Complete",
    MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
