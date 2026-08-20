using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace FoodDelivery.Shared.Extensions;

/// <summary>
/// Extension methods for configuring Serilog structured logging.
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// Configures Serilog with console and file sinks, structured logging,
    /// and enrichment with machine name, environment, and correlation IDs.
    /// </summary>
    public static void ConfigureSerilog(IConfiguration configuration, string serviceName)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {ServiceName} | {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: $"Logs/{serviceName}-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {ServiceName} | {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
