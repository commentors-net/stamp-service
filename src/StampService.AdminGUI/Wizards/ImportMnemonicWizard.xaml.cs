using System.Windows;
using System.Windows.Controls;
using StampService.ClientLib;
using NBitcoin;
using Nethereum.HdWallet;

namespace StampService.AdminGUI.Wizards;

public partial class ImportMnemonicWizard : Window
{
    private readonly StampServiceClient _client;
    private int _currentStep = 1;
    private string _secretName = string.Empty;
  private string _keyType = string.Empty;
private string _network = string.Empty;
    private string _mnemonicPhrase = string.Empty;
    private string _address = string.Empty;
    private string _privateKey = string.Empty;

    public string SecretName => _secretName;
    public bool ImportSuccessful { get; private set; }

    public ImportMnemonicWizard()
    {
        InitializeComponent();
        _client = new StampServiceClient();
        UpdateStepVisibility();

        // Add event handler to count words as user types
      MnemonicInput.TextChanged += MnemonicInput_TextChanged;
    }

    private void MnemonicInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        var words = MnemonicInput.Text.Trim().Split(new[] { ' ', '\n', '\r', '\t' }, 
   StringSplitOptions.RemoveEmptyEntries);
        WordCountText.Text = $"Word count: {words.Length}";

      if (words.Length == 12)
        {
            WordCountText.Foreground = System.Windows.Media.Brushes.Green;
        }
    else
        {
WordCountText.Foreground = System.Windows.Media.Brushes.Orange;
    }
    }

    private void UpdateStepVisibility()
    {
        Step1Panel.Visibility = _currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
        Step2Panel.Visibility = _currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
        Step3Panel.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;

        BackButton.IsEnabled = _currentStep > 1;
        NextButton.Content = _currentStep == 3 ? "Finish" : "Next ?";

   StepIndicator.Text = $"Step {_currentStep} of 3";
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        switch (_currentStep)
        {
case 1:
                if (!ValidateStep1())
    return;
    _currentStep = 2;
            await ImportMnemonic();
             break;

            case 2:
           _currentStep = 3;
   UpdateSummary();
     break;

    case 3:
                ImportSuccessful = true;
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

  private bool ValidateStep1()
    {
        // Validate secret name
        if (string.IsNullOrWhiteSpace(SecretNameInput.Text))
      {
      MessageBox.Show("Please enter a secret name.", "Validation", 
   MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        // Validate mnemonic
      var mnemonicText = MnemonicInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(mnemonicText))
        {
         MessageBox.Show("Please enter your 12-word mnemonic phrase.", "Validation", 
         MessageBoxButton.OK, MessageBoxImage.Warning);
       return false;
        }

        var words = mnemonicText.Split(new[] { ' ', '\n', '\r', '\t' }, 
 StringSplitOptions.RemoveEmptyEntries);

        if (words.Length != 12)
        {
            MessageBox.Show($"Mnemonic must be exactly 12 words. You entered {words.Length} words.", 
  "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
  }

     // Try to validate mnemonic format
        try
 {
            var testMnemonic = new Mnemonic(string.Join(" ", words), Wordlist.English);
 _mnemonicPhrase = testMnemonic.ToString();
 }
        catch (Exception ex)
        {
 MessageBox.Show($"Invalid mnemonic phrase. Please check that all words are correct.\n\nError: {ex.Message}", 
        "Validation", MessageBoxButton.OK, MessageBoxImage.Error);
     return false;
  }

        _secretName = SecretNameInput.Text.Trim();
        _keyType = ((ComboBoxItem)KeyTypeComboBox.SelectedItem).Content.ToString() ?? "Master Contract Key";
        _network = ((ComboBoxItem)NetworkComboBox.SelectedItem).Content.ToString() ?? "Ethereum Mainnet";

        return true;
    }

    private async Task ImportMnemonic()
    {
    try
   {
      ProgressBar.Visibility = Visibility.Visible;
ProgressBar.IsIndeterminate = true;

            // Step 1: Validate mnemonic
      ValidationStatus.Text = "? Mnemonic validated";
            await Task.Delay(300); // Small delay for UX

            // Step 2: Derive wallet
     WalletStatus.Text = "? Deriving wallet...";
            var wallet = new Wallet(_mnemonicPhrase, "");
      var account = wallet.GetAccount(0);

       _address = account.Address;
       _privateKey = account.PrivateKey;

     AddressText.Text = $"Address: {_address}";
            WalletStatus.Text = "? Wallet derived";
       await Task.Delay(300);

            // Step 3: Store in service
      StorageStatus.Text = "? Storing securely...";

            var keyTypeShort = _keyType.Contains("Proxy") ? "MasterProxy" : "MasterContract";
            var metadata = new Dictionary<string, string>
            {
      ["KeyType"] = keyTypeShort,
["Address"] = _address,
            ["PrivateKey"] = _privateKey,
     ["Network"] = _network,
 ["ImportedAt"] = DateTime.UtcNow.ToString("O"),
                ["ImportSource"] = "AdminGUI-ImportWizard",
    ["Version"] = "1.0.0"
            };

    bool stored = await _client.StoreSecretAsync(_secretName, _mnemonicPhrase, metadata);

 if (!stored)
        {
                throw new Exception("Failed to store secret in service");
            }

     StorageStatus.Text = "? Keys stored securely";

   ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = 100;

 await Task.Delay(500);
    }
     catch (Exception ex)
     {
          ProgressBar.Visibility = Visibility.Collapsed;
      MessageBox.Show($"Error importing mnemonic: {ex.Message}", "Error", 
 MessageBoxButton.OK, MessageBoxImage.Error);
            
         // Go back to step 1
       _currentStep = 1;
 UpdateStepVisibility();
        }
  }

    private void UpdateSummary()
    {
        SummarySecretName.Text = _secretName;
      SummaryKeyType.Text = _keyType;
      SummaryNetwork.Text = _network;
        SummaryAddress.Text = _address;
    }
}
