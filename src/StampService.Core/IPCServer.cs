using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using StampService.Core.Interfaces;
using StampService.Core.Models;
using System.Security.Cryptography;

namespace StampService.Core;

/// <summary>
/// IPC Server using Named Pipes for secure local communication
/// </summary>
public class IPCServer : IDisposable
{
    private readonly string _pipeName;
    private readonly KeyManager _keyManager;
    private readonly SSSManager _sssManager;
    private readonly IAuditLogger _auditLogger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly List<Task> _clientTasks = new();
    private readonly DateTime _startTime;
    private DateTime _lastHealthCheck;
    
    // Recovery state
    private bool _recoveryMode = false;
    private readonly List<Share> _recoveryShares = new();
    private int _recoveryThreshold = 0;

    public IPCServer(string pipeName, KeyManager keyManager, SSSManager sssManager, IAuditLogger auditLogger)
    {
        _pipeName = pipeName;
        _keyManager = keyManager;
        _sssManager = sssManager;
        _auditLogger = auditLogger;
        _cancellationTokenSource = new CancellationTokenSource();
        _startTime = DateTime.UtcNow;
        _lastHealthCheck = DateTime.UtcNow;
    }

    /// <summary>
    /// Start listening for client connections
    /// </summary>
    public async Task StartAsync()
    {
        _auditLogger.LogSecurityEvent("IPCServerStarted", $"Named Pipe: {_pipeName}");

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var serverPipe = CreateNamedPipeServer();
                
                // Wait for client connection
                await serverPipe.WaitForConnectionAsync(_cancellationTokenSource.Token);
                
                // Handle client in a separate task
                var clientTask = Task.Run(() => HandleClientAsync(serverPipe), _cancellationTokenSource.Token);
                _clientTasks.Add(clientTask);
                
                // Clean up completed tasks
                _clientTasks.RemoveAll(t => t.IsCompleted);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _auditLogger.LogSecurityEvent("IPCServerError", $"Error: {ex.Message}");
                await Task.Delay(1000); // Wait before retry
            }
        }
    }

    private NamedPipeServerStream CreateNamedPipeServer()
    {
        var pipeSecurity = new PipeSecurity();
        
        // Allow local system and administrators
        var sid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
        pipeSecurity.AddAccessRule(new PipeAccessRule(sid, PipeAccessRights.FullControl, AccessControlType.Allow));
        
        var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        pipeSecurity.AddAccessRule(new PipeAccessRule(adminSid, PipeAccessRights.FullControl, AccessControlType.Allow));

        // Allow authenticated users (can be restricted further)
        var usersSid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
        pipeSecurity.AddAccessRule(new PipeAccessRule(usersSid, PipeAccessRights.ReadWrite, AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            _pipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Message,
            PipeOptions.Asynchronous,
            4096,
            4096,
            pipeSecurity);
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipeServer)
    {
        try
        {
            using (pipeServer)
            {
                // Read request
                var request = await ReadMessageAsync(pipeServer);
                
                if (string.IsNullOrEmpty(request))
                    return;

                // Parse and handle request
                var response = await ProcessRequestAsync(request);
                
                // Send response
                await WriteMessageAsync(pipeServer, response);
            }
        }
        catch (Exception ex)
        {
            _auditLogger.LogSecurityEvent("ClientHandlingError", $"Error: {ex.Message}");
        }
    }

    private async Task<string> ReadMessageAsync(NamedPipeServerStream pipe)
    {
        var buffer = new byte[4096];
        using var ms = new MemoryStream();
        
        int bytesRead;
        do
        {
            bytesRead = await pipe.ReadAsync(buffer, 0, buffer.Length);
            ms.Write(buffer, 0, bytesRead);
        } while (!pipe.IsMessageComplete);

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private async Task WriteMessageAsync(NamedPipeServerStream pipe, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await pipe.WriteAsync(bytes, 0, bytes.Length);
        await pipe.FlushAsync();
    }

    private async Task<string> ProcessRequestAsync(string requestJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(requestJson);
            var root = doc.RootElement;
            
            var method = root.GetProperty("method").GetString();
            var paramsElement = root.TryGetProperty("params", out var p) ? p : default;

            return method switch
            {
                "Sign" => HandleSign(paramsElement),
                "TestStamp" => HandleTestStamp(),
                "GetStatus" => HandleGetStatus(),
                "CreateShares" => HandleCreateShares(paramsElement),
                "VerifyShare" => HandleVerifyShare(paramsElement),
                "RecoverStart" => HandleRecoverStart(paramsElement),
                "RecoverProvideShare" => HandleRecoverProvideShare(paramsElement),
                "RecoverStatus" => HandleRecoverStatus(),
                "DeleteKey" => HandleDeleteKey(paramsElement),
                _ => JsonSerializer.Serialize(new { error = "Unknown method" })
            };
        }
        catch (Exception ex)
        {
            _auditLogger.LogSecurityEvent("RequestProcessingError", $"Error: {ex.Message}");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private string HandleSign(JsonElement paramsElement)
    {
        var signRequest = JsonSerializer.Deserialize<SignRequest>(paramsElement.GetRawText());
        
        if (signRequest == null || !_keyManager.HasKey)
            return JsonSerializer.Serialize(new { error = "No key loaded or invalid request" });

        // Serialize payload and compute hash
        var payloadJson = JsonSerializer.Serialize(signRequest.Payload);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        
        string payloadHash;
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(payloadBytes);
            payloadHash = Convert.ToBase64String(hash);
        }

        // Sign the payload
        var signature = _keyManager.Sign(payloadBytes);
        var signatureBase64 = Convert.ToBase64String(signature);

        // Log the operation
        _auditLogger.LogSignOperation(
            signRequest.Operation,
            signRequest.RequesterId,
            payloadHash,
            signatureBase64.Substring(0, Math.Min(16, signatureBase64.Length)));

        var response = new SignedResponse
        {
            Signature = signatureBase64,
            Algorithm = _keyManager.Algorithm,
            SignedPayload = Convert.ToBase64String(payloadBytes),
            SignerId = "StampService-v1",
            Timestamp = DateTime.UtcNow,
            PublicKey = _keyManager.GetPublicKeyPem()
        };

        return JsonSerializer.Serialize(response);
    }

    private string HandleTestStamp()
    {
        if (!_keyManager.HasKey)
            return JsonSerializer.Serialize(new { error = "No key loaded" });

        _lastHealthCheck = DateTime.UtcNow;

        var testMessage = $"StampService Health Check - {DateTime.UtcNow:O}";
        var messageBytes = Encoding.UTF8.GetBytes(testMessage);
        
        var signature = _keyManager.Sign(messageBytes);
        
        _auditLogger.LogSecurityEvent("TestStamp", "Health check performed");

        var response = new SignedResponse
        {
            Signature = Convert.ToBase64String(signature),
            Algorithm = _keyManager.Algorithm,
            SignedPayload = Convert.ToBase64String(messageBytes),
            SignerId = "StampService-v1",
            Timestamp = DateTime.UtcNow,
            PublicKey = _keyManager.GetPublicKeyPem()
        };

        return JsonSerializer.Serialize(response);
    }

    private string HandleGetStatus()
    {
        var status = new ServiceStatus
        {
            IsRunning = true,
            KeyPresent = _keyManager.HasKey,
            UptimeSeconds = (long)(DateTime.UtcNow - _startTime).TotalSeconds,
            LastHealthCheck = _lastHealthCheck,
            Algorithm = _keyManager.HasKey ? _keyManager.Algorithm : null,
            PublicKey = _keyManager.HasKey ? _keyManager.GetPublicKeyPem() : null
        };

        return JsonSerializer.Serialize(status);
    }

    private string HandleCreateShares(JsonElement paramsElement)
    {
        if (!_keyManager.HasKey)
            return JsonSerializer.Serialize(new { error = "No key loaded" });

        var options = JsonSerializer.Deserialize<ShareCreationOptions>(paramsElement.GetRawText());
        
        if (options == null)
            return JsonSerializer.Serialize(new { error = "Invalid options" });

        _auditLogger.LogShareCreation(options.TotalShares, options.Threshold, options.Initiator);

        // Export private key for SSS (sensitive operation)
        var privateKey = _keyManager.ExportPrivateKeyForSSS();
        
        try
        {
            var shareBundle = _sssManager.CreateShares(
                privateKey,
                options.TotalShares,
                options.Threshold,
                _keyManager.GetPublicKeyPem(),
                _keyManager.Algorithm);

            return JsonSerializer.Serialize(shareBundle);
        }
        finally
        {
            // Always clear the private key
            Array.Clear(privateKey, 0, privateKey.Length);
        }
    }

    private string HandleVerifyShare(JsonElement paramsElement)
    {
        var share = JsonSerializer.Deserialize<Share>(paramsElement.GetRawText());
        var commitment = paramsElement.GetProperty("commitment").GetString();
        
        if (share == null || string.IsNullOrEmpty(commitment))
            return JsonSerializer.Serialize(new { error = "Invalid share or commitment" });

        var result = _sssManager.VerifyShare(share, commitment);
        return JsonSerializer.Serialize(result);
    }

    private string HandleRecoverStart(JsonElement paramsElement)
    {
        if (_keyManager.HasKey)
            return JsonSerializer.Serialize(new { error = "Key already exists. Cannot start recovery." });

        var threshold = paramsElement.GetProperty("threshold").GetInt32();
        
        _recoveryMode = true;
        _recoveryShares.Clear();
        _recoveryThreshold = threshold;

        _auditLogger.LogRecoveryEvent("RecoveryStarted", $"Threshold: {threshold}");

        return JsonSerializer.Serialize(new RecoveryStatus
        {
            IsActive = true,
            SharesProvided = 0,
            SharesRequired = threshold,
            CanRecover = false,
            Message = "Recovery mode activated. Provide shares."
        });
    }

    private string HandleRecoverProvideShare(JsonElement paramsElement)
    {
        if (!_recoveryMode)
            return JsonSerializer.Serialize(new { error = "Recovery mode not active" });

        var share = JsonSerializer.Deserialize<Share>(paramsElement.GetRawText());
        
        if (share == null)
            return JsonSerializer.Serialize(new { error = "Invalid share" });

        // Check if share with this index already provided
        if (_recoveryShares.Any(s => s.Index == share.Index))
            return JsonSerializer.Serialize(new { error = "Share with this index already provided" });

        _recoveryShares.Add(share);
        _auditLogger.LogRecoveryEvent("ShareProvided", $"Share {share.Index} provided. Total: {_recoveryShares.Count}/{_recoveryThreshold}");

        if (_recoveryShares.Count >= _recoveryThreshold)
        {
            try
            {
                // Reconstruct the secret
                var reconstructedKey = _sssManager.ReconstructSecret(_recoveryShares);
                
                // We need to derive the public key - for Ed25519, we can use the crypto provider
                var cryptoProvider = new Crypto.Ed25519Provider();
                var publicKey = DerivePublicKeyFromPrivate(reconstructedKey);

                // Import the reconstructed key
                _keyManager.ImportPrivateKeyFromSSS(reconstructedKey, publicKey);
                
                _recoveryMode = false;
                _recoveryShares.Clear();

                _auditLogger.LogRecoveryEvent("RecoveryCompleted", "Key successfully recovered");

                return JsonSerializer.Serialize(new RecoveryStatus
                {
                    IsActive = false,
                    SharesProvided = _recoveryThreshold,
                    SharesRequired = _recoveryThreshold,
                    CanRecover = true,
                    Message = "Recovery successful. Key restored."
                });
            }
            catch (Exception ex)
            {
                _auditLogger.LogRecoveryEvent("RecoveryFailed", $"Error: {ex.Message}");
                return JsonSerializer.Serialize(new { error = $"Recovery failed: {ex.Message}" });
            }
        }

        return JsonSerializer.Serialize(new RecoveryStatus
        {
            IsActive = true,
            SharesProvided = _recoveryShares.Count,
            SharesRequired = _recoveryThreshold,
            CanRecover = false,
            Message = $"Share accepted. Need {_recoveryThreshold - _recoveryShares.Count} more."
        });
    }

    private string HandleRecoverStatus()
    {
        return JsonSerializer.Serialize(new RecoveryStatus
        {
            IsActive = _recoveryMode,
            SharesProvided = _recoveryShares.Count,
            SharesRequired = _recoveryThreshold,
            CanRecover = _recoveryShares.Count >= _recoveryThreshold,
            Message = _recoveryMode 
                ? $"Recovery in progress. {_recoveryShares.Count}/{_recoveryThreshold} shares provided."
                : "No active recovery."
        });
    }

    private string HandleDeleteKey(JsonElement paramsElement)
    {
        try
        {
            // Parse confirmation parameter
            var confirmDeletion = paramsElement.TryGetProperty("confirmDeletion", out var confirm) 
                && confirm.GetBoolean();

            if (!confirmDeletion)
            {
                return JsonSerializer.Serialize(new
                {
                    Success = false,
                    Message = "Key deletion must be explicitly confirmed"
                });
            }

            // Check if key exists
            if (!_keyManager.HasKey && !_keyManager.KeyFileExists())
            {
                return JsonSerializer.Serialize(new
                {
                    Success = false,
                    Message = "No key exists to delete"
                });
            }

            // Perform deletion
            _keyManager.DeleteKey(confirmDeletion: true);

            _auditLogger.LogSecurityEvent("KeyDeleted", 
                "Master key permanently deleted via AdminCLI (CRITICAL)");

            return JsonSerializer.Serialize(new
            {
                Success = true,
                Message = "Master key successfully deleted. Service is now in keyless state."
            });
        }
        catch (Exception ex)
        {
            _auditLogger.LogSecurityEvent("KeyDeletionFailed", $"Error: {ex.Message}");
            return JsonSerializer.Serialize(new
            {
                Success = false,
                Message = $"Key deletion failed: {ex.Message}"
            });
        }
    }

    private byte[] DerivePublicKeyFromPrivate(byte[] privateKey)
    {
        // For Ed25519, the public key can be derived from private key
        // The private key in Ed25519 is 32 bytes, and we can generate the public key from it
        var privateKeyParams = new Org.BouncyCastle.Crypto.Parameters.Ed25519PrivateKeyParameters(privateKey, 0);
        var publicKeyParams = privateKeyParams.GeneratePublicKey();
        return publicKeyParams.GetEncoded();
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        Task.WaitAll(_clientTasks.ToArray(), TimeSpan.FromSeconds(5));
        _auditLogger.LogSecurityEvent("IPCServerStopped", "Server stopped");
    }

    public void Dispose()
    {
        Stop();
        _cancellationTokenSource.Dispose();
    }
}
