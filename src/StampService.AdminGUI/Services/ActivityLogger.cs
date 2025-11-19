using System.Collections.ObjectModel;

namespace StampService.AdminGUI.Services;

/// <summary>
/// Activity logger for tracking user actions in the AdminGUI
/// </summary>
public class ActivityLogger
{
    private static ActivityLogger? _instance;
    private static readonly object _lock = new();
    private readonly ObservableCollection<Activity> _activities = new();
    private readonly int _maxActivities = 100;

    private ActivityLogger()
    {
 }

    public static ActivityLogger Instance
    {
        get
      {
      if (_instance == null)
 {
   lock (_lock)
      {
 _instance ??= new ActivityLogger();
    }
          }
 return _instance;
     }
    }

    /// <summary>
    /// Log a new activity
    /// </summary>
    public void LogActivity(string message, ActivityType type = ActivityType.Info)
    {
 lock (_lock)
        {
            _activities.Insert(0, new Activity
    {
      Message = message,
     Type = type,
       Timestamp = DateTime.Now
            });

         // Keep only the most recent activities
       while (_activities.Count > _maxActivities)
      {
            _activities.RemoveAt(_activities.Count - 1);
       }
 }
    }

    /// <summary>
    /// Get recent activities
    /// </summary>
    public List<Activity> GetRecentActivities(int count = 10)
    {
        lock (_lock)
{
        return _activities.Take(count).ToList();
        }
    }

    /// <summary>
    /// Get all activities
    /// </summary>
    public List<Activity> GetAllActivities()
    {
      lock (_lock)
     {
     return _activities.ToList();
        }
    }

  /// <summary>
    /// Clear all activities
    /// </summary>
    public void Clear()
    {
     lock (_lock)
    {
            _activities.Clear();
        }
    }

    public enum ActivityType
    {
      Info,
        Success,
        Warning,
        Error
    }

    public class Activity
 {
        public string Message { get; set; } = string.Empty;
        public ActivityType Type { get; set; }
        public DateTime Timestamp { get; set; }

        public string TimeAgo
        {
            get
     {
          var timeSpan = DateTime.Now - Timestamp;
       
     if (timeSpan.TotalSeconds < 60)
        return "just now";
     if (timeSpan.TotalMinutes < 60)
         return $"{(int)timeSpan.TotalMinutes}m ago";
          if (timeSpan.TotalHours < 24)
                  return $"{(int)timeSpan.TotalHours}h ago";
    if (timeSpan.TotalDays < 7)
              return $"{(int)timeSpan.TotalDays}d ago";
       
    return Timestamp.ToString("MMM dd");
      }
   }

        public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
