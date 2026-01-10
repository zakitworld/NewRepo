using Firebase.Auth;
using Firebase.Auth.Providers;
using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public class FirebaseAuthenticationService : IAuthenticationService
    {
        private readonly FirebaseAuthClient _authClient;
        private User? _currentUser;

        public FirebaseAuthenticationService()
        {
            var config = new FirebaseAuthConfig
            {
                ApiKey = FirebaseConfig.ApiKey,
                AuthDomain = FirebaseConfig.AuthDomain,
                Providers = new FirebaseAuthProvider[]
                {
                    new EmailProvider(),
                    new GoogleProvider().AddScopes("email"),
                    new AppleProvider(),
                    new FacebookProvider()
                }
            };

            _authClient = new FirebaseAuthClient(config);
        }

        public async Task<(bool Success, string? Error, User? User)> LoginWithEmailPasswordAsync(string email, string password)
        {
            try
            {
                var userCredential = await _authClient.SignInWithEmailAndPasswordAsync(email, password);

                if (userCredential?.User == null)
                    return (false, "Login failed. Please try again.", null);

                var user = await MapFirebaseUserToAppUser(userCredential.User);

                FirebaseConfig.SetAuthToken(userCredential.User.Credential.IdToken);
                Preferences.Set(AppConstants.Preferences.IsLoggedIn, true);
                Preferences.Set(AppConstants.Preferences.UserId, user.Id);
                Preferences.Set(AppConstants.Preferences.UserEmail, user.Email);
                Preferences.Set(AppConstants.Preferences.UserName, user.FullName);

                _currentUser = user;

                return (true, null, user);
            }
            catch (FirebaseAuthException ex)
            {
                return (false, GetFriendlyErrorMessage(ex), null);
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string? Error, User? User)> RegisterWithEmailPasswordAsync(string email, string password, string fullName)
        {
            try
            {
                var userCredential = await _authClient.CreateUserWithEmailAndPasswordAsync(email, password, fullName);

                if (userCredential?.User == null)
                    return (false, "Registration failed. Please try again.", null);

                var user = new User
                {
                    Id = userCredential.User.Uid,
                    Email = email,
                    FullName = fullName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsEmailVerified = false,
                    IsActive = true,
                    Role = UserRole.User
                };

                FirebaseConfig.SetAuthToken(userCredential.User.Credential.IdToken);
                Preferences.Set(AppConstants.Preferences.IsLoggedIn, true);
                Preferences.Set(AppConstants.Preferences.UserId, user.Id);
                Preferences.Set(AppConstants.Preferences.UserEmail, user.Email);
                Preferences.Set(AppConstants.Preferences.UserName, user.FullName);

                _currentUser = user;

                return (true, null, user);
            }
            catch (FirebaseAuthException ex)
            {
                return (false, GetFriendlyErrorMessage(ex), null);
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string? Error, User? User)> LoginWithGoogleAsync()
        {
            try
            {
                var userCredential = await _authClient.SignInWithGoogleAsync();

                if (userCredential?.User == null)
                    return (false, "Google login failed. Please try again.", null);

                var user = await MapFirebaseUserToAppUser(userCredential.User);

                FirebaseConfig.SetAuthToken(userCredential.User.Credential.IdToken);
                Preferences.Set(AppConstants.Preferences.IsLoggedIn, true);
                Preferences.Set(AppConstants.Preferences.UserId, user.Id);
                Preferences.Set(AppConstants.Preferences.UserEmail, user.Email);
                Preferences.Set(AppConstants.Preferences.UserName, user.FullName);

                _currentUser = user;

                return (true, null, user);
            }
            catch (Exception ex)
            {
                return (false, $"Google login error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string? Error, User? User)> LoginWithAppleAsync()
        {
            try
            {
                var userCredential = await _authClient.SignInWithAppleAsync();

                if (userCredential?.User == null)
                    return (false, "Apple login failed. Please try again.", null);

                var user = await MapFirebaseUserToAppUser(userCredential.User);

                FirebaseConfig.SetAuthToken(userCredential.User.Credential.IdToken);
                Preferences.Set(AppConstants.Preferences.IsLoggedIn, true);
                Preferences.Set(AppConstants.Preferences.UserId, user.Id);
                Preferences.Set(AppConstants.Preferences.UserEmail, user.Email);
                Preferences.Set(AppConstants.Preferences.UserName, user.FullName);

                _currentUser = user;

                return (true, null, user);
            }
            catch (Exception ex)
            {
                return (false, $"Apple login error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string? Error, User? User)> LoginWithFacebookAsync()
        {
            try
            {
                var userCredential = await _authClient.SignInWithFacebookAsync();

                if (userCredential?.User == null)
                    return (false, "Facebook login failed. Please try again.", null);

                var user = await MapFirebaseUserToAppUser(userCredential.User);

                FirebaseConfig.SetAuthToken(userCredential.User.Credential.IdToken);
                Preferences.Set(AppConstants.Preferences.IsLoggedIn, true);
                Preferences.Set(AppConstants.Preferences.UserId, user.Id);
                Preferences.Set(AppConstants.Preferences.UserEmail, user.Email);
                Preferences.Set(AppConstants.Preferences.UserName, user.FullName);

                _currentUser = user;

                return (true, null, user);
            }
            catch (Exception ex)
            {
                return (false, $"Facebook login error: {ex.Message}", null);
            }
        }

        public Task<bool> LogoutAsync()
        {
            try
            {
                _authClient.SignOut();

                FirebaseConfig.ClearAuthToken();
                Preferences.Remove(AppConstants.Preferences.IsLoggedIn);
                Preferences.Remove(AppConstants.Preferences.UserId);
                Preferences.Remove(AppConstants.Preferences.UserEmail);
                Preferences.Remove(AppConstants.Preferences.UserName);

                _currentUser = null;

                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public Task<User?> GetCurrentUserAsync()
        {
            return Task.FromResult(_currentUser);
        }

        public Task<bool> IsUserLoggedInAsync()
        {
            var isLoggedIn = Preferences.Get(AppConstants.Preferences.IsLoggedIn, false);
            return Task.FromResult(isLoggedIn);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email)
        {
            try
            {
                await _authClient.ResetEmailPasswordAsync(email);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<bool> VerifyEmailAsync(string code)
        {
            return Task.FromResult(true);
        }

        private Task<User> MapFirebaseUserToAppUser(Firebase.Auth.User firebaseUser)
        {
            var user = new User
            {
                Id = firebaseUser.Uid,
                Email = firebaseUser.Info.Email ?? string.Empty,
                FullName = firebaseUser.Info.DisplayName ?? string.Empty,
                ProfileImageUrl = firebaseUser.Info.PhotoUrl ?? string.Empty,
                IsEmailVerified = firebaseUser.Info.IsEmailVerified,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                Role = UserRole.User
            };

            return Task.FromResult(user);
        }

        private string GetFriendlyErrorMessage(FirebaseAuthException ex)
        {
            return ex.Reason switch
            {
                AuthErrorReason.InvalidEmailAddress => "Invalid email address.",
                AuthErrorReason.WrongPassword => "Incorrect password.",
                AuthErrorReason.UserNotFound => "No account found with this email.",
                AuthErrorReason.EmailExists => "An account with this email already exists.",
                AuthErrorReason.WeakPassword => "Password is too weak. Please use a stronger password.",
                AuthErrorReason.TooManyAttemptsTryLater => "Too many attempts. Please try again later.",
                _ => "Authentication error. Please try again."
            };
        }
    }
}
