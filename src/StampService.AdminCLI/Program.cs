using System.CommandLine;
using System.Text.Json;
using StampService.ClientLib;
using StampService.Core.Models;
using System.Diagnostics;
using System.Security.Principal;

namespace StampService.AdminCLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("StampService Admin CLI - Manage the Secure Stamp Service");

        // Status command
        var statusCommand = new Command("status", "Get service status");
        statusCommand.SetHandler(async () => await HandleStatusAsync());
        rootCommand.AddCommand(statusCommand);

        // Test-stamp command
        var testStampCommand = new Command("test-stamp", "Request a test stamp for health check");
        testStampCommand.SetHandler(async () => await HandleTestStampAsync());
        rootCommand.AddCommand(testStampCommand);

        // Create-shares command
        var createSharesCommand = new Command("create-shares", "Create Shamir Secret Shares for backup");
        var totalSharesOption = new Option<int>(
            aliases: new[] { "--total", "-n" },
            description: "Total number of shares to create",
            getDefaultValue: () => 5);
        var thresholdOption = new Option<int>(
            aliases: new[] { "--threshold", "-t" },
            description: "Minimum shares required for recovery",
            getDefaultValue: () => 3);
        var initiatorOption = new Option<string>(
            aliases: new[] { "--initiator", "-i" },
            description: "Name of the person initiating share creation",
            getDefaultValue: () => Environment.UserName);
        var outputDirOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output directory for share files",
            getDefaultValue: () => ".");

        createSharesCommand.AddOption(totalSharesOption);
        createSharesCommand.AddOption(thresholdOption);
        createSharesCommand.AddOption(initiatorOption);
        createSharesCommand.AddOption(outputDirOption);
        createSharesCommand.SetHandler(async (int total, int threshold, string initiator, string output) =>
        {
            await HandleCreateSharesAsync(total, threshold, initiator, output);
        }, totalSharesOption, thresholdOption, initiatorOption, outputDirOption);
        rootCommand.AddCommand(createSharesCommand);

        // Verify-share command
        var verifyShareCommand = new Command("verify-share", "Verify a share file");
        var shareFileArg = new Argument<string>("share-file", "Path to the share file");
        var commitmentOption = new Option<string>(
            aliases: new[] { "--commitment", "-c" },
            description: "Expected commitment hash");
        verifyShareCommand.AddArgument(shareFileArg);
        verifyShareCommand.AddOption(commitmentOption);
        verifyShareCommand.SetHandler(async (string shareFile, string commitment) =>
        {
            await HandleVerifyShareAsync(shareFile, commitment);
        }, shareFileArg, commitmentOption);
        rootCommand.AddCommand(verifyShareCommand);

        // Recover command
        var recoverCommand = new Command("recover", "Start or continue key recovery process");
        var recoverSubcommands = new Command[]
        {
            CreateRecoverStartCommand(),
            CreateRecoverAddShareCommand(),
            CreateRecoverStatusCommand()
        };
        foreach (var subcommand in recoverSubcommands)
        {
            recoverCommand.AddCommand(subcommand);
        }
        rootCommand.AddCommand(recoverCommand);

        // Install-service command
        var installCommand = new Command("install-service", "Install StampService as a Windows Service");
        var servicePathOption = new Option<string>(
            aliases: new[] { "--path", "-p" },
            description: "Path to StampService.exe");
        installCommand.AddOption(servicePathOption);
        installCommand.SetHandler(async (string path) =>
        {
            await HandleInstallServiceAsync(path);
        }, servicePathOption);
        rootCommand.AddCommand(installCommand);

        // Uninstall-service command
        var uninstallCommand = new Command("uninstall-service", "Uninstall StampService Windows Service");
        uninstallCommand.SetHandler(async () => await HandleUninstallServiceAsync());
        rootCommand.AddCommand(uninstallCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task HandleStatusAsync()
    {
        try
        {
            Console.WriteLine("Connecting to StampService...");
            using var client = new StampServiceClient();
            var status = await client.GetStatusAsync();

            Console.WriteLine("\n=== StampService Status ===");
            Console.WriteLine($"Running: {status.IsRunning}");
            Console.WriteLine($"Key Present: {status.KeyPresent}");
            Console.WriteLine($"Uptime: {TimeSpan.FromSeconds(status.UptimeSeconds)}");
            Console.WriteLine($"Last Health Check: {status.LastHealthCheck:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Algorithm: {status.Algorithm ?? "N/A"}");
            
            if (!string.IsNullOrEmpty(status.PublicKey))
            {
                Console.WriteLine("\nPublic Key:");
                Console.WriteLine(status.PublicKey);
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static async Task HandleTestStampAsync()
    {
        try
        {
            Console.WriteLine("Requesting test stamp...");
            using var client = new StampServiceClient();
            var response = await client.TestStampAsync();

            Console.WriteLine("\n=== Test Stamp Response ===");
            Console.WriteLine($"Algorithm: {response.Algorithm}");
            Console.WriteLine($"Signer ID: {response.SignerId}");
            Console.WriteLine($"Timestamp: {response.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
            Console.WriteLine($"\nSignature (first 64 chars):");
            Console.WriteLine(response.Signature.Substring(0, Math.Min(64, response.Signature.Length)));

            // Verify signature
            var isValid = client.VerifySignature(response);
            Console.ForegroundColor = isValid ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"\n? Signature Valid: {isValid}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static async Task HandleCreateSharesAsync(int total, int threshold, string initiator, string outputDir)
    {
        try
        {
            Console.WriteLine($"Creating {total} shares with threshold {threshold}...");
            Console.WriteLine($"Initiator: {initiator}");
            Console.WriteLine();

            // Send request via Named Pipe
            using var client = new StampServiceClient();
            var requestJson = JsonSerializer.Serialize(new
            {
                method = "CreateShares",
                @params = new ShareCreationOptions
                {
                    TotalShares = total,
                    Threshold = threshold,
                    Initiator = initiator
                }
            });

            // Use reflection to call private method (for admin operations)
            var response = await CallServiceMethodAsync<ShareBundle>("CreateShares", new ShareCreationOptions
            {
                TotalShares = total,
                Threshold = threshold,
                Initiator = initiator
            });

            if (response == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to create shares");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("? Shares created successfully!");
            Console.ResetColor();
            Console.WriteLine($"\nCommitment: {response.Commitment}");
            Console.WriteLine($"Algorithm: {response.Algorithm}");
            Console.WriteLine();

            // Save shares to files
            Directory.CreateDirectory(outputDir);
            foreach (var share in response.Shares)
            {
                var fileName = Path.Combine(outputDir, $"share-{share.Index}-of-{share.TotalShares}.json");
                var shareJson = JsonSerializer.Serialize(share, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(fileName, shareJson);
                Console.WriteLine($"Share {share.Index} saved to: {fileName}");
            }

            // Save commitment separately
            var commitmentFile = Path.Combine(outputDir, "commitment.txt");
            await File.WriteAllTextAsync(commitmentFile, response.Commitment);
            Console.WriteLine($"\nCommitment saved to: {commitmentFile}");

            Console.WriteLine("\n??  IMPORTANT: Distribute shares to trusted custodians securely!");
            Console.WriteLine("    Each custodian should verify their share before storing it.");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static async Task HandleVerifyShareAsync(string shareFile, string commitment)
    {
        try
        {
            if (!File.Exists(shareFile))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Share file not found: {shareFile}");
                Console.ResetColor();
                return;
            }

            var shareJson = await File.ReadAllTextAsync(shareFile);
            var share = JsonSerializer.Deserialize<Share>(shareJson);

            if (share == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid share file");
                Console.ResetColor();
                return;
            }

            // If no commitment provided, use the one from the share
            if (string.IsNullOrEmpty(commitment))
            {
                commitment = share.Commitment ?? "";
            }

            Console.WriteLine($"Verifying share {share.Index}...");

            var result = await CallServiceMethodAsync<ShareVerificationResult>("VerifyShare", new
            {
                share = share,
                commitment = commitment
            });

            if (result == null || !result.IsValid)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"? Share verification failed: {result?.Message ?? "Unknown error"}");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"? Share {result.ShareIndex} is valid!");
            Console.ResetColor();
            Console.WriteLine($"Message: {result.Message}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static Command CreateRecoverStartCommand()
    {
        var command = new Command("start", "Start a new recovery session");
        var thresholdOption = new Option<int>(
            aliases: new[] { "--threshold", "-t" },
            description: "Minimum shares required for recovery",
            getDefaultValue: () => 3);
        command.AddOption(thresholdOption);
        command.SetHandler(async (int threshold) =>
        {
            await HandleRecoverStartAsync(threshold);
        }, thresholdOption);
        return command;
    }

    static Command CreateRecoverAddShareCommand()
    {
        var command = new Command("add-share", "Add a share to the recovery session");
        var shareFileArg = new Argument<string>("share-file", "Path to the share file");
        command.AddArgument(shareFileArg);
        command.SetHandler(async (string shareFile) =>
        {
            await HandleRecoverAddShareAsync(shareFile);
        }, shareFileArg);
        return command;
    }

    static Command CreateRecoverStatusCommand()
    {
        var command = new Command("status", "Check recovery session status");
        command.SetHandler(async () => await HandleRecoverStatusAsync());
        return command;
    }

    static async Task HandleRecoverStartAsync(int threshold)
    {
        try
        {
            Console.WriteLine($"Starting recovery session with threshold {threshold}...");
            
            var result = await CallServiceMethodAsync<RecoveryStatus>("RecoverStart", new
            {
                threshold = threshold
            });

            if (result == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to start recovery");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("? Recovery session started");
            Console.ResetColor();
            Console.WriteLine($"Status: {result.Message}");
            Console.WriteLine($"Shares needed: {result.SharesRequired}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static async Task HandleRecoverAddShareAsync(string shareFile)
    {
        try
        {
            if (!File.Exists(shareFile))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Share file not found: {shareFile}");
                Console.ResetColor();
                return;
            }

            var shareJson = await File.ReadAllTextAsync(shareFile);
            var share = JsonSerializer.Deserialize<Share>(shareJson);

            if (share == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid share file");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"Adding share {share.Index} to recovery session...");

            var result = await CallServiceMethodAsync<RecoveryStatus>("RecoverProvideShare", share);

            if (result == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to add share");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = result.CanRecover ? ConsoleColor.Green : ConsoleColor.Yellow;
            Console.WriteLine($"? {result.Message}");
            Console.ResetColor();
            Console.WriteLine($"Progress: {result.SharesProvided}/{result.SharesRequired}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static async Task HandleRecoverStatusAsync()
    {
        try
        {
            var result = await CallServiceMethodAsync<RecoveryStatus>("RecoverStatus", new { });

            if (result == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to get recovery status");
                Console.ResetColor();
                return;
            }

            Console.WriteLine("\n=== Recovery Status ===");
            Console.WriteLine($"Active: {result.IsActive}");
            Console.WriteLine($"Shares provided: {result.SharesProvided}/{result.SharesRequired}");
            Console.WriteLine($"Can recover: {result.CanRecover}");
            Console.WriteLine($"Status: {result.Message}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static async Task HandleInstallServiceAsync(string? servicePath)
    {
        if (!IsAdministrator())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: This command requires administrator privileges");
            Console.ResetColor();
            return;
        }

        try
        {
            if (string.IsNullOrEmpty(servicePath))
            {
                // Try to find StampService.exe in common locations
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                servicePath = Path.Combine(exeDir, "..", "StampService", "StampService.exe");
                
                if (!File.Exists(servicePath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Could not find StampService.exe. Please specify path with --path");
                    Console.ResetColor();
                    return;
                }
            }

            servicePath = Path.GetFullPath(servicePath);

            Console.WriteLine($"Installing service from: {servicePath}");

            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"create \"SecureStampService\" binPath= \"{servicePath}\" start= auto DisplayName= \"Secure Stamp Service\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                await process.WaitForExitAsync();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                if (process.ExitCode == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("? Service installed successfully!");
                    Console.ResetColor();
                    Console.WriteLine("\nTo start the service, run:");
                    Console.WriteLine("  net start SecureStampService");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Failed to install service: {error}");
                    Console.ResetColor();
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static async Task HandleUninstallServiceAsync()
    {
        if (!IsAdministrator())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: This command requires administrator privileges");
            Console.ResetColor();
            return;
        }

        try
        {
            Console.WriteLine("Stopping service...");

            var stopPsi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = "stop \"SecureStampService\"",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using (var process = Process.Start(stopPsi))
            {
                if (process != null)
                {
                    await process.WaitForExitAsync();
                }
            }

            await Task.Delay(2000); // Wait for service to stop

            Console.WriteLine("Uninstalling service...");

            var deletePsi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = "delete \"SecureStampService\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var deleteProcess = Process.Start(deletePsi);
            if (deleteProcess != null)
            {
                await deleteProcess.WaitForExitAsync();
                var error = await deleteProcess.StandardError.ReadToEndAsync();

                if (deleteProcess.ExitCode == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("? Service uninstalled successfully!");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Failed to uninstall service: {error}");
                    Console.ResetColor();
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static bool IsAdministrator()
    {
        if (OperatingSystem.IsWindows())
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        return false;
    }

    static async Task<T?> CallServiceMethodAsync<T>(string method, object parameters)
    {
        using var client = new StampServiceClient();
        
        // Use reflection to access the private SendRequestAsync method
        var methodInfo = typeof(StampServiceClient).GetMethod("SendRequestAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (methodInfo == null)
            throw new InvalidOperationException("Could not access service method");

        var task = (Task<string>)methodInfo.Invoke(client, new object[] { method, parameters })!;
        var response = await task;
        
        return JsonSerializer.Deserialize<T>(response);
    }
}
