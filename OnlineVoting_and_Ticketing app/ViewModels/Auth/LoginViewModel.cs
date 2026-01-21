using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Helpers;
using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app.ViewModels.Auth
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        public LoginViewModel(IAuthenticationService authService)
        {
            _authService = authService;
            Title = "Sign In";
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (IsBusy) return;

            HasError = false;
            ErrorMessage = string.Empty;

            var validation = ValidationHelper.ValidateLogin(Email, Password);
            if (!validation.IsValid)
            {
                ErrorMessage = validation.ErrorMessage ?? "Invalid login credentials";
                HasError = true;
                return;
            }

            IsBusy = true;

            try
            {
                var (success, error, user) = await _authService.LoginWithEmailPasswordAsync(Email, Password);

                if (success && user != null)
                {
                    await Shell.Current.GoToAsync("//main/home");
                }
                else
                {
                    ErrorMessage = error ?? AppConstants.Messages.UnknownError;
                    HasError = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login failed: {ex.Message}";
                HasError = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GoogleLoginAsync()
        {
            if (IsBusy) return;

            HasError = false;
            IsBusy = true;

            try
            {
                var (success, error, user) = await _authService.LoginWithGoogleAsync();
                if (success && user != null)
                {
                    await Shell.Current.GoToAsync("//main/home");
                }
                else
                {
                    ErrorMessage = error ?? AppConstants.Messages.UnknownError;
                    HasError = true;
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task FacebookLoginAsync()
        {
            if (IsBusy) return;

            HasError = false;
            IsBusy = true;

            try
            {
                var (success, error, user) = await _authService.LoginWithFacebookAsync();
                if (success && user != null)
                {
                    await Shell.Current.GoToAsync("//main/home");
                }
                else
                {
                    ErrorMessage = error ?? AppConstants.Messages.UnknownError;
                    HasError = true;
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ForgotPasswordAsync()
        {
            var email = await Shell.Current.DisplayPromptAsync("Forgot Password", "Enter your email address:", "Send", "Cancel", keyboard: Keyboard.Email);

            if (string.IsNullOrWhiteSpace(email))
                return;

            if (!ValidationHelper.IsValidEmail(email))
            {
                await Shell.Current.DisplayAlertAsync("Error", "Please enter a valid email address", "OK");
                return;
            }

            var success = await _authService.SendPasswordResetEmailAsync(email);

            if (success)
            {
                await Shell.Current.DisplayAlertAsync("Success", "Password reset email sent! Please check your inbox.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Error", "Failed to send password reset email. Please try again.", "OK");
            }
        }

        [RelayCommand]
        private async Task SignUpAsync()
        {
            await Shell.Current.GoToAsync(AppConstants.Routes.Register);
        }
    }
}
