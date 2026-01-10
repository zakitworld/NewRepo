using Microsoft.Extensions.Logging;
using OnlineVoting_and_Ticketing_app.Services;
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

            // Services
            builder.Services.AddSingleton<IAuthenticationService, FirebaseAuthenticationService>();
            builder.Services.AddSingleton<IEventService, FirebaseEventService>();
            builder.Services.AddSingleton<IPollService, FirebasePollService>();
            builder.Services.AddSingleton<IPaymentService, PaystackPaymentService>();
            builder.Services.AddTransient<ITicketService, FirebaseTicketService>();

            // Pages - Auth
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Auth.LoginPage>();
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Auth.RegisterPage>();

            // Pages - Main
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Events.EventsPage>();
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Events.EventDetailsPage>();
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Tickets.TicketsPage>();
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Tickets.TicketDetailsPage>();
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Polls.PollsPage>();
            builder.Services.AddTransient<OnlineVoting_and_Ticketing_app.Views.Profile.ProfilePage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
