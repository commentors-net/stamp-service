using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace StampService.AdminGUI.Views;

public partial class AuditLogViewer : Window
{
    private List<LogEntry> _allLogs = new();
    private List<LogEntry> _filteredLogs = new();
    private readonly string _logPath;
    private bool _isInitialized = false;

    public AuditLogViewer()
    {
        InitializeComponent();

        // Determine log path
        _logPath = Path.Combine(
         Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
 "StampService", "Logs");

        FooterTextBlock.Text = $"Log Location: {_logPath}";

        // Hook events after initialization
        EventTypeComboBox.SelectionChanged += FilterChanged;
        DateRangeComboBox.SelectionChanged += FilterChanged;

        Loaded += AuditLogViewer_Loaded;
    }

    private async void AuditLogViewer_Loaded(object sender, RoutedEventArgs e)
    {
        _isInitialized = true;
        await LoadLogs();
    }

    private async Task LoadLogs()
    {
        try
        {
            _allLogs.Clear();

            if (!Directory.Exists(_logPath))
            {
                MessageBox.Show(
                    $"Log directory not found:\n{_logPath}\n\n" +
                            "The service may not have created logs yet.",
                    "No Logs",
                        MessageBoxButton.OK,
                            MessageBoxImage.Information);
                return;
            }

            // Load both audit and service logs
            var logFiles = Directory.GetFiles(_logPath, "*.log")
             .OrderByDescending(f => File.GetLastWriteTime(f))
                     .ToList();

            if (!logFiles.Any())
            {
                SummaryTextBlock.Text = "No log files found";
                return;
            }

            // Parse all log files
            foreach (var logFile in logFiles)
            {
                await ParseLogFile(logFile);
            }

            // Sort by timestamp descending (newest first)
            _allLogs = _allLogs.OrderByDescending(l => l.TimestampDateTime).ToList();

            SummaryTextBlock.Text = $"Loaded {_allLogs.Count} log entries from {logFiles.Count} file(s)";

            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
         $"Error loading logs:\n\n{ex.Message}",
               "Error",
            MessageBoxButton.OK,
                   MessageBoxImage.Error);
        }
    }

    private async Task ParseLogFile(string filePath)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);

            foreach (var line in lines)
            {
                var entry = ParseLogLine(line);
                if (entry != null)
                {
                    _allLogs.Add(entry);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error parsing log file {filePath}: {ex.Message}");
        }
    }

    private LogEntry? ParseLogLine(string line)
    {
        try
        {
            // Parse Serilog format: 2025-01-04 12:34:56.789 [Information] MESSAGE
            var match = Regex.Match(line, @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) \[(\w+)\] (.+)$");

            if (!match.Success)
                return null;

            var timestamp = match.Groups[1].Value;
            var level = match.Groups[2].Value;
            var message = match.Groups[3].Value;

            // Try to extract event type from message
            var eventType = ExtractEventType(message);
            var details = ExtractDetails(message);

            return new LogEntry
            {
                Timestamp = timestamp,
                TimestampDateTime = DateTime.Parse(timestamp),
                Level = level,
                EventType = eventType,
                Details = details,
                RawMessage = message
            };
        }
        catch
        {
            return null;
        }
    }

    private string ExtractEventType(string message)
    {
        if (message.Contains("SIGN_OPERATION:"))
            return "Sign Operation";
        if (message.Contains("SHARE_CREATION:"))
            return "Share Creation";
        if (message.Contains("RECOVERY_EVENT:"))
            return "Recovery Event";
        if (message.Contains("AUTH_FAILURE:"))
            return "Auth Failure";
        if (message.Contains("SECURITY_EVENT:"))
            return "Security Event";
        if (message.Contains("StampService"))
            return "Service Event";

        return "General";
    }

    private string ExtractDetails(string message)
    {
        // Remove the event type prefix if present
        var prefixes = new[] { "SIGN_OPERATION:", "SHARE_CREATION:", "RECOVERY_EVENT:",
"AUTH_FAILURE:", "SECURITY_EVENT:" };

        foreach (var prefix in prefixes)
        {
            if (message.Contains(prefix))
            {
                var index = message.IndexOf(prefix) + prefix.Length;
                return message.Substring(index).Trim();
            }
        }

        return message;
    }

    private void ApplyFilters()
    {
        // Don't apply filters if not initialized or controls aren't loaded yet
        if (!_isInitialized || EventTypeComboBox == null || DateRangeComboBox == null || SearchTextBox == null || LogDataGrid == null)
            return;

        _filteredLogs = _allLogs.ToList();

        // Apply event type filter
        var eventType = (EventTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        if (!string.IsNullOrEmpty(eventType) && eventType != "All Events")
        {
            _filteredLogs = _filteredLogs.Where(l => l.EventType.Contains(eventType.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Apply date range filter
        var dateRange = (DateRangeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        var cutoffDate = DateTime.Now;

        switch (dateRange)
        {
            case "Today":
                cutoffDate = DateTime.Today;
                break;
            case "Last 7 Days":
                cutoffDate = DateTime.Now.AddDays(-7);
                break;
            case "Last 30 Days":
                cutoffDate = DateTime.Now.AddDays(-30);
                break;
            case "All Time":
                cutoffDate = DateTime.MinValue;
                break;
        }

        if (cutoffDate > DateTime.MinValue)
        {
            _filteredLogs = _filteredLogs.Where(l => l.TimestampDateTime >= cutoffDate).ToList();
        }

        // Apply search filter
        var searchText = SearchTextBox.Text?.Trim();
        if (!string.IsNullOrEmpty(searchText))
        {
            _filteredLogs = _filteredLogs.Where(l =>
             l.RawMessage.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
               l.EventType.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
               l.Details.Contains(searchText, StringComparison.OrdinalIgnoreCase)
          ).ToList();
        }

        // Update grid
        LogDataGrid.ItemsSource = _filteredLogs;

        // Update summary
        if (SummaryTextBlock != null)
        {
            SummaryTextBlock.Text = $"Showing {_filteredLogs.Count} of {_allLogs.Count} log entries";
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void FilterChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void ApplyFilterButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilters();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadLogs();
    }

    private void OpenLogFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Directory.Exists(_logPath))
            {
                Process.Start("explorer.exe", _logPath);
            }
            else
            {
                MessageBox.Show(
      $"Log directory does not exist:\n{_logPath}",
                "Directory Not Found",
        MessageBoxButton.OK,
         MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
         $"Error opening log folder:\n\n{ex.Message}",
  "Error",
     MessageBoxButton.OK,
      MessageBoxImage.Error);
        }
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Title = "Export Audit Logs",
                Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                FilterIndex = 1,
                FileName = $"audit-log-export-{DateTime.Now:yyyyMMdd-HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                ExportLogs(dialog.FileName, Path.GetExtension(dialog.FileName));

                MessageBox.Show(
                       $"Successfully exported {_filteredLogs.Count} log entries to:\n\n{dialog.FileName}",
               "Export Complete",
                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
      $"Error exporting logs:\n\n{ex.Message}",
     "Export Error",
    MessageBoxButton.OK,
      MessageBoxImage.Error);
        }
    }

    private void ExportLogs(string filePath, string extension)
    {
        switch (extension.ToLowerInvariant())
        {
            case ".csv":
                ExportToCsv(filePath);
                break;
            case ".json":
                ExportToJson(filePath);
                break;
            default:
                ExportToText(filePath);
                break;
        }
    }

    private void ExportToCsv(string filePath)
    {
        using var writer = new StreamWriter(filePath);

        // Header
        writer.WriteLine("Timestamp,Level,Event Type,Details");

        // Data
        foreach (var log in _filteredLogs)
        {
            writer.WriteLine($"\"{log.Timestamp}\",\"{log.Level}\",\"{log.EventType}\",\"{log.Details.Replace("\"", "\"\"")}\"");
        }
    }

    private void ExportToJson(string filePath)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_filteredLogs, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(filePath, json);
    }

    private void ExportToText(string filePath)
    {
        using var writer = new StreamWriter(filePath);

        writer.WriteLine("=".PadRight(80, '='));
        writer.WriteLine("AUDIT LOG EXPORT");
        writer.WriteLine($"Generated: {DateTime.Now}");
        writer.WriteLine($"Total Entries: {_filteredLogs.Count}");
        writer.WriteLine("=".PadRight(80, '='));
        writer.WriteLine();

        foreach (var log in _filteredLogs)
        {
            writer.WriteLine($"[{log.Timestamp}] [{log.Level}] {log.EventType}");
            writer.WriteLine($"{log.Details}");
            writer.WriteLine();
        }
    }

    private void LogDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (LogDataGrid.SelectedItem is LogEntry log)
        {
            var details = $"Timestamp: {log.Timestamp}\n" +
              $"Level: {log.Level}\n" +
     $"Event Type: {log.EventType}\n\n" +
          $"Details:\n{log.RawMessage}";

            MessageBox.Show(details, "Log Entry Details", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // Helper class for log entries
    private class LogEntry
    {
        public string Timestamp { get; set; } = string.Empty;
        public DateTime TimestampDateTime { get; set; }
        public string Level { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string RawMessage { get; set; } = string.Empty;
    }
}
