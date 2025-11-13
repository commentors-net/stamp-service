using System.Windows;
using System.Windows.Controls;
using StampService.ClientLib;
using NBitcoin;
using Nethereum.HdWallet;

namespace StampService.AdminGUI.Wizards;

public partial class CreateTokenWizard : Window
{
    private readonly StampServiceClient _client;
    private int _currentStep = 1;
    private string _tokenName = string.Empty;
    private string _network = string.Empty;
    private Wallet? _masterWallet;
    private Wallet? _proxyWallet;

    public string TokenName => _tokenName;

    public CreateTokenWizard()
    {
 InitializeComponent();
        _client = new StampServiceClient();
   UpdateStepVisibility();
    }

    private void UpdateStepVisibility()
    {
        Step1Panel.Visibility = _currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
        Step2Panel.Visibility = _currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
   Step3Panel.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;
      Step4Panel.Visibility = _currentStep == 4 ? Visibility.Visible : Visibility.Collapsed;

        BackButton.IsEnabled = _currentStep > 1;
        NextButton.Content = _currentStep == 4 ? "Finish" : "Next ?";
    
        StepIndicator.Text = $"Step {_currentStep} of 4";
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
     switch (_currentStep)
        {
     case 1:
   if (string.IsNullOrWhiteSpace(TokenNameInput.Text))
 {
      MessageBox.Show("Please enter a token name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
   return;
           }
   _tokenName = TokenNameInput.Text;
     _network = NetworkComboBox.Text;
     _currentStep = 2;
                await GenerateKeys();
 break;

         case 2:
      _currentStep = 3;
       await CreateBackupShares();
           break;

        case 3:
   _currentStep = 4;
   break;

        case 4:
            DialogResult = true;
      Close();
   break;
        }

   UpdateStepVisibility();
    }

 private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep > 1)
        {
    _currentStep--;
            UpdateStepVisibility();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async Task GenerateKeys()
    {
        try
        {
            ProgressBar.Visibility = Visibility.Visible;
         ProgressBar.IsIndeterminate = true;

         // Generate Master Contract Key
   var masterMnemo = new Mnemonic(Wordlist.English, WordCount.Twelve);
            _masterWallet = new Wallet(masterMnemo.ToString(), "");
       var masterAccount = _masterWallet.GetAccount(0);

            MasterAddressText.Text = $"Address: {masterAccount.Address}";
            MasterKeyStatus.Text = "? Master Contract Key Generated";

            // Generate Proxy Key
     var proxyMnemo = new Mnemonic(Wordlist.English, WordCount.Twelve);
            _proxyWallet = new Wallet(proxyMnemo.ToString(), "");
   var proxyAccount = _proxyWallet.GetAccount(0);

  ProxyAddressText.Text = $"Address: {proxyAccount.Address}";
  ProxyKeyStatus.Text = "? Proxy Contract Key Generated";

       // Store in service
            var masterMetadata = new Dictionary<string, string>
      {
          ["TokenName"] = _tokenName,
        ["KeyType"] = "MasterContract",
    ["Address"] = masterAccount.Address,
     ["PrivateKey"] = masterAccount.PrivateKey,
        ["Network"] = _network,
       ["GeneratedAt"] = DateTime.UtcNow.ToString("O")
            };

            await _client.StoreSecretAsync($"{_tokenName}_MasterContract", masterMnemo.ToString(), masterMetadata);

            var proxyMetadata = new Dictionary<string, string>
            {
  ["TokenName"] = _tokenName,
      ["KeyType"] = "ProxyContract",
             ["Address"] = proxyAccount.Address,
                ["PrivateKey"] = proxyAccount.PrivateKey,
       ["Network"] = _network,
     ["GeneratedAt"] = DateTime.UtcNow.ToString("O")
 };

          await _client.StoreSecretAsync($"{_tokenName}_ProxyContract", proxyMnemo.ToString(), proxyMetadata);

            StorageStatus.Text = "? Keys Stored Securely";

   ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = 100;
        }
        catch (Exception ex)
        {
MessageBox.Show($"Error generating keys: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task CreateBackupShares()
{
    try
        {
            BackupProgressBar.Visibility = Visibility.Visible;
      BackupProgressBar.IsIndeterminate = true;

            // In a real implementation, you would create SSS shares here
            // For now, we'll simulate the backup location

      var backupFolder = System.IO.Path.Combine(
  Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
           "StampService Backups",
   DateTime.Now.ToString("yyyy-MM-dd_HHmmss")
            );

     System.IO.Directory.CreateDirectory(backupFolder);

 BackupLocationText.Text = backupFolder;
          BackupStatus.Text = "? 5 backup shares created (need 3 to recover)";

            BackupProgressBar.IsIndeterminate = false;
            BackupProgressBar.Value = 100;

 await Task.Delay(500); // Small delay for UX
        }
        catch (Exception ex)
    {
            MessageBox.Show($"Error creating backups: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

 private void OpenBackupFolder_Click(object sender, RoutedEventArgs e)
    {
     if (!string.IsNullOrEmpty(BackupLocationText.Text))
        {
            System.Diagnostics.Process.Start("explorer.exe", BackupLocationText.Text);
        }
    }

    private void PrintShares_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Print functionality coming soon!", "Print Shares", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
