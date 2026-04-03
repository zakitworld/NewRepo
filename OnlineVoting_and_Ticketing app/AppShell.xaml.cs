using OnlineVoting_and_Ticketing_app.Views.Auth;
using OnlineVoting_and_Ticketing_app.Views.Events;
using OnlineVoting_and_Ticketing_app.Views.Tickets;
using OnlineVoting_and_Ticketing_app.Views.Polls;

namespace OnlineVoting_and_Ticketing_app
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Auth Routes
            // "login" is already declared as Route="login" in AppShell.xaml — do NOT re-register it here.
            Routing.RegisterRoute("register", typeof(RegisterPage));

            // Event Routes
            Routing.RegisterRoute("eventdetails", typeof(EventDetailsPage));
            Routing.RegisterRoute("createevent", typeof(CreateEventPage));

            // Ticket Routes
            Routing.RegisterRoute("ticketdetails", typeof(TicketDetailsPage));

            // Poll Routes
            Routing.RegisterRoute("polldetails", typeof(PollDetailsPage));
            Routing.RegisterRoute("createpoll", typeof(CreatePollPage));

            // Profile Routes
            Routing.RegisterRoute("editprofile", typeof(OnlineVoting_and_Ticketing_app.Views.Profile.EditProfilePage));
            Routing.RegisterRoute("settings", typeof(OnlineVoting_and_Ticketing_app.Views.Profile.SettingsPage));

            // QR Scanner
            Routing.RegisterRoute("qrscanner", typeof(OnlineVoting_and_Ticketing_app.Views.Tickets.QRScannerPage));
        }
    }
}
