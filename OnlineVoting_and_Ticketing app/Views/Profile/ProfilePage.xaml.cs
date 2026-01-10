using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app.Views.Profile
{
    public partial class ProfilePage : ContentPage
    {
        private readonly IAuthenticationService _authService;

        public ProfilePage(IAuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadProfileData();
        }

        private void LoadProfileData()
        {
            var userName = Preferences.Get(AppConstants.Preferences.UserName, "Guest User");
            var userEmail = Preferences.Get(AppConstants.Preferences.UserEmail, "guest@eventhub.com");

            UserNameLabel.Text = userName;
            UserEmailLabel.Text = userEmail;

            EventsCountLabel.Text = "0";
            TicketsCountLabel.Text = "0";
            VotesCountLabel.Text = "0";
        }

        private async void OnEditProfileTapped(object? sender, EventArgs e)
        {
            await DisplayAlert("Edit Profile", "Profile editing feature coming soon!", "OK");
        }

        private async void OnMyTicketsTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//tickets");
        }

        private async void OnMyEventsTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//events");
        }

        private async void OnSettingsTapped(object? sender, EventArgs e)
        {
            await DisplayAlert("Settings", "Settings feature coming soon!", "OK");
        }

        private async void OnLogoutClicked(object? sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");

            if (!confirm)
                return;

            var success = await _authService.LogoutAsync();

            if (success)
            {
                await DisplayAlert("Success", AppConstants.Messages.LogoutSuccess, "OK");
                await Shell.Current.GoToAsync("//login");
            }
            else
            {
                await DisplayAlert("Error", "Failed to logout. Please try again.", "OK");
            }
        }
    }
}
