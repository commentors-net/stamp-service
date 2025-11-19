using System.Text.Json.Serialization;

namespace StampService.AdminGUI.Models;

/// <summary>
/// Application settings model
/// </summary>
public class AppSettings
{
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "Light";

    [JsonPropertyName("auto_refresh")]
    public bool AutoRefresh { get; set; } = true;

    [JsonPropertyName("refresh_interval")]
    public int RefreshInterval { get; set; } = 30; // seconds

    [JsonPropertyName("show_notifications")]
    public bool ShowNotifications { get; set; } = true;

    [JsonPropertyName("confirm_deletions")]
    public bool ConfirmDeletions { get; set; } = true;

    [JsonPropertyName("confirm_secret_reveals")]
    public bool ConfirmSecretReveals { get; set; } = false;

 [JsonPropertyName("clipboard_auto_clear_enabled")]
    public bool ClipboardAutoClearEnabled { get; set; } = false;

    [JsonPropertyName("clipboard_auto_clear_delay")]
    public int ClipboardAutoClearDelay { get; set; } = 60; // seconds

    [JsonPropertyName("default_backup_shares")]
    public int DefaultBackupShares { get; set; } = 5;

    [JsonPropertyName("default_backup_threshold")]
    public int DefaultBackupThreshold { get; set; } = 3;

    [JsonPropertyName("default_backup_folder")]
    public string DefaultBackupFolder { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("last_saved")]
    public DateTime LastSaved { get; set; } = DateTime.UtcNow;
}
