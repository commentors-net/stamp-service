using System.Windows;
using System.Windows.Input;

namespace StampService.AdminGUI.Helpers;

/// <summary>
/// Helper class for registering keyboard shortcuts
/// </summary>
public static class KeyboardShortcuts
{
    /// <summary>
    /// Register common keyboard shortcuts for a window
    /// </summary>
    public static void RegisterShortcuts(Window window, Action? onRefresh = null, Action? onSearch = null, Action? onClose = null)
  {
   window.KeyDown += (sender, e) =>
        {
            // Ctrl+R - Refresh
            if (e.Key == Key.R && Keyboard.Modifiers == ModifierKeys.Control)
        {
           onRefresh?.Invoke();
      e.Handled = true;
            }
            
       // Ctrl+F - Focus Search
    else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
onSearch?.Invoke();
        e.Handled = true;
      }
        
            // Escape - Close
  else if (e.Key == Key.Escape)
       {
      onClose?.Invoke();
       e.Handled = true;
      }
   
         // F5 - Refresh
            else if (e.Key == Key.F5)
{
            onRefresh?.Invoke();
 e.Handled = true;
          }
        };
    }

    /// <summary>
    /// Show keyboard shortcuts help dialog
    /// </summary>
    public static void ShowHelp(Window owner)
    {
        var helpText = @"?? Keyboard Shortcuts

GLOBAL:
  F1  Show this help
  F5       Refresh current view
  Ctrl+R     Refresh current view
  Ctrl+F              Focus search box
  Escape            Close current dialog
  Alt+F4  Exit application

DASHBOARD:
  Ctrl+N      Create new token
  Ctrl+M              Manage secrets
  Ctrl+B      Backup & Recovery
  Ctrl+H              Service Health

SECRET MANAGER:
  Ctrl+F     Focus search
  Delete              Delete selected secret
  Ctrl+D     Delete selected secret
  Enter      View details
  F5 / Ctrl+R  Refresh list

DIALOGS:
  Enter         OK / Confirm
  Escape        Cancel / Close
  Tab     Navigate fields

TIP: Hold Ctrl and hover over buttons to see shortcuts!";

        MessageBox.Show(
     helpText,
         "Keyboard Shortcuts",
 MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
