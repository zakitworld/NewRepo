using Serilog;

namespace OnlineVoting_and_Ticketing_app.Helpers
{
    public static class GlobalExceptionHandler
    {
        public static void Initialize()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                HandleException(args.ExceptionObject as Exception);

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                HandleException(args.Exception);
                args.SetObserved();
            };
        }

        private static void HandleException(Exception? ex)
        {
            if (ex is null) return;

            // Structured log to file via Serilog
            Log.Fatal(ex, "Unhandled exception: {Message}", ex.Message);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Unexpected Error",
                        "An unexpected error occurred. The team has been notified. Please restart the app if issues persist.",
                        "OK");
                }
                catch { /* Shell not ready — swallow */ }
            });
        }
    }
}
