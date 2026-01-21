using OnlineVoting_and_Ticketing_app.Constants;

namespace OnlineVoting_and_Ticketing_app.Views.Profile
{
    public partial class SettingsPage : ContentPage
    {
        // Settings keys
        private const string DarkModeKey = "settings_dark_mode";
        private const string AccentColorKey = "settings_accent_color";
        private const string PushNotificationsKey = "settings_push_notifications";
        private const string EmailNotificationsKey = "settings_email_notifications";
        private const string EventRemindersKey = "settings_event_reminders";
        private const string PublicProfileKey = "settings_public_profile";
        private const string ShowVotingActivityKey = "settings_show_voting_activity";

        public SettingsPage()
        {
            InitializeComponent();
            VersionLabel.Text = AppConstants.AppVersion;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadSettings();
            CalculateCacheSize();
        }

        private void LoadSettings()
        {
            // Load appearance settings
            DarkModeSwitch.IsToggled = Preferences.Get(DarkModeKey, true);
            var accentColor = Preferences.Get(AccentColorKey, "#8B5CF6");
            AccentColorPreview.Color = Color.FromArgb(accentColor);

            // Load notification settings
            PushNotificationsSwitch.IsToggled = Preferences.Get(PushNotificationsKey, true);
            EmailNotificationsSwitch.IsToggled = Preferences.Get(EmailNotificationsKey, true);
            EventRemindersSwitch.IsToggled = Preferences.Get(EventRemindersKey, true);

            // Load privacy settings
            PublicProfileSwitch.IsToggled = Preferences.Get(PublicProfileKey, true);
            ShowVotingActivitySwitch.IsToggled = Preferences.Get(ShowVotingActivityKey, false);
        }

        private void CalculateCacheSize()
        {
            try
            {
                var cacheDir = FileSystem.CacheDirectory;
                if (Directory.Exists(cacheDir))
                {
                    var size = GetDirectorySize(cacheDir);
                    var sizeInMB = size / (1024.0 * 1024.0);
                    CacheSizeLabel.Text = $"{sizeInMB:F1} MB used";
                }
                else
                {
                    CacheSizeLabel.Text = "0 MB used";
                }
            }
            catch
            {
                CacheSizeLabel.Text = "Unable to calculate";
            }
        }

        private long GetDirectorySize(string path)
        {
            long size = 0;
            try
            {
                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    size += fileInfo.Length;
                }
            }
            catch
            {
                // Ignore access errors
            }
            return size;
        }

        private void OnDarkModeToggled(object? sender, ToggledEventArgs e)
        {
            Preferences.Set(DarkModeKey, e.Value);
            // In a real app, you would apply the theme change here
            // Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
        }

        private void OnAccentColorTapped(object? sender, EventArgs e)
        {
            AccentColorOptions.IsVisible = !AccentColorOptions.IsVisible;
        }

        private void OnColorSelected(object? sender, EventArgs e)
        {
            if (sender is Border border && border.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap)
            {
                var colorHex = tap.CommandParameter?.ToString();
                if (!string.IsNullOrEmpty(colorHex))
                {
                    var color = Color.FromArgb(colorHex);
                    AccentColorPreview.Color = color;
                    Preferences.Set(AccentColorKey, colorHex);
                    AccentColorOptions.IsVisible = false;

                    // In a real app, you would update the app's primary color dynamically
                    // This would require custom theme handling
                }
            }
        }

        private void OnNotificationSettingChanged(object? sender, ToggledEventArgs e)
        {
            if (sender == PushNotificationsSwitch)
                Preferences.Set(PushNotificationsKey, e.Value);
            else if (sender == EmailNotificationsSwitch)
                Preferences.Set(EmailNotificationsKey, e.Value);
            else if (sender == EventRemindersSwitch)
                Preferences.Set(EventRemindersKey, e.Value);
        }

        private void OnPrivacySettingChanged(object? sender, ToggledEventArgs e)
        {
            if (sender == PublicProfileSwitch)
                Preferences.Set(PublicProfileKey, e.Value);
            else if (sender == ShowVotingActivitySwitch)
                Preferences.Set(ShowVotingActivityKey, e.Value);
        }

        private async void OnClearCacheTapped(object? sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Clear Cache", "This will remove all cached data. Continue?", "Yes", "Cancel");

            if (confirm)
            {
                try
                {
                    var cacheDir = FileSystem.CacheDirectory;
                    if (Directory.Exists(cacheDir))
                    {
                        var files = Directory.GetFiles(cacheDir, "*", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch
                            {
                                // Skip files that can't be deleted
                            }
                        }
                    }

                    CalculateCacheSize();
                    await DisplayAlert("Success", "Cache cleared successfully", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to clear cache: {ex.Message}", "OK");
                }
            }
        }

        private async void OnTermsTapped(object? sender, EventArgs e)
        {
            try
            {
                await Browser.OpenAsync("https://eventhub.app/terms", BrowserLaunchMode.SystemPreferred);
            }
            catch
            {
                await DisplayAlert("Error", "Unable to open Terms of Service", "OK");
            }
        }

        private async void OnPrivacyPolicyTapped(object? sender, EventArgs e)
        {
            try
            {
                await Browser.OpenAsync("https://eventhub.app/privacy", BrowserLaunchMode.SystemPreferred);
            }
            catch
            {
                await DisplayAlert("Error", "Unable to open Privacy Policy", "OK");
            }
        }
    }
}
