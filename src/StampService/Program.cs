using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StampService;
using Serilog;

// Check for console mode
var isConsoleMode = args.Contains("--console");

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 
            "StampService", "Logs", "service.log"),
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("StampService initializing...");
    Log.Information("Mode: {Mode}", isConsoleMode ? "Console" : "Windows Service");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure Windows Service (ignored in console mode)
    if (!isConsoleMode)
    {
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "Secure Stamp Service";
        });
    }

    // Add hosted service
    builder.Services.AddHostedService<StampServiceWorker>();

    // Add Serilog
    builder.Services.AddSerilog();

    var host = builder.Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "StampService terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
