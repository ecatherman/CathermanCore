using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CathermanCore;

public static class StartupExtensions
{
    /// <summary>
    /// Configures Serilog for the application from the "Log" section of the configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The "Log" section is bound to <see cref="Logging"/>. If the host has an environment name, it replaces
    /// <see cref="Logging.Environment"/>. Serilog is then configured from the JSON that <see cref="Logging"/> builds,
    /// with <see cref="SerilogCustomModelDestructuringPolicy"/> registered so that entities and the application's
    /// model types are written to the log in a readable shape. The <see cref="Logging"/> settings are also registered
    /// as options so they can be injected as <c>IOptions&lt;Logging&gt;</c>.
    /// </para>
    /// <para>Call this before anything else that logs, so the rest of startup is captured.</para>
    /// </remarks>
    /// <param name="builder">The host application builder to configure.</param>
    /// <param name="modelNamespace">
    /// Optional. The namespace that holds the application's model classes (for example <c>"MyApp.Models"</c>).
    /// It is assigned to <see cref="SerilogCustomModelDestructuringPolicy.ModelNamespace"/> before logging is set up,
    /// and objects from that namespace are then destructured by the policy instead of being logged with their default
    /// representation. When omitted (null), the current value is left as it is, which is no special handling unless
    /// something else has already set it.
    /// </param>
    /// <exception cref="InvalidOperationException">The "Log" settings section is missing or cannot be read.</exception>
    public static void AddLogging(this IHostApplicationBuilder builder, string? modelNamespace = null)
    {
        if (modelNamespace != null)
            SerilogCustomModelDestructuringPolicy.ModelNamespace = modelNamespace;

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