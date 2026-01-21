using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineVoting_and_Ticketing_app.Data;

namespace OnlineVoting_and_Ticketing_app
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Helpers.GlobalExceptionHandler.Initialize();

            // Initialize database
            InitializeDatabaseAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell())
            {
                Title = "EventHub"
            };
        }

        private async void InitializeDatabaseAsync()
        {
            try
            {
                if (Handler?.MauiContext?.Services == null)
                {
                    await Task.Delay(1000);
                    InitializeDatabaseAsync();
                    return;
                }

                var dbContext = Handler.MauiContext.Services.GetRequiredService<AppDbContext>();
                if (dbContext != null)
                {
                    await dbContext.Database.EnsureCreatedAsync();
                }

                await CheckAuthenticationAsync();
            }
            catch
            {
                // Silent fail - app will show login screen on error
            }
        }

        private async Task CheckAuthenticationAsync()
        {
            // Wait a moment for the app to fully initialize
            await Task.Delay(500);

            var isLoggedIn = await SecureStorage.GetAsync(Constants.AppConstants.Preferences.IsLoggedIn);

            if (isLoggedIn != "true")
            {
                // Navigate to login page
                await Shell.Current.GoToAsync("//login");
            }
        }
    }
}