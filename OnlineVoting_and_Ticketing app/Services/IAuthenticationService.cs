using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public interface IAuthenticationService
    {
        Task<(bool Success, string? Error, User? User)> LoginWithEmailPasswordAsync(string email, string password);
        Task<(bool Success, string? Error, User? User)> RegisterWithEmailPasswordAsync(string email, string password, string fullName);
        Task<(bool Success, string? Error, User? User)> LoginWithGoogleAsync();
        Task<(bool Success, string? Error, User? User)> LoginWithAppleAsync();
        Task<(bool Success, string? Error, User? User)> LoginWithFacebookAsync();
        Task<bool> LogoutAsync();
        Task<User?> GetCurrentUserAsync();
        Task<bool> IsUserLoggedInAsync();
        Task<bool> SendPasswordResetEmailAsync(string email);
        Task<bool> VerifyEmailAsync(string code);
        Task<(bool Success, string? Error)> UpdateProfileAsync(string fullName, string? phoneNumber, string? profileImageUrl, string? currentPassword, string? newPassword);
    }
}
