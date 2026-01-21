using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Data;
using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public class SqliteAuthenticationService : IAuthenticationService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SqliteAuthenticationService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<(bool Success, string? Error, User? User)> LoginWithEmailPasswordAsync(string email, string password)
        {
            try
            {
                if (_userManager == null)
                    return (false, "Authentication service not initialized", null);

                if (_context == null)
                    return (false, "Database service not initialized", null);

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                    return (false, "Invalid email or password", null);

                var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
                if (!isPasswordValid)
                    return (false, "Invalid email or password", null);

                var appUser = MapToUser(user);

                await SecureStorage.SetAsync(AppConstants.Preferences.IsLoggedIn, "true");
                await SecureStorage.SetAsync(AppConstants.Preferences.UserId, user.Id);
                await SecureStorage.SetAsync(AppConstants.Preferences.UserEmail, user.Email ?? string.Empty);
                await SecureStorage.SetAsync(AppConstants.Preferences.UserName, user.FullName);

                return (true, null, appUser);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public async Task<(bool Success, string? Error, User? User)> RegisterWithEmailPasswordAsync(string email, string password, string fullName)
        {
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
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

                var appUser = MapToUser(user);

                // Store user session
                await SecureStorage.SetAsync(AppConstants.Preferences.IsLoggedIn, "true");
                await SecureStorage.SetAsync(AppConstants.Preferences.UserId, user.Id);
                await SecureStorage.SetAsync(AppConstants.Preferences.UserEmail, user.Email ?? string.Empty);
                await SecureStorage.SetAsync(AppConstants.Preferences.UserName, user.FullName);

                return (true, null, appUser);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public Task<(bool Success, string? Error, User? User)> LoginWithGoogleAsync()
        {
            // Social login would require additional OAuth setup
            return Task.FromResult<(bool, string?, User?)>((false, "Google login not yet implemented with SQLite", null));
        }

        public Task<(bool Success, string? Error, User? User)> LoginWithAppleAsync()
        {
            // Social login would require additional OAuth setup
            return Task.FromResult<(bool, string?, User?)>((false, "Apple login not yet implemented with SQLite", null));
        }

        public Task<(bool Success, string? Error, User? User)> LoginWithFacebookAsync()
        {
            // Social login would require additional OAuth setup
            return Task.FromResult<(bool, string?, User?)>((false, "Facebook login not yet implemented with SQLite", null));
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
                return await Task.FromResult(false);
            }
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
            if (string.IsNullOrEmpty(userId))
                return null;

            var user = await _userManager.FindByIdAsync(userId);
            return user != null ? MapToUser(user) : null;
        }

        public async Task<bool> IsUserLoggedInAsync()
        {
            var isLoggedIn = await SecureStorage.GetAsync(AppConstants.Preferences.IsLoggedIn);
            return isLoggedIn == "true";
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                    return false;

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                // In a real app, you would send an email with the token
                // For now, we'll just return true
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> VerifyEmailAsync(string code)
        {
            // Email verification implementation
            return await Task.FromResult(true);
        }

        public async Task<(bool Success, string? Error)> UpdateProfileAsync(string fullName, string? phoneNumber, string? profileImageUrl, string? currentPassword, string? newPassword)
        {
            try
            {
                var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
                if (string.IsNullOrEmpty(userId))
                    return (false, "User not logged in");

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return (false, "User not found");

                // Update basic profile info
                user.FullName = fullName;
                user.PhoneNumber = phoneNumber;
                user.ProfileImageUrl = profileImageUrl;
                user.UpdatedAt = DateTime.UtcNow;

                // Handle password change if requested
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

                // Update local storage
                await SecureStorage.SetAsync(AppConstants.Preferences.UserName, fullName);

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private User MapToUser(ApplicationUser appUser)
        {
            return new User
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
                Role = UserRole.User // You can extend this with roles
            };
        }
    }
}
