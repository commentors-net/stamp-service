using System.Windows;
using StampService.ClientLib;
using StampService.Core.Models;
using System.Diagnostics;

namespace StampService.AdminGUI.Views;

public partial class ServiceHealthView : Window
{
    private readonly StampServiceClient _client;
    private readonly List<double> _responseTimes = new();

    public ServiceHealthView()
    {
      InitializeComponent();
        _client = new StampServiceClient();
      
  Loaded += ServiceHealthView_Loaded;
    }

    private async void ServiceHealthView_Loaded(object sender, RoutedEventArgs e)
{
  await LoadHealthStatus();
  }

    private async Task LoadHealthStatus()
    {
  try
        {
      var stopwatch = Stopwatch.StartNew();
         var status = await _client.GetStatusAsync();
      stopwatch.Stop();

       // Update status fields
if (status.IsRunning)
   {
       ServiceStatusText.Text = "? Running";
    ServiceStatusText.Foreground = System.Windows.Media.Brushes.Green;
   }
  else
   {
     ServiceStatusText.Text = "? Stopped";
   ServiceStatusText.Foreground = System.Windows.Media.Brushes.Red;
    }

     UptimeText.Text = TimeSpan.FromSeconds(status.UptimeSeconds).ToString(@"dd\d\ hh\h\ mm\m\ ss\s");

      if (status.KeyPresent)
  {
       KeyStatusText.Text = "? Master Key Present";
      KeyStatusText.Foreground = System.Windows.Media.Brushes.Green;
  }
   else
   {
    KeyStatusText.Text = "? No Master Key";
   KeyStatusText.Foreground = System.Windows.Media.Brushes.Red;
    }

         AlgorithmText.Text = status.Algorithm ?? "Unknown";
      LastCheckText.Text = status.LastHealthCheck.ToString("yyyy-MM-dd HH:mm:ss");

       // Record response time
      _responseTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
     if (_responseTimes.Count > 10)
         _responseTimes.RemoveAt(0);

      UpdatePerformanceMetrics();
     }
  catch (Exception ex)
        {
   ServiceStatusText.Text = "? Service Unavailable";
 ServiceStatusText.Foreground = System.Windows.Media.Brushes.Red;
       
      MessageBox.Show(
       $"Cannot connect to service.\n\n" +
 $"Error: {ex.Message}\n\n" +
    "Please ensure the service is running.",
    "Connection Error",
       MessageBoxButton.OK,
      MessageBoxImage.Error);
        }
    }

    private async void TestStampButton_Click(object sender, RoutedEventArgs e)
    {
try
     {
       TestResultsText.Text = "Running test stamp...\n";
    
      var stopwatch = Stopwatch.StartNew();
  var response = await _client.TestStampAsync();
       stopwatch.Stop();

       var isValid = _client.VerifySignature(response);

     TestResultsText.Text += $"\n=== Test Stamp Results ===\n";
  TestResultsText.Text += $"Timestamp: {response.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\n";
       TestResultsText.Text += $"Algorithm: {response.Algorithm}\n";
       TestResultsText.Text += $"Signature Length: {response.Signature.Length} chars\n";
   TestResultsText.Text += $"Response Time: {stopwatch.Elapsed.TotalMilliseconds:F2} ms\n";
       TestResultsText.Text += $"Signature Valid: {(isValid ? "? Yes" : "? No")}\n";
    
      if (isValid)
   {
       TestResultsText.Text += "\n? Service is healthy and signing correctly!";
      }
    else
            {
    TestResultsText.Text += "\n? WARNING: Signature verification failed!";
     }

     // Update metrics
  _responseTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
     if (_responseTimes.Count > 10)
        _responseTimes.RemoveAt(0);
      
    LastSignTimeText.Text = $"{stopwatch.Elapsed.TotalMilliseconds:F2}";
      UpdatePerformanceMetrics();
   }
 catch (Exception ex)
        {
  TestResultsText.Text += $"\n? Error: {ex.Message}";
    }
    }

    private async void TestSignButton_Click(object sender, RoutedEventArgs e)
    {
      try
    {
   TestResultsText.Text = "Running test sign request...\n";
      
       var request = new SignRequest
   {
     Operation = "test-health",
RequesterId = "ServiceHealthMonitor",
           Payload = new Dictionary<string, object>
     {
   ["timestamp"] = DateTime.UtcNow.ToString("O"),
     ["test_data"] = "health_check_payload"
       }
      };

  var stopwatch = Stopwatch.StartNew();
 var response = await _client.SignAsync(request);
  stopwatch.Stop();

   var isValid = _client.VerifySignature(response);

         TestResultsText.Text += $"\n=== Sign Request Results ===\n";
   TestResultsText.Text += $"Operation: {request.Operation}\n";
       TestResultsText.Text += $"Timestamp: {response.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\n";
    TestResultsText.Text += $"Algorithm: {response.Algorithm}\n";
      TestResultsText.Text += $"Response Time: {stopwatch.Elapsed.TotalMilliseconds:F2} ms\n";
     TestResultsText.Text += $"Signature Valid: {(isValid ? "? Yes" : "? No")}\n";
TestResultsText.Text += $"\nSignature (first 64 chars):\n{response.Signature.Substring(0, Math.Min(64, response.Signature.Length))}...\n";
   
if (isValid)
      {
     TestResultsText.Text += "\n? Sign operation successful!";
    }
     else
      {
     TestResultsText.Text += "\n? WARNING: Signature verification failed!";
   }

      // Update metrics
       _responseTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
  if (_responseTimes.Count > 10)
    _responseTimes.RemoveAt(0);
   
      LastSignTimeText.Text = $"{stopwatch.Elapsed.TotalMilliseconds:F2}";
     UpdatePerformanceMetrics();
        }
        catch (Exception ex)
  {
      TestResultsText.Text += $"\n? Error: {ex.Message}";
     }
    }

    private void UpdatePerformanceMetrics()
    {
        if (_responseTimes.Count > 0)
        {
   var average = _responseTimes.Average();
   AvgResponseTimeText.Text = $"{average:F2}";
 }
        else
   {
     AvgResponseTimeText.Text = "N/A";
   }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
  await LoadHealthStatus();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
