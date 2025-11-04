using System.Text.Json.Serialization;

namespace StampService.Core.Models;

/// <summary>
/// Request to sign data
/// </summary>
public class SignRequest
{
    [JsonPropertyName("operation")]
    public string Operation { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public Dictionary<string, object> Payload { get; set; } = new();

    [JsonPropertyName("requester_id")]
    public string RequesterId { get; set; } = string.Empty;

    [JsonPropertyName("auth")]
    public AuthInfo? Auth { get; set; }
}

/// <summary>
/// Authentication information
/// </summary>
public class AuthInfo
{
    [JsonPropertyName("client_token")]
    public string ClientToken { get; set; } = string.Empty;
}

/// <summary>
/// Response with signature
/// </summary>
public class SignedResponse
{
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = string.Empty;

    [JsonPropertyName("signed_payload")]
    public string SignedPayload { get; set; } = string.Empty;

    [JsonPropertyName("signer_id")]
    public string SignerId { get; set; } = "StampService-v1";

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("public_key")]
    public string? PublicKey { get; set; }
}

/// <summary>
/// Service status information
/// </summary>
public class ServiceStatus
{
    [JsonPropertyName("is_running")]
    public bool IsRunning { get; set; }

    [JsonPropertyName("key_present")]
    public bool KeyPresent { get; set; }

    [JsonPropertyName("uptime_seconds")]
    public long UptimeSeconds { get; set; }

    [JsonPropertyName("last_health_check")]
    public DateTime LastHealthCheck { get; set; }

    [JsonPropertyName("algorithm")]
    public string? Algorithm { get; set; }

    [JsonPropertyName("public_key")]
    public string? PublicKey { get; set; }
}

/// <summary>
/// Options for creating shares
/// </summary>
public class ShareCreationOptions
{
    [JsonPropertyName("total_shares")]
    public int TotalShares { get; set; } = 5;

    [JsonPropertyName("threshold")]
    public int Threshold { get; set; } = 3;

    [JsonPropertyName("initiator")]
    public string Initiator { get; set; } = string.Empty;
}

/// <summary>
/// A single Shamir share
/// </summary>
public class Share
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;

    [JsonPropertyName("threshold")]
    public int Threshold { get; set; }

    [JsonPropertyName("total_shares")]
    public int TotalShares { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("commitment")]
    public string? Commitment { get; set; }
}

/// <summary>
/// Bundle of shares
/// </summary>
public class ShareBundle
{
    [JsonPropertyName("shares")]
    public List<Share> Shares { get; set; } = new();

    [JsonPropertyName("public_key")]
    public string PublicKey { get; set; } = string.Empty;

    [JsonPropertyName("commitment")]
    public string Commitment { get; set; } = string.Empty;

    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = string.Empty;
}

/// <summary>
/// Result of share verification
/// </summary>
public class ShareVerificationResult
{
    [JsonPropertyName("is_valid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("share_index")]
    public int ShareIndex { get; set; }
}

/// <summary>
/// Recovery operation status
/// </summary>
public class RecoveryStatus
{
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("shares_provided")]
    public int SharesProvided { get; set; }

    [JsonPropertyName("shares_required")]
    public int SharesRequired { get; set; }

    [JsonPropertyName("can_recover")]
    public bool CanRecover { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
