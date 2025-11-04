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
    /// Send a generic request to the service
    /// </summary>
    private async Task<string> SendRequestAsync(string method, object parameters)
    {
        using var pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        // Connect with timeout
        var cts = new CancellationTokenSource(_timeoutMs);
        await pipeClient.ConnectAsync(cts.Token);

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
