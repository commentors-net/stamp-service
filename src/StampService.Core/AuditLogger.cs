using Serilog;
using StampService.Core.Interfaces;
using System.Text.Json;

namespace StampService.Core;

/// <summary>
/// Audit logger for security events and operations
/// </summary>
public class AuditLogger : IAuditLogger
{
    private readonly ILogger _logger;
    private readonly object _lockObj = new();

    public AuditLogger(string logFilePath)
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}",
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .WriteTo.Console()
            .CreateLogger();
    }

    public void LogSignOperation(string operation, string requesterId, string payloadHash, string signatureId)
    {
        lock (_lockObj)
        {
            var logEntry = new
            {
                EventType = "SignOperation",
                Timestamp = DateTime.UtcNow,
                Operation = operation,
                RequesterId = requesterId,
                PayloadHash = payloadHash,
                SignatureId = signatureId
            };

            _logger.Information("SIGN_OPERATION: {LogEntry}", JsonSerializer.Serialize(logEntry));
        }
    }

    public void LogShareCreation(int totalShares, int threshold, string initiator)
    {
        lock (_lockObj)
        {
            var logEntry = new
            {
                EventType = "ShareCreation",
                Timestamp = DateTime.UtcNow,
                TotalShares = totalShares,
                Threshold = threshold,
                Initiator = initiator
            };

            _logger.Warning("SHARE_CREATION: {LogEntry}", JsonSerializer.Serialize(logEntry));
        }
    }

    public void LogRecoveryEvent(string eventType, string details)
    {
        lock (_lockObj)
        {
            var logEntry = new
            {
                EventType = "Recovery",
                RecoveryEventType = eventType,
                Timestamp = DateTime.UtcNow,
                Details = details
            };

            _logger.Warning("RECOVERY_EVENT: {LogEntry}", JsonSerializer.Serialize(logEntry));
        }
    }

    public void LogAuthFailure(string requesterId, string reason)
    {
        lock (_lockObj)
        {
            var logEntry = new
            {
                EventType = "AuthFailure",
                Timestamp = DateTime.UtcNow,
                RequesterId = requesterId,
                Reason = reason
            };

            _logger.Warning("AUTH_FAILURE: {LogEntry}", JsonSerializer.Serialize(logEntry));
        }
    }

    public void LogSecurityEvent(string eventType, string details)
    {
        lock (_lockObj)
        {
            var logEntry = new
            {
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                Details = details
            };

            _logger.Information("SECURITY_EVENT: {LogEntry}", JsonSerializer.Serialize(logEntry));
        }
    }
}
