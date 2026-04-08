using OnlineVoting_and_Ticketing_app.ViewModels.Profile;

namespace OnlineVoting_and_Ticketing_app.Views.Profile
{
    public partial class ProfilePage : ContentPage
    {
        private readonly ProfileViewModel _viewModel;

        public ProfilePage(ProfileViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadProfileCommand.ExecuteAsync(null);
        }

        private async void OnEditProfileTapped(object? sender, TappedEventArgs e)
            => await _viewModel.EditProfileCommand.ExecuteAsync(null);

        private async void OnMyTicketsTapped(object? sender, TappedEventArgs e)
            => await _viewModel.GoToTicketsCommand.ExecuteAsync(null);

        private async void OnMyEventsTapped(object? sender, TappedEventArgs e)
            => await _viewModel.GoToEventsCommand.ExecuteAsync(null);

        private async void OnSettingsTapped(object? sender, TappedEventArgs e)
            => await _viewModel.GoToSettingsCommand.ExecuteAsync(null);

        private async void OnLogoutClicked(object? sender, EventArgs e)
            => await _viewModel.LogoutCommand.ExecuteAsync(null);
    }
}
