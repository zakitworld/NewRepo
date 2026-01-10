using OnlineVoting_and_Ticketing_app.Constants;

namespace OnlineVoting_and_Ticketing_app
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            var userName = Preferences.Get(AppConstants.Preferences.UserName, "Guest");
            WelcomeLabel.Text = $"Welcome, {userName}!";
        }

        private async void OnEventsCardTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//events");
        }

        private async void OnPollsCardTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//polls");
        }

        private async void OnTicketsCardTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//tickets");
        }

        private async void OnProfileCardTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//profile");
        }
    }
}
