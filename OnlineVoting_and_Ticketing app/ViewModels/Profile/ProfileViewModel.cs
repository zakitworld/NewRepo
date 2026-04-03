using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app.ViewModels.Profile
{
    public partial class ProfileViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;
        private readonly ITicketService _ticketService;
        private readonly INavigationService _navigation;
        private readonly ILogger<ProfileViewModel> _logger;

        [ObservableProperty] private string _userName = "Guest User";
        [ObservableProperty] private string _userEmail = string.Empty;
        [ObservableProperty] private string _profileImageUrl = string.Empty;
        [ObservableProperty] private int _ticketCount;
        [ObservableProperty] private int _eventsCount;
        [ObservableProperty] private int _votesCount;

        public ProfileViewModel(
            IAuthenticationService authService,
            ITicketService ticketService,
            INavigationService navigation,
            ILogger<ProfileViewModel> logger)
        {
            _authService = authService;
            _ticketService = ticketService;
            _navigation = navigation;
            _logger = logger;

            Title = "Profile";
        }

        [RelayCommand]
        private async Task LoadProfileAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user != null)
                {
                    UserName = user.FullName;
                    UserEmail = user.Email;
                    ProfileImageUrl = user.ProfileImageUrl ?? string.Empty;

                    TicketCount = await _ticketService.GetUserTicketCountAsync(user.Id);
                }
                else
                {
                    UserName = await SecureStorage.GetAsync(AppConstants.Preferences.UserName) ?? "Guest User";
                    UserEmail = await SecureStorage.GetAsync(AppConstants.Preferences.UserEmail) ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadProfileAsync failed");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task EditProfileAsync() =>
            await _navigation.GoToAsync("editprofile");

        [RelayCommand]
        private async Task GoToTicketsAsync() =>
            await _navigation.GoToRootAsync("main/tickets");

        [RelayCommand]
        private async Task GoToEventsAsync() =>
            await _navigation.GoToRootAsync("main/events");

        [RelayCommand]
        private async Task GoToSettingsAsync() =>
            await _navigation.GoToAsync("settings");

        [RelayCommand]
        private async Task LogoutAsync()
        {
            var confirm = await _navigation.DisplayConfirmAsync(
                "Logout", "Are you sure you want to logout?", "Yes", "No");

            if (!confirm) return;

            IsBusy = true;
            try
            {
                var success = await _authService.LogoutAsync();
                if (success)
                    await _navigation.GoToRootAsync("login");
                else
                    await _navigation.DisplayAlertAsync("Error", "Failed to logout. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
