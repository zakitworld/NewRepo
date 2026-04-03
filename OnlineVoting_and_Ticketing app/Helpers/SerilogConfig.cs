using Serilog;
using Serilog.Events;

namespace OnlineVoting_and_Ticketing_app.Helpers
{
    /// <summary>
    /// Configures Serilog with a rolling file sink stored in the app data directory.
    /// Logs are retained for 7 days and capped at 20 MB per file.
    /// </summary>
    public static class SerilogConfig
    {
        public static void Configure()
        {
            var logPath = Path.Combine(FileSystem.AppDataDirectory, "logs", "eventhub-.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.File(
                    path: logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    fileSizeLimitBytes: 20 * 1024 * 1024,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("EventHub starting — log path: {LogPath}", logPath);
        }

        public static void CloseAndFlush() => Log.CloseAndFlush();
    }
}
