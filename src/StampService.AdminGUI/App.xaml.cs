using System.Configuration;
using System.Data;
using System.Windows;
using StampService.AdminGUI.Helpers;

namespace StampService.AdminGUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

#if RELEASE
        // Only check for administrator privileges in Release mode
        // In Debug mode, allow running without elevation for easier development
        if (!AdminHelper.IsAdministrator())
        {
            var result = MessageBox.Show(
      "⚠️ Administrator Privileges Required\n\n" +
        "This application requires administrator privileges to manage the Stamp Service.\n\n" +
     "Would you like to restart with administrator privileges?",
                "Administrator Required",
     MessageBoxButton.YesNo,
     MessageBoxImage.Warning);

         if (result == MessageBoxResult.Yes)
          {
      AdminHelper.RestartAsAdmin();
            }
  else
       {
       Shutdown();
            }
        }
#else
        // Debug mode - show info but continue without admin
        if (!AdminHelper.IsAdministrator())
        {
MessageBox.Show(
        "ℹ️ Debug Mode - Running Without Admin Privileges\n\n" +
        "The application is running in Debug mode without administrator privileges.\n" +
"Some features may not work correctly.\n\n" +
        "In Release mode, administrator privileges will be required.",
           "Debug Mode",
            MessageBoxButton.OK,
 MessageBoxImage.Information);
      }
#endif
    }
}

