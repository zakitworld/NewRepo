namespace OnlineVoting_and_Ticketing_app.Constants
{
    public static class AppConstants
    {
        public const string AppName = "EventHub";
        public const string AppVersion = "1.0.0";

        public static class Firebase
        {
            public const string CollectionUsers = "users";
            public const string CollectionEvents = "events";
            public const string CollectionTickets = "tickets";
            public const string CollectionPolls = "polls";
            public const string CollectionVotes = "votes";
            public const string CollectionTransactions = "transactions";
        }

        public static class Routes
        {
            public const string Splash = "splash";
            public const string Login = "login";
            public const string Register = "register";
            public const string Home = "home";
            public const string Events = "events";
            public const string EventDetails = "eventdetails";
            public const string Tickets = "tickets";
            public const string TicketDetails = "ticketdetails";
            public const string Polls = "polls";
            public const string PollDetails = "polldetails";
            public const string Profile = "profile";
            public const string Settings = "settings";
            public const string CreateEvent = "createevent";
            public const string CreatePoll = "createpoll";
        }

        public static class Preferences
        {
            public const string UserId = "user_id";
            public const string UserEmail = "user_email";
            public const string UserName = "user_name";
            public const string IsLoggedIn = "is_logged_in";
            public const string UserRole = "user_role";
            public const string HasSeenOnboarding = "has_seen_onboarding";
        }

        public static class Messages
        {
            public const string NetworkError = "Network error. Please check your connection.";
            public const string UnknownError = "An unknown error occurred. Please try again.";
            public const string LoginSuccess = "Login successful!";
            public const string LogoutSuccess = "Logged out successfully.";
            public const string RegistrationSuccess = "Registration successful!";
            public const string EventCreatedSuccess = "Event created successfully!";
            public const string TicketPurchasedSuccess = "Ticket purchased successfully!";
            public const string VoteSubmittedSuccess = "Vote submitted successfully!";
        }
    }
}
