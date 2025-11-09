using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using StampService.Core.Models;

namespace StampService.ClientLib;

/// <summary>
/// Client library for communicating with StampService
/// </summary>
public class StampServiceClient : IDisposable
{
    private readonly string _pipeName;
    private readonly int _timeoutMs;

    public StampServiceClient(string pipeName = "StampServicePipe", int timeoutMs = 5000)
    {
        _pipeName = pipeName;
        _timeoutMs = timeoutMs;
    }

    /// <summary>
    /// Sign a request
    /// </summary>
    public async Task<SignedResponse> SignAsync(SignRequest request)
    {
        var response = await SendRequestAsync("Sign", request);
        return JsonSerializer.Deserialize<SignedResponse>(response) 
            ?? throw new InvalidOperationException("Invalid response from service");
    }

    /// <summary>
    /// Request a test stamp for health check
    /// </summary>
    public async Task<SignedResponse> TestStampAsync()
    {
        var response = await SendRequestAsync("TestStamp", new { });
        return JsonSerializer.Deserialize<SignedResponse>(response) 
            ?? throw new InvalidOperationException("Invalid response from service");
    }

    /// <summary>
    /// Get service status
    /// </summary>
    public async Task<ServiceStatus> GetStatusAsync()
    {
        var response = await SendRequestAsync("GetStatus", new { });
        return JsonSerializer.Deserialize<ServiceStatus>(response) 
            ?? throw new InvalidOperationException("Invalid response from service");
    }

    /// <summary>
    /// Verify a signature locally
    /// </summary>
    public bool VerifySignature(SignedResponse signedResponse)
    {
        try
        {
            // Decode the signature and payload
            var signature = Convert.FromBase64String(signedResponse.Signature);
            var payload = Convert.FromBase64String(signedResponse.SignedPayload);

            // Get the public key
            var publicKeyPem = signedResponse.PublicKey 
                ?? throw new ArgumentException("No public key in response");

            // Use the crypto provider to verify
            var cryptoProvider = new Core.Crypto.Ed25519Provider();
            var publicKey = cryptoProvider.ImportPublicKeyPem(publicKeyPem);

            return cryptoProvider.Verify(publicKey, payload, signature);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Store a secret securely in the service
    /// </summary>
    public async Task<bool> StoreSecretAsync(string name, string value, Dictionary<string, string>? metadata = null)
    {
        var parameters = new
        {
            name = name,
            value = value,
            metadata = metadata
        };

        var response = await SendRequestAsync("StoreSecret", parameters);
        var result = JsonSerializer.Deserialize<JsonElement>(response);
        return result.TryGetProperty("success", out var success) && success.GetBoolean();
    }

    /// <summary>
    /// Retrieve a secret from the service
    /// </summary>
    public async Task<(string value, DateTime createdAt, Dictionary<string, string> metadata)?> RetrieveSecretAsync(string name)
    {
        var parameters = new { name = name };
        var response = await SendRequestAsync("RetrieveSecret", parameters);
        var result = JsonSerializer.Deserialize<JsonElement>(response);

        if (!result.TryGetProperty("success", out var success) || !success.GetBoolean())
            return null;

        var value = result.GetProperty("value").GetString() ?? string.Empty;
        var createdAt = result.GetProperty("createdAt").GetDateTime();
        var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(
            result.GetProperty("metadata").GetRawText()) ?? new Dictionary<string, string>();

        return (value, createdAt, metadata);
    }

    /// <summary>
    /// List all stored secrets (names only)
    /// </summary>
    public async Task<List<string>> ListSecretsAsync()
    {
        var response = await SendRequestAsync("ListSecrets", new { });
        var result = JsonSerializer.Deserialize<JsonElement>(response);

        if (!result.TryGetProperty("success", out var success) || !success.GetBoolean())
            return new List<string>();

        return JsonSerializer.Deserialize<List<string>>(result.GetProperty("secrets").GetRawText()) 
            ?? new List<string>();
    }

    /// <summary>
    /// Delete a secret
    /// </summary>
    public async Task<bool> DeleteSecretAsync(string name)
    {
        var parameters = new { name = name };
        var response = await SendRequestAsync("DeleteSecret", parameters);
        var result = JsonSerializer.Deserialize<JsonElement>(response);
        return result.TryGetProperty("success", out var success) && success.GetBoolean();
    }

    /// <summary>
    /// Check if a secret exists
    /// </summary>
    public async Task<bool> SecretExistsAsync(string name)
    {
        var parameters = new { name = name };
        var response = await SendRequestAsync("SecretExists", parameters);
        var result = JsonSerializer.Deserialize<JsonElement>(response);
   
        if (!result.TryGetProperty("success", out var success) || !success.GetBoolean())
            return false;

        return result.TryGetProperty("exists", out var exists) && exists.GetBoolean();
    }

    /// <summary>
    /// Send a generic request to the service
    /// </summary>
    private async Task<string> SendRequestAsync(string method, object parameters)
    {
        using var pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        // Connect with timeout
        var cts = new CancellationTokenSource(_timeoutMs);
        await pipeClient.ConnectAsync(cts.Token);

        // CRITICAL FIX: Set ReadMode to Message to match server
        pipeClient.ReadMode = PipeTransmissionMode.Message;

        // Prepare request
        var request = new
        {
            method = method,
            @params = parameters
        };

        var requestJson = JsonSerializer.Serialize(request);
        var requestBytes = Encoding.UTF8.GetBytes(requestJson);

        // Send request
        await pipeClient.WriteAsync(requestBytes, 0, requestBytes.Length, cts.Token);
        await pipeClient.FlushAsync(cts.Token);

        // Read response
        var buffer = new byte[4096];
        using var ms = new MemoryStream();

        int bytesRead;
        do
        {
            bytesRead = await pipeClient.ReadAsync(buffer, 0, buffer.Length, cts.Token);
            ms.Write(buffer, 0, bytesRead);
        } while (!pipeClient.IsMessageComplete);

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public void Dispose()
    {
        // Nothing to dispose currently, but keeping for future use
    }
}
