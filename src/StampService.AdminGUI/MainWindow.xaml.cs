using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using StampService.ClientLib;
using StampService.AdminGUI.Wizards;
using StampService.AdminGUI.Views;
using StampService.AdminGUI.Helpers;
using StampService.AdminGUI.Services;

namespace StampService.AdminGUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly StampServiceClient _client;
    private readonly ActivityLogger _activityLogger;

    public MainWindow()
    {
        InitializeComponent();
        _client = new StampServiceClient();
        _activityLogger = ActivityLogger.Instance;
        
        // Register keyboard shortcuts
        RegisterKeyboardShortcuts();
        
        Loaded += MainWindow_Loaded;
    }

    private void RegisterKeyboardShortcuts()
    {
    // F1 - Show help
    this.PreviewKeyDown += (sender, e) =>
        {
      if (e.Key == Key.F1)
            {
       KeyboardShortcuts.ShowHelp(this);
     e.Handled = true;
  }
      // Ctrl+N - Create new token
   else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
       {
      CreateTokenButton_Click(sender, null!);
                e.Handled = true;
            }
            // Ctrl+I - Import existing mnemonic
            else if (e.Key == Key.I && Keyboard.Modifiers == ModifierKeys.Control)
      {
    ImportMnemonicButton_Click(sender, null!);
          e.Handled = true;
         }
            // Ctrl+M - Manage secrets
      else if (e.Key == Key.M && Keyboard.Modifiers == ModifierKeys.Control)
      {
   ManageSecretsButton_Click(sender, null!);
          e.Handled = true;
          }
            // Ctrl+B - Backup & Recovery
            else if (e.Key == Key.B && Keyboard.Modifiers == ModifierKeys.Control)
    {
        BackupButton_Click(sender, null!);
           e.Handled = true;
            }
            // Ctrl+H - Service Health
   else if (e.Key == Key.H && Keyboard.Modifiers == ModifierKeys.Control)
   {
      HealthButton_Click(sender, null!);
       e.Handled = true;
    }
            // Ctrl+L - View logs
      else if (e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.Control)
   {
         ViewLogsButton_Click(sender, null!);
   e.Handled = true;
        }
     // F5 or Ctrl+R - Refresh
      else if (e.Key == Key.F5 || (e.Key == Key.R && Keyboard.Modifiers == ModifierKeys.Control))
  {
    _ = LoadServiceStatus();
       e.Handled = true;
          }
        };
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
    // Show privilege status in footer
        PrivilegeStatusText.Text = AdminHelper.GetPrivilegeStatusMessage();
        
        // Load recent activities
        LoadRecentActivities();
        
        await LoadServiceStatus();
    }

    private async Task LoadServiceStatus()
    {
        try
        {
  var status = await _client.GetStatusAsync();
    
            ServiceStatusText.Text = status.IsRunning ? "● Running" : "● Stopped";
            ServiceStatusText.Foreground = status.IsRunning ? Brushes.Green : Brushes.Red;
            
  UptimeText.Text = $"Uptime: {TimeSpan.FromSeconds(status.UptimeSeconds):dd\\d\\ hh\\h\\ mm\\m}";

            MasterKeyText.Text = status.KeyPresent ? "✓ Master Key Present" : "✗ No Master Key";
         MasterKeyText.Foreground = status.KeyPresent ? Brushes.Green : Brushes.Red;
            
            // Get secret count
 var secrets = await _client.ListSecretsAsync();
            SecretsText.Text = $"Secrets Stored: {secrets.Count}";

            // Log activity
 _activityLogger.LogActivity("Service health check completed");
            LoadRecentActivities();
        }
        catch (Exception ex)
        {
       ServiceStatusText.Text = "● Service Unavailable";
   ServiceStatusText.Foreground = Brushes.Red;
        
            // Better error message
            var errorMessage = $"Cannot connect to Stamp Service.\n\n" +
             $"Error: {ex.Message}\n\n" +
    "Possible causes:\n" +
         "• Service is not running\n" +
   "• Service is not installed\n" +
           "• Insufficient permissions\n\n" +
       "Please ensure the service is installed and running.";
    
            MessageBox.Show(errorMessage, "Service Error",
          MessageBoxButton.OK, MessageBoxImage.Error);
         
            _activityLogger.LogActivity($"Service health check failed: {ex.Message}");
        }
  }

    private void LoadRecentActivities()
    {
   RecentActivityList.Items.Clear();
        
        var activities = _activityLogger.GetRecentActivities(10);
        foreach (var activity in activities)
        {
            AddActivityToList(activity);
        }
}

    private void AddActivityToList(ActivityLogger.Activity activity)
    {
        var listItem = new ListBoxItem
      {
      Content = new StackPanel
          {
      Orientation = Orientation.Horizontal,
   Children =
  {
     new MaterialDesignThemes.Wpf.PackIcon
          {
      Kind = MaterialDesignThemes.Wpf.PackIconKind.Circle,
    Width = 8,
           Height = 8,
      Foreground = Brushes.Green,
         VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
     },
     new TextBlock
       {
            Text = $"{activity.Message} - {activity.TimeAgo}",
    FontSize = 12
     }
        }
            }
        };
  RecentActivityList.Items.Add(listItem);
    }

  private void CreateTokenButton_Click(object sender, RoutedEventArgs e)
    {
        var wizard = new CreateTokenWizard();
        wizard.Owner = this;
 if (wizard.ShowDialog() == true)
        {
   _activityLogger.LogActivity($"Token '{wizard.TokenName}' created successfully");
            LoadRecentActivities();
       _ = LoadServiceStatus(); // Refresh
        }
    }

    private void ImportMnemonicButton_Click(object sender, RoutedEventArgs e)
  {
        var wizard = new Wizards.ImportMnemonicWizard();
        wizard.Owner = this;
        if (wizard.ShowDialog() == true && wizard.ImportSuccessful)
        {
            _activityLogger.LogActivity($"Mnemonic '{wizard.SecretName}' imported successfully");
    LoadRecentActivities();
            _ = LoadServiceStatus(); // Refresh
     }
    }

    private void ManageSecretsButton_Click(object sender, RoutedEventArgs e)
{
 var view = new SecretManagerView();
        view.Owner = this;
      view.ShowDialog();
        LoadRecentActivities(); // Refresh to show any changes
        _ = LoadServiceStatus(); // Refresh
    }

    private void BackupButton_Click(object sender, RoutedEventArgs e)
    {
   var view = new BackupRecoveryView();
        view.Owner = this;
  view.ShowDialog();
        LoadRecentActivities(); // Refresh
    }

 private void HealthButton_Click(object sender, RoutedEventArgs e)
    {
        var view = new ServiceHealthView();
  view.Owner = this;
        view.ShowDialog();
    }

    private void ViewLogsButton_Click(object sender, RoutedEventArgs e)
    {
   var viewer = new AuditLogViewer();
      viewer.Owner = this;
        viewer.ShowDialog();
 }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
{
        var settings = new SettingsWindow();
        settings.Owner = this;
  settings.ShowDialog();
    }
}