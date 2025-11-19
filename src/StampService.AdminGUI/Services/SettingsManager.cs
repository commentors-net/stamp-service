using System.IO;
using System.Text.Json;
using StampService.AdminGUI.Models;

namespace StampService.AdminGUI.Services;

/// <summary>
/// Manages application settings persistence
/// </summary>
public class SettingsManager
{
    private static SettingsManager? _instance;
    private static readonly object _lock = new();
    private AppSettings _settings;
    private readonly string _settingsPath;

    public static SettingsManager Instance
    {
        get
   {
      if (_instance == null)
            {
  lock (_lock)
      {
           _instance ??= new SettingsManager();
                }
}
            return _instance;
        }
    }

    public AppSettings Settings => _settings;

    private SettingsManager()
    {
        _settingsPath = GetSettingsPath();
        _settings = LoadSettings();
    }

    private string GetSettingsPath()
    {
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      var stampServiceFolder = Path.Combine(appDataFolder, "StampService");
     
        // Create folder if it doesn't exist
        Directory.CreateDirectory(stampServiceFolder);
        
        return Path.Combine(stampServiceFolder, "settings.json");
  }

    private AppSettings LoadSettings()
    {
      try
        {
            if (File.Exists(_settingsPath))
       {
  var json = File.ReadAllText(_settingsPath);
   var settings = JsonSerializer.Deserialize<AppSettings>(json);
                
                if (settings != null)
  {
                    return settings;
              }
            }
        }
   catch (Exception ex)
        {
            // Log error but continue with defaults
     System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }

        // Return default settings
        return new AppSettings();
    }

    public void SaveSettings()
    {
        try
        {
            _settings.LastSaved = DateTime.UtcNow;
     _settings.Version = "1.0.0";

     var options = new JsonSerializerOptions
            {
     WriteIndented = true
        };

            var json = JsonSerializer.Serialize(_settings, options);
    File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
         throw;
        }
}

    public void UpdateSetting(Action<AppSettings> updateAction)
    {
 updateAction(_settings);
    SaveSettings();
    }

    public void ResetToDefaults()
    {
     _settings = new AppSettings();
        SaveSettings();
    }
}
