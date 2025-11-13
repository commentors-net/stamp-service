using System.Windows;
using Microsoft.Win32;
using StampService.ClientLib;
using System.IO;
using Ookii.Dialogs.Wpf;
using System.Text.Json;
using StampService.Core.Models;

namespace StampService.AdminGUI.Views;

public partial class BackupRecoveryView : Window
{
    private readonly StampServiceClient _client;
    private List<string> _recoveryShares = new();
    private int _recoveryThreshold = 3;

    public BackupRecoveryView()
{
      InitializeComponent();
        _client = new StampServiceClient();
    }

  // ===== CREATE BACKUP SECTION =====

    private async void CreateBackupButton_Click(object sender, RoutedEventArgs e)
    {
        try
{
      var totalShares = int.Parse(((System.Windows.Controls.ComboBoxItem)TotalSharesComboBox.SelectedItem).Content.ToString()!);
            var threshold = int.Parse(((System.Windows.Controls.ComboBoxItem)ThresholdComboBox.SelectedItem).Content.ToString()!);

     if (threshold > totalShares)
   {
    MessageBox.Show("Threshold cannot be greater than total shares!",
          "Invalid Configuration", MessageBoxButton.OK, MessageBoxImage.Warning);
return;
            }

            // Confirm action
       var confirmResult = MessageBox.Show(
            $"Create backup with {totalShares} shares (threshold: {threshold})?\n\n" +
      "This will:\n" +
     "• Generate Shamir Secret Shares from the master key\n" +
     "• Save shares to secure files\n" +
         "• You must distribute these shares to trusted custodians\n\n" +
    "Continue?",
       "Confirm Backup Creation",
          MessageBoxButton.YesNo,
    MessageBoxImage.Question);

         if (confirmResult != MessageBoxResult.Yes)
   return;

            // Select output folder using Ookii dialog
            var dialog = new VistaFolderBrowserDialog
  {
        Description = "Select folder to save backup shares",
     UseDescriptionForTitle = true
   };

       if (dialog.ShowDialog() == true)
            {
    CreateBackupProgressBar.Visibility = Visibility.Visible;
        CreateBackupProgressBar.IsIndeterminate = true;
     CreateBackupButton.IsEnabled = false;

      try
         {
  // Call the service to create real shares
      var options = new ShareCreationOptions
    {
    TotalShares = totalShares,
               Threshold = threshold,
       Initiator = Environment.UserName
        };

 // Use reflection to call CreateShares (admin method)
       var shareBundle = await CallServiceMethodAsync<ShareBundle>("CreateShares", options);

  if (shareBundle == null)
         {
    throw new Exception("Service returned null share bundle");
       }

               var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
           var backupFolder = Path.Combine(dialog.SelectedPath, $"StampService_Backup_{timestamp}");
   Directory.CreateDirectory(backupFolder);

        // Save each share to a file
         foreach (var share in shareBundle.Shares)
     {
              var shareFile = Path.Combine(backupFolder, $"share-{share.Index}-of-{share.TotalShares}.json");
     var shareJson = JsonSerializer.Serialize(share, new JsonSerializerOptions { WriteIndented = true });
      await File.WriteAllTextAsync(shareFile, shareJson);
         }

              // Save commitment separately
    var commitmentFile = Path.Combine(backupFolder, "commitment.txt");
          await File.WriteAllTextAsync(commitmentFile, 
    $"Commitment: {shareBundle.Commitment}\n" +
            $"Algorithm: {shareBundle.Algorithm}\n" +
 $"Public Key: {shareBundle.PublicKey}\n" +
      $"Created: {DateTime.UtcNow:O}\n" +
     $"Total Shares: {totalShares}\n" +
         $"Threshold: {threshold}");

           // Save README
  var readmeFile = Path.Combine(backupFolder, "README.txt");
             await File.WriteAllTextAsync(readmeFile,
            "STAMP SERVICE BACKUP SHARES\n" +
      "============================\n\n" +
    $"Created: {DateTime.UtcNow:O}\n" +
       $"Total Shares: {totalShares}\n" +
             $"Threshold Required: {threshold}\n\n" +
            "IMPORTANT INSTRUCTIONS:\n\n" +
              "1. Distribute shares to DIFFERENT trusted custodians\n" +
      "2. Store shares OFFLINE (USB drive, safe, etc.)\n" +
          "3. NEVER store all shares together\n" +
           "4. Each custodian should verify their share\n" +
     "5. Document who has which share\n\n" +
         "RECOVERY:\n" +
          $"You need {threshold} of these {totalShares} shares to recover the key.\n" +
           "Use the 'Recover from Backup' feature in AdminGUI.\n\n" +
  $"Commitment: {shareBundle.Commitment}\n");

    CreateBackupProgressBar.IsIndeterminate = false;
    CreateBackupProgressBar.Value = 100;

           MessageBox.Show(
      $"? Backup created successfully!\n\n" +
      $"Location: {backupFolder}\n\n" +
   $"Files created:\n" +
      $"• {totalShares} share files (share-X-of-{totalShares}.json)\n" +
        $"• commitment.txt (for verification)\n" +
          $"• README.txt (instructions)\n\n" +
          "?? CRITICAL NEXT STEPS:\n" +
     "1. Distribute shares to different people\n" +
             "2. Store shares offline\n" +
          "3. Document who has which share\n" +
   "4. Each custodian should verify their share",
                "Backup Created",
MessageBoxButton.OK,
      MessageBoxImage.Information);

      // Open folder
         System.Diagnostics.Process.Start("explorer.exe", backupFolder);
       }
       catch (Exception ex)
      {
        MessageBox.Show(
       $"Failed to create backup shares.\n\n" +
          $"Error: {ex.Message}\n\n" +
         "Possible causes:\n" +
  "• Service is not running\n" +
               "• No master key present\n" +
 "• Insufficient permissions\n\n" +
   "Please check service status and try again.",
          "Backup Failed",
     MessageBoxButton.OK,
      MessageBoxImage.Error);
         }
     finally
     {
    CreateBackupButton.IsEnabled = true;
  CreateBackupProgressBar.Visibility = Visibility.Collapsed;
     }
    }
        }
        catch (Exception ex)
        {
      CreateBackupButton.IsEnabled = true;
      CreateBackupProgressBar.Visibility = Visibility.Collapsed;
    MessageBox.Show($"Error: {ex.Message}", "Error",
           MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task<T?> CallServiceMethodAsync<T>(string method, object parameters)
    {
        // Use reflection to access the private SendRequestAsync method
        var methodInfo = typeof(StampServiceClient).GetMethod("SendRequestAsync",
      System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
 if (methodInfo == null)
    throw new InvalidOperationException("Could not access service method");

        var task = (Task<string>)methodInfo.Invoke(_client, new object[] { method, parameters })!;
        var response = await task;
   
        return JsonSerializer.Deserialize<T>(response);
    }

    // ===== RECOVER FROM BACKUP SECTION =====

    private void AddShareButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
  {
    Title = "Select Share File(s)",
      Filter = "Share Files (*.json)|*.json|All Files (*.*)|*.*",
         Multiselect = true
   };

if (dialog.ShowDialog() == true)
 {
    foreach (var file in dialog.FileNames)
  {
     if (!_recoveryShares.Contains(file))
           {
    _recoveryShares.Add(file);
   SharesList.Items.Add(Path.GetFileName(file));
     }
  }

     UpdateRecoveryStatus();
  }
    }

    private void RemoveShareButton_Click(object sender, RoutedEventArgs e)
    {
        if (SharesList.SelectedIndex >= 0)
   {
       _recoveryShares.RemoveAt(SharesList.SelectedIndex);
      SharesList.Items.RemoveAt(SharesList.SelectedIndex);
      UpdateRecoveryStatus();
    }
  }

    private void ClearSharesButton_Click(object sender, RoutedEventArgs e)
    {
 _recoveryShares.Clear();
   SharesList.Items.Clear();
   UpdateRecoveryStatus();
    }

    private void UpdateRecoveryStatus()
    {
        RecoveryStatusText.Text = $"Shares provided: {_recoveryShares.Count} / {_recoveryThreshold}";
      
        if (_recoveryShares.Count >= _recoveryThreshold)
  {
 RecoveryStatusText.Foreground = System.Windows.Media.Brushes.Green;
       StartRecoveryButton.IsEnabled = true;
        }
        else
  {
      RecoveryStatusText.Foreground = System.Windows.Media.Brushes.Orange;
            StartRecoveryButton.IsEnabled = false;
   }
    }

    private async void StartRecoveryButton_Click(object sender, RoutedEventArgs e)
    {
var result = MessageBox.Show(
     "?? CRITICAL: Start Key Recovery?\n\n" +
 "This will:\n" +
  "• Read the provided share files\n" +
 "• Reconstruct the master key using Shamir's Secret Sharing\n" +
     "• Replace any existing key in the service\n" +
     "• This action CANNOT be undone\n\n" +
    $"Shares provided: {_recoveryShares.Count}\n" +
            $"Threshold: {_recoveryThreshold}\n\n" +
            "Are you absolutely sure you want to proceed?",
 "?? Confirm Key Recovery",
       MessageBoxButton.YesNo,
       MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
          try
      {
       RecoveryProgressBar.Visibility = Visibility.Visible;
      RecoveryProgressBar.IsIndeterminate = true;
     StartRecoveryButton.IsEnabled = false;

           // Step 1: Start recovery session
     await CallServiceMethodAsync<RecoveryStatus>("RecoverStart", new
{
    threshold = _recoveryThreshold
     });

        await Task.Delay(500); // Small delay

    // Step 2: Load and send each share
       int sharesSent = 0;
      foreach (var shareFilePath in _recoveryShares)
     {
       try
{
    // Read share file
  var shareJson = await File.ReadAllTextAsync(shareFilePath);
         var share = JsonSerializer.Deserialize<Share>(shareJson);

      if (share == null)
     {
 MessageBox.Show(
     $"Invalid share file: {Path.GetFileName(shareFilePath)}\n\n" +
         "The file could not be parsed as a valid share.",
     "Invalid Share",
  MessageBoxButton.OK,
       MessageBoxImage.Warning);
       continue;
     }

  // Send share to service
      var status = await CallServiceMethodAsync<RecoveryStatus>("RecoverProvideShare", share);
         
          sharesSent++;
     
    RecoveryStatusText.Text = $"Processing share {sharesSent} of {_recoveryShares.Count}...";

       if (status != null && status.CanRecover)
        {
    // Enough shares! Key reconstructed
          RecoveryProgressBar.IsIndeterminate = false;
      RecoveryProgressBar.Value = 100;

         MessageBox.Show(
"? Recovery Successful!\n\n" +
       "The master key has been successfully reconstructed from the shares.\n\n" +
             "The service now has the restored key and is ready to use.\n\n" +
        "Next steps:\n" +
        "1. Test the service with a test-stamp operation\n" +
           "2. Verify the public key matches your records\n" +
        "3. Create new backup shares if needed",
     "? Key Recovered",
      MessageBoxButton.OK,
    MessageBoxImage.Information);

      RecoveryProgressBar.Visibility = Visibility.Collapsed;
          _recoveryShares.Clear();
      SharesList.Items.Clear();
    UpdateRecoveryStatus();
             return;
          }
      }
   catch (Exception ex)
            {
    MessageBox.Show(
   $"Error processing share: {Path.GetFileName(shareFilePath)}\n\n" +
   $"Error: {ex.Message}",
   "Share Error",
              MessageBoxButton.OK,
      MessageBoxImage.Warning);
   }
      }

       // If we get here, not enough valid shares were provided
 RecoveryProgressBar.IsIndeterminate = false;
    RecoveryProgressBar.Visibility = Visibility.Collapsed;
     StartRecoveryButton.IsEnabled = true;

    MessageBox.Show(
      $"Recovery incomplete.\n\n" +
        $"Shares processed: {sharesSent}\n" +
        $"Threshold required: {_recoveryThreshold}\n\n" +
           "Please add more valid shares and try again.",
     "Recovery Incomplete",
 MessageBoxButton.OK,
           MessageBoxImage.Warning);
       }
   catch (Exception ex)
   {
      RecoveryProgressBar.Visibility = Visibility.Collapsed;
      StartRecoveryButton.IsEnabled = true;
  
      MessageBox.Show(
     $"Error during recovery:\n\n" +
     $"{ex.Message}\n\n" +
        "Possible causes:\n" +
    "• Service is not running\n" +
        "• Invalid share files\n" +
 "• Shares from different backup sets\n" +
     "• Network connectivity issues\n\n" +
             "Please check and try again.",
      "Recovery Failed",
           MessageBoxButton.OK,
      MessageBoxImage.Error);
  }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
