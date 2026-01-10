using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Helpers;
using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app.Views.Auth
{
    public partial class LoginPage : ContentPage
    {
        private readonly IAuthenticationService _authService;

        public LoginPage(IAuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        private async void OnLoginClicked(object? sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;

            var email = EmailEntry.Text?.Trim();
            var password = PasswordEntry.Text;

            var validation = ValidationHelper.ValidateLogin(email ?? string.Empty, password ?? string.Empty);
            if (!validation.IsValid)
            {
                ErrorLabel.Text = validation.ErrorMessage;
                ErrorLabel.IsVisible = true;
                return;
            }

            LoginButton.IsEnabled = false;
            LoginButton.Text = "Signing in...";

            var (success, error, user) = await _authService.LoginWithEmailPasswordAsync(email!, password!);

            if (success && user != null)
            {
                await DisplayAlert("Success", AppConstants.Messages.LoginSuccess, "OK");
                await Shell.Current.GoToAsync($"//{AppConstants.Routes.Home}");
            }
            else
            {
                ErrorLabel.Text = error ?? AppConstants.Messages.UnknownError;
                ErrorLabel.IsVisible = true;
            }

            LoginButton.IsEnabled = true;
            LoginButton.Text = "Sign In";
        }

        private async void OnGoogleLoginTapped(object? sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;
            LoginButton.IsEnabled = false;

            var (success, error, user) = await _authService.LoginWithGoogleAsync();

            if (success && user != null)
            {
                await DisplayAlert("Success", AppConstants.Messages.LoginSuccess, "OK");
                await Shell.Current.GoToAsync($"//{AppConstants.Routes.Home}");
            }
            else
            {
                ErrorLabel.Text = error ?? AppConstants.Messages.UnknownError;
                ErrorLabel.IsVisible = true;
            }

            LoginButton.IsEnabled = true;
        }

        private async void OnFacebookLoginTapped(object? sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;
            LoginButton.IsEnabled = false;

            var (success, error, user) = await _authService.LoginWithFacebookAsync();

            if (success && user != null)
            {
                await DisplayAlert("Success", AppConstants.Messages.LoginSuccess, "OK");
                await Shell.Current.GoToAsync($"//{AppConstants.Routes.Home}");
            }
            else
            {
                ErrorLabel.Text = error ?? AppConstants.Messages.UnknownError;
                ErrorLabel.IsVisible = true;
            }

            LoginButton.IsEnabled = true;
        }

        private async void OnForgotPasswordTapped(object? sender, EventArgs e)
        {
            var email = await DisplayPromptAsync("Forgot Password", "Enter your email address:", "Send", "Cancel", keyboard: Keyboard.Email);

            if (string.IsNullOrWhiteSpace(email))
                return;

            if (!ValidationHelper.IsValidEmail(email))
            {
                await DisplayAlert("Error", "Please enter a valid email address", "OK");
                return;
            }

            var success = await _authService.SendPasswordResetEmailAsync(email);

            if (success)
            {
                await DisplayAlert("Success", "Password reset email sent! Please check your inbox.", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Failed to send password reset email. Please try again.", "OK");
            }
        }

        private async void OnSignUpTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(AppConstants.Routes.Register);
        }
    }
}
