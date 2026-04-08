using OnlineVoting_and_Ticketing_app.ViewModels.Profile;

namespace OnlineVoting_and_Ticketing_app.Views.Profile
{
    public partial class SettingsPage : ContentPage
    {
        private readonly SettingsViewModel _viewModel;
        private bool _suppressToggles; // prevent re-entrant command calls while syncing UI

        public SettingsPage(SettingsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadSettingsCommand.ExecuteAsync(null);
            SyncSwitchStates();
            CacheSizeLabel.Text = _viewModel.CacheSizeText;
            VersionLabel.Text = _viewModel.AppVersion;
        }

        // Sync Switch UI state from ViewModel after settings are loaded
        private void SyncSwitchStates()
        {
            _suppressToggles = true;
            DarkModeSwitch.IsToggled            = _viewModel.IsDarkMode;
            PushNotificationsSwitch.IsToggled   = _viewModel.IsPushEnabled;
            EmailNotificationsSwitch.IsToggled  = _viewModel.IsEmailEnabled;
            EventRemindersSwitch.IsToggled      = _viewModel.IsEventRemindersEnabled;
            PublicProfileSwitch.IsToggled       = _viewModel.IsPublicProfile;
            _suppressToggles = false;
        }

        private void OnDarkModeToggled(object? sender, ToggledEventArgs e)
        {
            if (_suppressToggles) return;
            _viewModel.ToggleDarkModeCommand.Execute(e.Value);
        }

        private void OnAccentColorTapped(object? sender, TappedEventArgs e)
        {
            AccentColorOptions.IsVisible = !AccentColorOptions.IsVisible;
        }

        private void OnColorSelected(object? sender, TappedEventArgs e)
        {
            var hex = e.Parameter as string;
            if (string.IsNullOrEmpty(hex)) return;

            try
            {
                var color = Color.FromArgb(hex);
                AccentColorPreview.Color = color;
                Preferences.Set("settings_accent_color", hex);

                // Apply accent color to app resources at runtime
                if (Application.Current?.Resources != null)
                {
                    Application.Current.Resources["Primary"] = color;
                    var brush = new SolidColorBrush(color);
                    Application.Current.Resources["PrimaryBrush"] = brush;
                }
            }
            catch { /* invalid hex — ignore */ }

            AccentColorOptions.IsVisible = false;
        }

        private void OnNotificationSettingChanged(object? sender, ToggledEventArgs e)
        {
            if (_suppressToggles) return;

            if (sender == PushNotificationsSwitch)
                _viewModel.TogglePushNotificationsCommand.Execute(e.Value);
            else if (sender == EmailNotificationsSwitch)
                _viewModel.ToggleEmailNotificationsCommand.Execute(e.Value);
            else if (sender == EventRemindersSwitch)
                _viewModel.ToggleEventRemindersCommand.Execute(e.Value);
        }

        private void OnPrivacySettingChanged(object? sender, ToggledEventArgs e)
        {
            if (_suppressToggles) return;

            if (sender == PublicProfileSwitch)
                _viewModel.TogglePublicProfileCommand.Execute(e.Value);
            // ShowVotingActivitySwitch — save directly via Preferences
            else if (sender == ShowVotingActivitySwitch)
                Preferences.Set("settings_show_voting_activity", e.Value);
        }

        private async void OnClearCacheTapped(object? sender, TappedEventArgs e)
        {
            await _viewModel.ClearCacheCommand.ExecuteAsync(null);
            CacheSizeLabel.Text = _viewModel.CacheSizeText;
        }

        private async void OnTermsTapped(object? sender, TappedEventArgs e)
        {
            await Browser.OpenAsync("https://eventhub.app/terms", BrowserLaunchMode.SystemPreferred);
        }

        private async void OnPrivacyPolicyTapped(object? sender, TappedEventArgs e)
        {
            await Browser.OpenAsync("https://eventhub.app/privacy", BrowserLaunchMode.SystemPreferred);
        }
    }
}
