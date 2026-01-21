using System.Diagnostics;

namespace OnlineVoting_and_Ticketing_app.Helpers
{
    public static class GlobalExceptionHandler
    {
        public static void Initialize()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                LogException(args.ExceptionObject as Exception);
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                LogException(args.Exception);
                args.SetObserved();
            };
        }

        private static void LogException(Exception? ex)
        {
            if (ex == null) return;

            Debug.WriteLine($"[GlobalException] {ex.Message}");
            Debug.WriteLine(ex.StackTrace);

            // In a real app, you would use a service like AppCenter or Sentry here
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.DisplayAlertAsync("Unexpected Error",
                    "An unexpected error occurred. Please try again or contact support if the issue persists.",
                    "OK");
            });
        }
    }
}
