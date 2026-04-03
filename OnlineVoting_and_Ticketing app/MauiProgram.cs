using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnlineVoting_and_Ticketing_app.Data;
using OnlineVoting_and_Ticketing_app.Repositories;
using OnlineVoting_and_Ticketing_app.Services;
using OnlineVoting_and_Ticketing_app.ViewModels.Auth;
using OnlineVoting_and_Ticketing_app.ViewModels.Events;
using OnlineVoting_and_Ticketing_app.ViewModels.Polls;
using OnlineVoting_and_Ticketing_app.ViewModels.Profile;
using OnlineVoting_and_Ticketing_app.ViewModels.Tickets;
using Serilog;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Reflection;

namespace OnlineVoting_and_Ticketing_app
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // ── Configuration ────────────────────────────────────────────────
            // Load appsettings.json embedded as a resource
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(
                "OnlineVoting_and_Ticketing_app.appsettings.json");

            if (stream is not null)
            {
                var config = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();
                builder.Configuration.AddConfiguration(config);
            }

            // ── Database (SQLCipher encrypted) ───────────────────────────────
            // The encryption key is derived from the app's unique install ID stored in
            // SecureStorage. On first run a new random key is generated and persisted.
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "eventhub.db");
            var dbEncryptionKey = GetOrCreateDbKeyAsync().GetAwaiter().GetResult();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath};Password={dbEncryptionKey}"));

            // ── ASP.NET Identity ─────────────────────────────────────────────
            builder.Services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<Microsoft.AspNetCore.Identity.IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

            // ── Repositories ─────────────────────────────────────────────────
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ── Services ─────────────────────────────────────────────────────
            builder.Services.AddScoped<IAuthenticationService, SqliteAuthenticationService>();
            builder.Services.AddScoped<IEventService, SqliteEventService>();
            builder.Services.AddScoped<IPollService, SqlitePollService>();
            builder.Services.AddScoped<ITicketService, SqliteTicketService>();
            builder.Services.AddSingleton<IPaymentService, PaystackPaymentService>();
            builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<IEmailService, EmailService>();
            builder.Services.AddSingleton<ITwoFactorService, TwoFactorService>();
            builder.Services.AddScoped<TicketExpiryService>();

            // ── ViewModels ────────────────────────────────────────────────────
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<EventsViewModel>();
            builder.Services.AddTransient<TicketsViewModel>();
            builder.Services.AddTransient<PollsViewModel>();
            builder.Services.AddTransient<PollDetailsViewModel>();
            builder.Services.AddTransient<ProfileViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();

            // ── Pages — Auth ──────────────────────────────────────────────────
            builder.Services.AddTransient<Views.Auth.LoginPage>();
            builder.Services.AddTransient<Views.Auth.RegisterPage>();

            // ── Pages — Main ──────────────────────────────────────────────────
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<Views.Events.EventsPage>();
            builder.Services.AddTransient<Views.Events.EventDetailsPage>();
            builder.Services.AddTransient<Views.Events.CreateEventPage>();
            builder.Services.AddTransient<Views.Tickets.TicketsPage>();
            builder.Services.AddTransient<Views.Tickets.TicketDetailsPage>();
            builder.Services.AddTransient<Views.Tickets.QRScannerPage>();
            builder.Services.AddTransient<Views.Polls.PollsPage>();
            builder.Services.AddTransient<Views.Polls.CreatePollPage>();
            builder.Services.AddTransient<Views.Polls.PollDetailsPage>();
            builder.Services.AddTransient<Views.Profile.ProfilePage>();
            builder.Services.AddTransient<Views.Profile.EditProfilePage>();
            builder.Services.AddTransient<Views.Profile.SettingsPage>();

            // ── Logging ───────────────────────────────────────────────────────
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(dispose: true);
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        /// <summary>
        /// Retrieves the per-device database encryption key from SecureStorage,
        /// generating and persisting a new one on first launch.
        /// The key never leaves the device — it is not derived from user credentials.
        /// </summary>
        private static async Task<string> GetOrCreateDbKeyAsync()
        {
            const string keyName = "eventhub_db_key";
            try
            {
                var existing = await SecureStorage.GetAsync(keyName);
                if (!string.IsNullOrEmpty(existing))
                    return existing;

                var newKey = Convert.ToBase64String(
                    System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
                await SecureStorage.SetAsync(keyName, newKey);
                return newKey;
            }
            catch
            {
                // SecureStorage unavailable on some platforms in tests — fall back to no encryption
                return string.Empty;
            }
        }
    }
}
