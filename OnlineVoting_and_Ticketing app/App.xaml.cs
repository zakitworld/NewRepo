using Microsoft.Extensions.DependencyInjection;
using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Data;
using OnlineVoting_and_Ticketing_app.Helpers;
using OnlineVoting_and_Ticketing_app.Services;
using Serilog;

namespace OnlineVoting_and_Ticketing_app
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            SerilogConfig.Configure();
            GlobalExceptionHandler.Initialize();
            ApplySavedTheme();
        }

        /// <summary>
        /// Restores the user's saved dark/light mode preference on every cold start.
        /// </summary>
        private static void ApplySavedTheme()
        {
            try
            {
                var isDark = Preferences.Get("settings_dark_mode", true);
                Current!.UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;
            }
            catch { /* fall back to system default */ }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell()) { Title = "EventHub" };

            // activationState.Context is the IMauiContext — reliably available here,
            // unlike Handler?.MauiContext which is null during the constructor.
            if (activationState?.Context?.Services is { } services)
                _ = InitializeDatabaseAsync(services);

            return window;
        }

        private async Task InitializeDatabaseAsync(IServiceProvider services)
        {
            try
            {
                // Create / migrate database schema
                var dbContext = services.GetRequiredService<AppDbContext>();
                await dbContext.Database.EnsureCreatedAsync();

                // Expire stale tickets on startup
                var ticketService = services.GetRequiredService<ITicketService>();
                await ticketService.ExpireOldTicketsAsync();

                // (AppCenter removed — see csproj comment)
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database initialisation failed");
            }
            finally
            {
                // Always run auth check — navigate to main if logged in,
                // otherwise the default Shell page (LoginPage) is already shown.
                await CheckAuthenticationAsync();
            }
        }

        private static async Task CheckAuthenticationAsync()
        {
            await Task.Delay(300);
            try
            {
                var isLoggedIn = await SecureStorage.GetAsync(AppConstants.Preferences.IsLoggedIn);
                if (isLoggedIn == "true")
                {
                    await MainThread.InvokeOnMainThreadAsync(
                        () => Shell.Current.GoToAsync("//main/home"));
                }
            }
            catch
            {
                // SecureStorage may be unavailable on some platforms or in tests.
                // Swallow exceptions here to avoid crashing the app during startup
                // and allow the default (login) page to be shown.
            }
            // Not logged in — AppShell already shows LoginPage as the default page.
        }
    }
}
