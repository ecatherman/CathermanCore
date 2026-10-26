namespace CathermanCore;

public class Logging
{
    public string LogLevel { get; set; } = nameof(Serilog.Events.LogEventLevel.Information);
	public string MicrosoftLogLevel { get; set; } = nameof(Serilog.Events.LogEventLevel.Warning);
	public string SystemLogLevel { get; set; } = nameof(Serilog.Events.LogEventLevel.Warning);
	public string EfLogLevel { get; set; } = nameof(Serilog.Events.LogEventLevel.Information);
    public string FilePath { get; set; } = "Logs/applog-.txt";
	public string FileInterval { get; set; } = nameof(Serilog.RollingInterval.Day);
	public string Application { get; set; } = "App";
	public string Environment { get; set; } = "Development";
	private static int FileSizeLimitBytes { get; set; } = 5242880;//5MB
	public static int FileCountToKeep { get; set; } = 31;

	public string Json => @$"
	{{
		""Serilog"": {{
			""Using"": [ ""Serilog.Sinks.Console"", ""Serilog.Sinks.File"" ],
			""MinimumLevel"": {{
				""Default"": ""{LogLevel}"",
				""Override"": {{
					""Microsoft"": ""{MicrosoftLogLevel}"",
					""System"": ""{SystemLogLevel}"",
					""Microsoft.EntityFrameworkCore.Database.Command"": ""{EfLogLevel}""
				}}
			}},
			""WriteTo"": [
				{{ ""Name"": ""Console"" }},
				{{ ""Name"": ""Debug"" }},
				{{ 
					""Name"": ""File"",
					""Args"": {{
						""path"": ""{FilePath}"",
						""rollingInterval"": ""{FileInterval}"",
						""fileSizeLimitBytes"": {FileSizeLimitBytes},
						""retainedFileCountLimit"": {FileCountToKeep},
						""formatter"": ""Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact""
					}}
				}}
			],
			""Enrich"": [ ""FromLogContext"", ""WithMachineName"", ""WithProcessId"", ""WithThreadId"" ],
			""Properties"": {{
				""Application"": ""{Application}"",
				""Environment"": ""{Environment}""
			}}
		}}
	}}";

    private class WriteTo
    {
        public string Name { get; set; } = "";
        public Args Args { get; set; } = default!;
    }

    private class Args
    {
        public string path { get; set; } = "";
        public string rollingInterval { get; set; } = "";
        public bool rollOnFileSizeLimit { get; set; } = true;
		public int fileSizeLimitBytes { get; set; }
        public int retainedFileCountLimit { get; set; }
        public string formatter { get; set; } = "";
    }
}

