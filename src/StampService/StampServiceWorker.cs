using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StampService.Core;
using StampService.Core.Crypto;
using Microsoft.Extensions.Configuration;

namespace StampService;

/// <summary>
/// Main Windows Service Worker
/// </summary>
public class StampServiceWorker : BackgroundService
{
    private readonly ILogger<StampServiceWorker> _logger;
    private readonly IConfiguration _configuration;
    private KeyManager? _keyManager;
    private IPCServer? _ipcServer;
    private AuditLogger? _auditLogger;
    private SSSManager? _sssManager;

    public StampServiceWorker(ILogger<StampServiceWorker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("StampService starting at: {time}", DateTimeOffset.Now);

            // Read configuration
            var pipeName = _configuration["ServiceConfiguration:PipeName"] ?? "StampServicePipe";
            var keyStorePath = _configuration["ServiceConfiguration:KeyStorePath"] ?? 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 
                    "StampService", "master.key");
            var auditLogPath = _configuration["ServiceConfiguration:AuditLogPath"] ?? 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 
                    "StampService", "Logs", "audit.log");

            // Ensure directories exist
            var keyDir = Path.GetDirectoryName(keyStorePath);
            if (!string.IsNullOrEmpty(keyDir) && !Directory.Exists(keyDir))
            {
                Directory.CreateDirectory(keyDir);
            }

            var logDir = Path.GetDirectoryName(auditLogPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            // Initialize components
            _auditLogger = new AuditLogger(auditLogPath);
            var cryptoProvider = new Ed25519Provider();
            _keyManager = new KeyManager(cryptoProvider, _auditLogger, keyStorePath);
            _sssManager = new SSSManager();

            // Load or generate key
            if (!_keyManager.LoadKey())
            {
                _logger.LogWarning("No existing key found. Generating new key...");
                _keyManager.GenerateKey();
                _logger.LogInformation("New key generated and stored securely");
            }
            else
            {
                _logger.LogInformation("Existing key loaded successfully");
            }

            // Log public key for verification
            _logger.LogInformation("Public Key (PEM):\n{PublicKey}", _keyManager.GetPublicKeyPem());

            // Start IPC server
            _ipcServer = new IPCServer(pipeName, _keyManager, _sssManager, _auditLogger);
            _logger.LogInformation("Starting IPC server on pipe: {PipeName}", pipeName);

            await _ipcServer.StartAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in StampService");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StampService stopping at: {time}", DateTimeOffset.Now);
        
        _ipcServer?.Stop();
        _keyManager?.Dispose();

        await base.StopAsync(cancellationToken);
    }
}
