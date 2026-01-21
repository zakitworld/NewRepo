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
            _ = LoadProfileData();
        }

        private async Task LoadProfileData()
        {
            var userName = await SecureStorage.GetAsync(AppConstants.Preferences.UserName) ?? "Guest User";
            var userEmail = await SecureStorage.GetAsync(AppConstants.Preferences.UserEmail) ?? "guest@eventhub.com";

            UserNameLabel.Text = userName;
            UserEmailLabel.Text = userEmail;

            EventsCountLabel.Text = "0";
            TicketsCountLabel.Text = "0";
            VotesCountLabel.Text = "0";
        }

        private async void OnEditProfileTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("editprofile");
        }

        private async void OnMyTicketsTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//main/tickets");
        }

        private async void OnMyEventsTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//main/events");
        }

        private async void OnSettingsTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("settings");
        }

        private async void OnLogoutClicked(object? sender, EventArgs e)
        {
            var confirm = await DisplayAlertAsync("Logout", "Are you sure you want to logout?", "Yes", "No");

            if (!confirm)
                return;

            var success = await _authService.LogoutAsync();

            if (success)
            {
                await DisplayAlertAsync("Success", AppConstants.Messages.LogoutSuccess, "OK");
                await Shell.Current.GoToAsync("//login");
            }
            else
            {
                await DisplayAlertAsync("Error", "Failed to logout. Please try again.", "OK");
            }
        }
    }
}
