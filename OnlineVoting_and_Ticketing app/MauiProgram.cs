using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using OnlineVoting_and_Ticketing_app.Services;
using OnlineVoting_and_Ticketing_app.Data;
using SkiaSharp.Views.Maui.Controls.Hosting;
using FFImageLoading.Maui;

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
                .UseFFImageLoading()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Database Configuration
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "eventhub.db");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Identity Configuration (using IdentityCore for MAUI)
            builder.Services.AddIdentityCore<ApplicationUser>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;

                // User settings
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

            // Services
            builder.Services.AddScoped<IAuthenticationService, SqliteAuthenticationService>();
            builder.Services.AddScoped<IEventService, SqliteEventService>();
            builder.Services.AddScoped<IPollService, SqlitePollService>();
            builder.Services.AddScoped<ITicketService, SqliteTicketService>();
            builder.Services.AddSingleton<IPaymentService, PaystackPaymentService>();

            // Pages - Auth
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Auth.LoginPage>();
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Auth.RegisterPage>();

            // Pages - Main
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Events.EventsPage>();
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Events.EventDetailsPage>();
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Events.CreateEventPage>();
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Tickets.TicketsPage>();
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Tickets.TicketDetailsPage>();
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Polls.PollsPage>();
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Polls.CreatePollPage>();
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Polls.PollDetailsPage>();
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Profile.ProfilePage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
