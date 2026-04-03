using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Data;
using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public class SqliteAuthenticationService : IAuthenticationService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SqliteAuthenticationService> _logger;

        public SqliteAuthenticationService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<SqliteAuthenticationService> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(bool Success, string? Error, User? User)> LoginWithEmailPasswordAsync(string email, string password)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                    return (false, "Invalid email or password", null);

                var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
                if (!isPasswordValid)
                    return (false, "Invalid email or password", null);

                await PersistSessionAsync(user);
                return (true, null, MapToUser(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", email);
                return (false, ex.Message, null);
            }
        }

        public async Task<(bool Success, string? Error, User? User)> RegisterWithEmailPasswordAsync(
            string email, string password, string fullName)
        {
            try
            {
                var existing = await _userManager.FindByEmailAsync(email);
                if (existing != null)
                    return (false, "An account with this email already exists", null);

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, errors, null);
                }

                await PersistSessionAsync(user);
                return (true, null, MapToUser(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for {Email}", email);
                return (false, ex.Message, null);
            }
        }

        /// <summary>
        /// Google OAuth via MAUI WebAuthenticator.
        /// Replace the placeholder URL with your backend OAuth endpoint or Google OAuth URL.
        /// Set up a custom URL scheme callback in AndroidManifest.xml / Info.plist.
        /// </summary>
        public async Task<(bool Success, string? Error, User? User)> LoginWithGoogleAsync()
        {
            try
            {
                var clientId = _configuration["SocialLogin:GoogleClientId"];
                if (string.IsNullOrWhiteSpace(clientId) || clientId.StartsWith("REPLACE"))
                    return (false, "Google Sign-In is not configured. Add your GoogleClientId to appsettings.json.", null);

                var redirectUri = "eventhub://oauth/google-callback";
                var authUrl = new Uri(
                    $"https://accounts.google.com/o/oauth2/v2/auth?" +
                    $"client_id={clientId}&" +
                    $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                    $"response_type=code&" +
                    $"scope=openid%20email%20profile");

                var result = await WebAuthenticator.AuthenticateAsync(authUrl, new Uri(redirectUri));

                if (result?.Properties.TryGetValue("email", out var email) == true && email is not null)
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        // Auto-register social user
                        user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            FullName = result.Properties.TryGetValue("name", out var name) ? name ?? email : email,
                            EmailConfirmed = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        await _userManager.CreateAsync(user);
                    }

                    await PersistSessionAsync(user);
                    return (true, null, MapToUser(user));
                }

                return (false, "Google sign-in was cancelled or failed.", null);
            }
            catch (TaskCanceledException)
            {
                return (false, "Google sign-in was cancelled.", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google login error");
                return (false, ex.Message, null);
            }
        }

        /// <summary>
        /// Apple Sign-In via MAUI WebAuthenticator.
        /// Apple Sign-In is required by App Store guidelines when other social logins are present.
        /// </summary>
        public async Task<(bool Success, string? Error, User? User)> LoginWithAppleAsync()
        {
            try
            {
                var clientId = _configuration["SocialLogin:AppleClientId"];
                if (string.IsNullOrWhiteSpace(clientId) || clientId.StartsWith("REPLACE"))
                    return (false, "Apple Sign-In is not configured. Add your AppleClientId to appsettings.json.", null);

                var redirectUri = "eventhub://oauth/apple-callback";
                var authUrl = new Uri(
                    $"https://appleid.apple.com/auth/authorize?" +
                    $"client_id={clientId}&" +
                    $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                    $"response_type=code%20id_token&" +
                    $"scope=name%20email&" +
                    $"response_mode=form_post");

                var result = await WebAuthenticator.AuthenticateAsync(
                    new WebAuthenticatorOptions { Url = authUrl, CallbackUrl = new Uri(redirectUri), PrefersEphemeralWebBrowserSession = true });

                if (result?.Properties.TryGetValue("email", out var email) == true && email is not null)
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            FullName = result.Properties.TryGetValue("name", out var name) ? name ?? "Apple User" : "Apple User",
                            EmailConfirmed = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        await _userManager.CreateAsync(user);
                    }

                    await PersistSessionAsync(user);
                    return (true, null, MapToUser(user));
                }

                return (false, "Apple Sign-In was cancelled or failed.", null);
            }
            catch (TaskCanceledException)
            {
                return (false, "Apple Sign-In was cancelled.", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Apple login error");
                return (false, ex.Message, null);
            }
        }

        public Task<(bool Success, string? Error, User? User)> LoginWithFacebookAsync()
        {
            // Facebook login requires a Facebook Developer App and the official SDK.
            // For a lightweight alternative, implement WebAuthenticator OAuth similar to Google above.
            return Task.FromResult<(bool, string?, User?)>(
                (false, "Facebook login is not yet configured. See appsettings.json SocialLogin section.", null));
        }

        public async Task<bool> LogoutAsync()
        {
            try
            {
                SecureStorage.Remove(AppConstants.Preferences.IsLoggedIn);
                SecureStorage.Remove(AppConstants.Preferences.UserId);
                SecureStorage.Remove(AppConstants.Preferences.UserEmail);
                SecureStorage.Remove(AppConstants.Preferences.UserName);
                return await Task.FromResult(true);
            }
            catch
            {
                return false;
            }
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
            if (string.IsNullOrEmpty(userId)) return null;
            var user = await _userManager.FindByIdAsync(userId);
            return user != null ? MapToUser(user) : null;
        }

        public async Task<bool> IsUserLoggedInAsync()
        {
            var isLoggedIn = await SecureStorage.GetAsync(AppConstants.Preferences.IsLoggedIn);
            return isLoggedIn == "true";
        }

        /// <summary>
        /// Generates a password reset token and emails it to the user.
        /// The token is a 6-digit code valid for 15 minutes (stored in SecureStorage keyed by email hash).
        /// </summary>
        public async Task<bool> SendPasswordResetEmailAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    // Return true to prevent email enumeration attacks
                    return true;
                }

                // Generate 6-digit numeric token
                var random = new Random();
                var resetCode = random.Next(100000, 999999).ToString();

                // Store token with 15-minute expiry in SecureStorage
                var storageKey = $"reset_{Uri.EscapeDataString(email)}";
                var expiry = DateTime.UtcNow.AddMinutes(15).Ticks.ToString();
                await SecureStorage.SetAsync(storageKey, $"{resetCode}|{expiry}");

                await _emailService.SendPasswordResetEmailAsync(email, user.FullName, resetCode);

                _logger.LogInformation("Password reset email sent to {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
                return false;
            }
        }

        /// <summary>
        /// Verifies the 6-digit reset code and returns the reset token for use with UserManager.ResetPasswordAsync.
        /// </summary>
        public async Task<(bool Valid, string? ResetToken, string? Error)> VerifyResetCodeAsync(string email, string code)
        {
            try
            {
                var storageKey = $"reset_{Uri.EscapeDataString(email)}";
                var stored = await SecureStorage.GetAsync(storageKey);
                if (string.IsNullOrEmpty(stored))
                    return (false, null, "Reset code not found or expired.");

                var parts = stored.Split('|');
                if (parts.Length != 2)
                    return (false, null, "Invalid reset code.");

                var storedCode = parts[0];
                var expiry = new DateTime(long.Parse(parts[1]), DateTimeKind.Utc);

                if (DateTime.UtcNow > expiry)
                {
                    SecureStorage.Remove(storageKey);
                    return (false, null, "Reset code has expired. Please request a new one.");
                }

                if (storedCode != code)
                    return (false, null, "Invalid reset code.");

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                    return (false, null, "User not found.");

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                SecureStorage.Remove(storageKey);

                return (true, resetToken, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying reset code for {Email}", email);
                return (false, null, ex.Message);
            }
        }

        public async Task<bool> VerifyEmailAsync(string code)
        {
            return await Task.FromResult(true);
        }

        public async Task<(bool Success, string? Error)> UpdateProfileAsync(
            string fullName, string? phoneNumber, string? profileImageUrl,
            string? currentPassword, string? newPassword)
        {
            try
            {
                var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
                if (string.IsNullOrEmpty(userId))
                    return (false, "User not logged in");

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return (false, "User not found");

                user.FullName = fullName;
                user.PhoneNumber = phoneNumber;
                user.ProfileImageUrl = profileImageUrl;
                user.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(currentPassword) && !string.IsNullOrEmpty(newPassword))
                {
                    var passwordCheck = await _userManager.CheckPasswordAsync(user, currentPassword);
                    if (!passwordCheck)
                        return (false, "Current password is incorrect");

                    var passwordResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                    if (!passwordResult.Succeeded)
                    {
                        var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
                        return (false, errors);
                    }
                }

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, errors);
                }

                await SecureStorage.SetAsync(AppConstants.Preferences.UserName, fullName);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateProfile failed");
                return (false, ex.Message);
            }
        }

        private async Task PersistSessionAsync(ApplicationUser user)
        {
            await SecureStorage.SetAsync(AppConstants.Preferences.IsLoggedIn, "true");
            await SecureStorage.SetAsync(AppConstants.Preferences.UserId, user.Id);
            await SecureStorage.SetAsync(AppConstants.Preferences.UserEmail, user.Email ?? string.Empty);
            await SecureStorage.SetAsync(AppConstants.Preferences.UserName, user.FullName);
        }

        private static User MapToUser(ApplicationUser appUser) => new()
        {
            Id = appUser.Id,
            Email = appUser.Email ?? string.Empty,
            FullName = appUser.FullName,
            PhoneNumber = appUser.PhoneNumber ?? string.Empty,
            ProfileImageUrl = appUser.ProfileImageUrl ?? string.Empty,
            IsEmailVerified = appUser.EmailConfirmed,
            CreatedAt = appUser.CreatedAt,
            UpdatedAt = appUser.UpdatedAt,
            IsActive = appUser.IsActive,
            Role = UserRole.User
        };
    }
}
