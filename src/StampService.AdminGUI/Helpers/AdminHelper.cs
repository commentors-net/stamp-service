using System.Security.Principal;

namespace StampService.AdminGUI.Helpers;

/// <summary>
/// Helper class for checking administrator privileges
/// </summary>
public static class AdminHelper
{
    /// <summary>
    /// Check if the current process is running with administrator privileges
    /// </summary>
    public static bool IsAdministrator()
    {
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if admin privileges are required based on build configuration
    /// </summary>
    /// <returns>True if running in Release mode, False in Debug mode</returns>
    public static bool IsAdminRequired()
    {
#if DEBUG
        return false; // Debug mode - admin not required
#else
        return true;  // Release mode - admin required
#endif
    }

    /// <summary>
    /// Show error message if not running as administrator
    /// </summary>
    public static bool CheckAdminPrivileges()
    {
        if (!IsAdministrator())
        {
            System.Windows.MessageBox.Show(
          "?? Administrator Privileges Required\n\n" +
      "This application requires administrator privileges to manage the Stamp Service.\n\n" +
        "Please right-click the application and select 'Run as administrator'.",
      "Administrator Required",
    System.Windows.MessageBoxButton.OK,
       System.Windows.MessageBoxImage.Warning);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Restart the application with administrator privileges
    /// </summary>
    public static void RestartAsAdmin()
    {
        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo
    {
       FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty,
                UseShellExecute = true,
    Verb = "runas" // This triggers UAC prompt
            };

   System.Diagnostics.Process.Start(processInfo);
            System.Windows.Application.Current.Shutdown();
        }
        catch
        {
        // User cancelled UAC prompt or other error
   }
    }

    /// <summary>
    /// Get a user-friendly message about the current privilege state
    /// </summary>
    public static string GetPrivilegeStatusMessage()
    {
    var isAdmin = IsAdministrator();
  var isRequired = IsAdminRequired();

#if DEBUG
    if (isAdmin)
        {
return "? Debug Mode - Running with admin privileges (optional)";
        }
  else
 {
            return "?? Debug Mode - Running without admin privileges (some features may not work)";
     }
#else
      if (isAdmin)
 {
         return "? Running with administrator privileges";
     }
  else
        {
 return "?? Administrator privileges required but not present!";
      }
#endif
 }
}
