using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CathermanCore;

public static class StartupExtensions
{
    public static void AddLogging(this IHostApplicationBuilder builder)
    {
        var logSettingsSection = builder.Configuration.GetSection("Log")
            ?? throw new InvalidOperationException("Log settings not found.");
        var logSettings = logSettingsSection.Get<Logging>()
            ?? throw new InvalidOperationException("Log settings not found.");
        var environment = builder.Environment.EnvironmentName ?? "";

        if (environment.HasValue())
            logSettings.Environment = environment;

        // AddSerilog attaches directly to IServiceCollection via builder.Services
        builder.Services.AddSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(logSettings.GetConfiguration())
                .Destructure.With<SerilogCustomModelDestructuringPolicy>());

        builder.Services.Configure<Logging>(options => logSettingsSection.Bind(options));

        Log.Information("Logging configured.");
    }

    public static IConfiguration GetConfiguration(this Logging logSettings) =>
        new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.ASCII.GetBytes(logSettings.Json)))
            .Build();
}