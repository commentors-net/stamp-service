namespace StampService.Core.Interfaces;

/// <summary>
/// Interface for audit logging
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Log a sign operation
    /// </summary>
    void LogSignOperation(string operation, string requesterId, string payloadHash, string signatureId);

    /// <summary>
    /// Log a share creation event
    /// </summary>
    void LogShareCreation(int totalShares, int threshold, string initiator);

    /// <summary>
    /// Log a recovery event
    /// </summary>
    void LogRecoveryEvent(string eventType, string details);

    /// <summary>
    /// Log an authentication failure
    /// </summary>
    void LogAuthFailure(string requesterId, string reason);

    /// <summary>
    /// Log a general security event
    /// </summary>
    void LogSecurityEvent(string eventType, string details);
}
