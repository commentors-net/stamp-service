using System.Windows;
using StampService.AdminGUI.Services;
using MaterialDesignThemes.Wpf;

namespace StampService.AdminGUI.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsManager _settingsManager;

    public SettingsWindow()
    {
        InitializeComponent();
        _settingsManager = SettingsManager.Instance;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settingsManager.Settings;

        // Theme
        ThemeComboBox.SelectedIndex = settings.Theme == "Dark" ? 1 : 0;

      // General
        AutoRefreshCheckBox.IsChecked = settings.AutoRefresh;
        ShowNotificationsCheckBox.IsChecked = settings.ShowNotifications;
        ConfirmDeletionsCheckBox.IsChecked = settings.ConfirmDeletions;
     RefreshIntervalSlider.Value = settings.RefreshInterval;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
 {
        try
  {
         // Update settings
     _settingsManager.UpdateSetting(settings =>
            {
   settings.Theme = ThemeComboBox.SelectedIndex == 0 ? "Light" : "Dark";
     settings.AutoRefresh = AutoRefreshCheckBox.IsChecked == true;
        settings.ShowNotifications = ShowNotificationsCheckBox.IsChecked == true;
         settings.ConfirmDeletions = ConfirmDeletionsCheckBox.IsChecked == true;
            settings.RefreshInterval = (int)RefreshIntervalSlider.Value;
  });

          // Apply theme immediately
            ApplyTheme(_settingsManager.Settings.Theme);

         MessageBox.Show(
     "? Settings saved successfully!\n\n" +
      $"Theme: {_settingsManager.Settings.Theme}\n" +
          $"Auto-refresh: {_settingsManager.Settings.AutoRefresh}\n" +
     $"Refresh interval: {_settingsManager.Settings.RefreshInterval}s\n" +
  $"Settings file: %APPDATA%\\StampService\\settings.json\n\n" +
         "Theme has been applied immediately.\n" +
          "Other settings will take effect on next startup.",
     "Settings Saved",
         MessageBoxButton.OK,
      MessageBoxImage.Information);

            DialogResult = true;
         Close();
 }
     catch (Exception ex)
  {
    MessageBox.Show(
        $"Error saving settings:\n\n{ex.Message}",
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
    "Reset all settings to default values?\n\n" +
            "This will:\n" +
    "• Reset theme to Light\n" +
            "• Reset all checkboxes to default\n" +
  "• Reset refresh interval to 30s\n" +
            "• Save changes immediately\n\n" +
        "Continue?",
     "Confirm Reset",
   MessageBoxButton.YesNo,
    MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
      _settingsManager.ResetToDefaults();
 LoadSettings();
            ApplyTheme("Light");

            MessageBox.Show(
        "? Settings reset to defaults and saved.",
    "Reset Complete",
       MessageBoxButton.OK,
             MessageBoxImage.Information);
        }
    }

    private void ApplyTheme(string themeName)
    {
        try
        {
            var paletteHelper = new PaletteHelper();
     var theme = paletteHelper.GetTheme();

    if (themeName == "Dark")
            {
                theme.SetBaseTheme(BaseTheme.Dark);
  }
            else
{
 theme.SetBaseTheme(BaseTheme.Light);
         }

            paletteHelper.SetTheme(theme);
   }
        catch (Exception ex)
     {
          System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
        }
    }
}
