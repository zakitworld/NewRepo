using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Helpers;
using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app.Views.Auth
{
    public partial class RegisterPage : ContentPage
    {
        private readonly IAuthenticationService _authService;

        public RegisterPage(IAuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        private async void OnRegisterClicked(object? sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;

            var fullName = FullNameEntry.Text?.Trim();
            var email = EmailEntry.Text?.Trim();
            var password = PasswordEntry.Text;
            var confirmPassword = ConfirmPasswordEntry.Text;

            if (password != confirmPassword)
            {
                ErrorLabel.Text = "Passwords do not match";
                ErrorLabel.IsVisible = true;
                return;
            }

            var validation = ValidationHelper.ValidateRegistration(email ?? string.Empty, password ?? string.Empty, fullName ?? string.Empty);
            if (!validation.IsValid)
            {
                ErrorLabel.Text = validation.ErrorMessage;
                ErrorLabel.IsVisible = true;
                return;
            }

            RegisterButton.IsEnabled = false;
            RegisterButton.Text = "Creating account...";

            var (success, error, user) = await _authService.RegisterWithEmailPasswordAsync(email!, password!, fullName!);

            if (success && user != null)
            {
                await DisplayAlertAsync("Success", AppConstants.Messages.RegistrationSuccess, "OK");
                await Shell.Current.GoToAsync($"//{AppConstants.Routes.Home}");
            }
            else
            {
                ErrorLabel.Text = error ?? AppConstants.Messages.UnknownError;
                ErrorLabel.IsVisible = true;
            }

            RegisterButton.IsEnabled = true;
            RegisterButton.Text = "Create Account";
        }

        private async void OnGoogleSignUpTapped(object? sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;
            RegisterButton.IsEnabled = false;

            var (success, error, user) = await _authService.LoginWithGoogleAsync();

            if (success && user != null)
            {
                await DisplayAlertAsync("Success", AppConstants.Messages.RegistrationSuccess, "OK");
                await Shell.Current.GoToAsync($"//{AppConstants.Routes.Home}");
            }
            else
            {
                ErrorLabel.Text = error ?? AppConstants.Messages.UnknownError;
                ErrorLabel.IsVisible = true;
            }

            RegisterButton.IsEnabled = true;
        }

        private async void OnFacebookSignUpTapped(object? sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;
            RegisterButton.IsEnabled = false;

            var (success, error, user) = await _authService.LoginWithFacebookAsync();

            if (success && user != null)
            {
                await DisplayAlertAsync("Success", AppConstants.Messages.RegistrationSuccess, "OK");
                await Shell.Current.GoToAsync($"//{AppConstants.Routes.Home}");
            }
            else
            {
                ErrorLabel.Text = error ?? AppConstants.Messages.UnknownError;
                ErrorLabel.IsVisible = true;
            }

            RegisterButton.IsEnabled = true;
        }

        private async void OnSignInTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
