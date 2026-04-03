using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Services;
using QRCoder;

namespace OnlineVoting_and_Ticketing_app.ViewModels.Profile
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly ITwoFactorService _twoFactor;
        private readonly IEmailService _emailService;
        private readonly IAuthenticationService _authService;
        private readonly INavigationService _navigation;
        private readonly ILogger<SettingsViewModel> _logger;

        private const string DarkModeKey = "settings_dark_mode";
        private const string PushNotificationsKey = "settings_push_notifications";
        private const string EmailNotificationsKey = "settings_email_notifications";
        private const string EventRemindersKey = "settings_event_reminders";
        private const string PublicProfileKey = "settings_public_profile";

        [ObservableProperty] private bool _isDarkMode;
        [ObservableProperty] private bool _isPushEnabled;
        [ObservableProperty] private bool _isEmailEnabled;
        [ObservableProperty] private bool _isEventRemindersEnabled;
        [ObservableProperty] private bool _isPublicProfile;

        // 2FA state
        [ObservableProperty] private bool _is2FAEnabled;
        [ObservableProperty] private bool _isSetup2FAVisible;
        [ObservableProperty] private string _totpQrCodeImage = string.Empty;
        [ObservableProperty] private string _totpSetupKey = string.Empty;
        [ObservableProperty] private string _totpVerificationCode = string.Empty;

        [ObservableProperty] private string _appVersion = AppConstants.AppVersion;
        [ObservableProperty] private string _cacheSizeText = "Calculating...";

        public SettingsViewModel(
            ITwoFactorService twoFactor,
            IEmailService emailService,
            IAuthenticationService authService,
            INavigationService navigation,
            ILogger<SettingsViewModel> logger)
        {
            _twoFactor = twoFactor;
            _emailService = emailService;
            _authService = authService;
            _navigation = navigation;
            _logger = logger;

            Title = "Settings";
        }

        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            IsDarkMode = Preferences.Get(DarkModeKey, true);
            IsPushEnabled = Preferences.Get(PushNotificationsKey, true);
            IsEmailEnabled = Preferences.Get(EmailNotificationsKey, true);
            IsEventRemindersEnabled = Preferences.Get(EventRemindersKey, true);
            IsPublicProfile = Preferences.Get(PublicProfileKey, true);

            CalculateCacheSize();

            var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
            if (!string.IsNullOrEmpty(userId))
                Is2FAEnabled = await _twoFactor.IsEnabledAsync(userId);
        }

        [RelayCommand]
        private void ToggleDarkMode(bool value)
        {
            Preferences.Set(DarkModeKey, value);
            Application.Current!.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
        }

        [RelayCommand]
        private void TogglePushNotifications(bool value) =>
            Preferences.Set(PushNotificationsKey, value);

        [RelayCommand]
        private void ToggleEmailNotifications(bool value) =>
            Preferences.Set(EmailNotificationsKey, value);

        [RelayCommand]
        private void ToggleEventReminders(bool value) =>
            Preferences.Set(EventRemindersKey, value);

        [RelayCommand]
        private void TogglePublicProfile(bool value) =>
            Preferences.Set(PublicProfileKey, value);

        /// <summary>
        /// Begins 2FA setup: generates a new TOTP secret, creates QR code URI,
        /// and renders it as a PNG so the user can scan with their authenticator app.
        /// </summary>
        [RelayCommand]
        private async Task Setup2FAAsync()
        {
            var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
            var userEmail = await SecureStorage.GetAsync(AppConstants.Preferences.UserEmail);

            if (string.IsNullOrEmpty(userId)) return;

            var secret = _twoFactor.GenerateSecretKey();
            var otpUri = _twoFactor.GetOtpAuthUri(secret, userEmail ?? "user", "EventHub");

            // Generate QR code image from the otpauth:// URI
            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(otpUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            var png = qrCode.GetGraphic(10);
            TotpQrCodeImage = $"data:image/png;base64,{Convert.ToBase64String(png)}";
            TotpSetupKey = secret;

            IsSetup2FAVisible = true;

            // Persist secret temporarily — only confirmed after code verification
            await SecureStorage.SetAsync("2fa_pending_secret", secret);
        }

        /// <summary>
        /// Validates the user-entered TOTP code against the pending setup secret.
        /// Only saves permanently if validation passes.
        /// </summary>
        [RelayCommand]
        private async Task Verify2FASetupAsync()
        {
            var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
            var pendingSecret = await SecureStorage.GetAsync("2fa_pending_secret");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(pendingSecret))
            {
                SetError("Setup session expired. Please try again.");
                return;
            }

            if (string.IsNullOrWhiteSpace(TotpVerificationCode) || TotpVerificationCode.Length != 6)
            {
                SetError("Enter the 6-digit code from your authenticator app.");
                return;
            }

            if (!_twoFactor.ValidateCode(pendingSecret, TotpVerificationCode))
            {
                SetError("Invalid code. Please try again.");
                return;
            }

            await _twoFactor.SaveSecretAsync(userId, pendingSecret);
            SecureStorage.Remove("2fa_pending_secret");

            Is2FAEnabled = true;
            IsSetup2FAVisible = false;
            TotpVerificationCode = string.Empty;
            ClearError();

            // Send confirmation email
            var user = await _authService.GetCurrentUserAsync();
            if (user != null)
                _ = _emailService.SendTwoFactorSetupEmailAsync(user.Email, user.FullName);

            await _navigation.DisplayAlertAsync("2FA Enabled",
                "Two-Factor Authentication is now active on your account.", "OK");
        }

        [RelayCommand]
        private async Task Disable2FAAsync()
        {
            var confirm = await _navigation.DisplayConfirmAsync(
                "Disable 2FA",
                "Are you sure? This will make your account less secure.",
                "Disable", "Cancel");

            if (!confirm) return;

            var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
            if (string.IsNullOrEmpty(userId)) return;

            await _twoFactor.DisableAsync(userId);
            Is2FAEnabled = false;
            IsSetup2FAVisible = false;

            await _navigation.DisplayAlertAsync("2FA Disabled",
                "Two-Factor Authentication has been disabled.", "OK");
        }

        [RelayCommand]
        private async Task ClearCacheAsync()
        {
            var confirm = await _navigation.DisplayConfirmAsync(
                "Clear Cache", "This will remove all cached data. Continue?", "Yes", "Cancel");

            if (!confirm) return;

            try
            {
                var cacheDir = FileSystem.CacheDirectory;
                if (Directory.Exists(cacheDir))
                {
                    foreach (var file in Directory.GetFiles(cacheDir, "*", SearchOption.AllDirectories))
                    {
                        try { File.Delete(file); } catch { /* skip locked files */ }
                    }
                }
                CalculateCacheSize();
                await _navigation.DisplayAlertAsync("Success", "Cache cleared successfully.", "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClearCacheAsync failed");
                await _navigation.DisplayAlertAsync("Error", $"Failed to clear cache: {ex.Message}", "OK");
            }
        }

        private void CalculateCacheSize()
        {
            try
            {
                var cacheDir = FileSystem.CacheDirectory;
                if (!Directory.Exists(cacheDir)) { CacheSizeText = "0 MB used"; return; }
                long size = Directory.GetFiles(cacheDir, "*", SearchOption.AllDirectories)
                    .Sum(f => new FileInfo(f).Length);
                CacheSizeText = $"{size / (1024.0 * 1024.0):F1} MB used";
            }
            catch
            {
                CacheSizeText = "Unable to calculate";
            }
        }
    }
}
