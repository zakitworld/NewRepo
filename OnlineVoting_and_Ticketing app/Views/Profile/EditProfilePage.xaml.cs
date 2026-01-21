using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app.Views.Profile
{
    public partial class EditProfilePage : ContentPage
    {
        private readonly IAuthenticationService _authService;
        private string _selectedImagePath = string.Empty;

        public EditProfilePage(IAuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadCurrentProfileData();
        }

        private async Task LoadCurrentProfileData()
        {
            try
            {
                LoadingOverlay.IsVisible = true;

                var user = await _authService.GetCurrentUserAsync();

                if (user != null)
                {
                    FullNameEntry.Text = user.FullName;
                    EmailEntry.Text = user.Email;
                    PhoneEntry.Text = user.PhoneNumber;

                    if (!string.IsNullOrEmpty(user.ProfileImageUrl))
                    {
                        ProfileImagePreview.Source = user.ProfileImageUrl;
                        ProfileImagePreview.IsVisible = true;
                        ProfileImagePlaceholder.IsVisible = false;
                        _selectedImagePath = user.ProfileImageUrl;
                    }
                }
                else
                {
                    // Fall back to SecureStorage
                    FullNameEntry.Text = await SecureStorage.GetAsync(AppConstants.Preferences.UserName) ?? string.Empty;
                    EmailEntry.Text = await SecureStorage.GetAsync(AppConstants.Preferences.UserEmail) ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load profile: {ex.Message}");
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
            }
        }

        private async void OnSelectImageTapped(object? sender, EventArgs e)
        {
            try
            {
                var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Select Profile Photo"
                });

                if (result != null)
                {
                    var stream = await result.OpenReadAsync();
                    ProfileImagePreview.Source = ImageSource.FromStream(() => stream);
                    ProfileImagePreview.IsVisible = true;
                    ProfileImagePlaceholder.IsVisible = false;
                    _selectedImagePath = result.FullPath;
                }
            }
            catch (PermissionException)
            {
                await DisplayAlert("Permission Required", "Please grant photo access permission to change your profile picture.", "OK");
            }
            catch (Exception ex)
            {
                ShowError($"Failed to select image: {ex.Message}");
            }
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            HideMessages();

            // Validate full name
            if (string.IsNullOrWhiteSpace(FullNameEntry.Text))
            {
                ShowError("Full name is required");
                return;
            }

            // Validate password change if attempting to change
            var isChangingPassword = !string.IsNullOrEmpty(NewPasswordEntry.Text);
            if (isChangingPassword)
            {
                if (string.IsNullOrEmpty(CurrentPasswordEntry.Text))
                {
                    ShowError("Current password is required to change password");
                    return;
                }

                if (NewPasswordEntry.Text.Length < 8)
                {
                    ShowError("New password must be at least 8 characters");
                    return;
                }

                if (NewPasswordEntry.Text != ConfirmPasswordEntry.Text)
                {
                    ShowError("New passwords do not match");
                    return;
                }
            }

            SaveButton.IsEnabled = false;
            SaveButton.Text = "Saving...";
            LoadingOverlay.IsVisible = true;

            try
            {
                var (success, error) = await _authService.UpdateProfileAsync(
                    FullNameEntry.Text.Trim(),
                    PhoneEntry.Text?.Trim(),
                    _selectedImagePath,
                    isChangingPassword ? CurrentPasswordEntry.Text : null,
                    isChangingPassword ? NewPasswordEntry.Text : null
                );

                if (success)
                {
                    // Update local storage
                    await SecureStorage.SetAsync(AppConstants.Preferences.UserName, FullNameEntry.Text.Trim());

                    ShowSuccess("Profile updated successfully!");

                    // Clear password fields
                    CurrentPasswordEntry.Text = string.Empty;
                    NewPasswordEntry.Text = string.Empty;
                    ConfirmPasswordEntry.Text = string.Empty;

                    // Navigate back after a short delay
                    await Task.Delay(1500);
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    ShowError(error ?? "Failed to update profile");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
            finally
            {
                SaveButton.IsEnabled = true;
                SaveButton.Text = "SAVE CHANGES";
                LoadingOverlay.IsVisible = false;
            }
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private void ShowError(string message)
        {
            ErrorLabel.Text = message;
            ErrorLabel.IsVisible = true;
            SuccessLabel.IsVisible = false;
        }

        private void ShowSuccess(string message)
        {
            SuccessLabel.Text = message;
            SuccessLabel.IsVisible = true;
            ErrorLabel.IsVisible = false;
        }

        private void HideMessages()
        {
            ErrorLabel.IsVisible = false;
            SuccessLabel.IsVisible = false;
        }
    }
}
