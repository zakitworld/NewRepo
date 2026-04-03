using Microsoft.Extensions.Logging;
using OtpNet;

namespace OnlineVoting_and_Ticketing_app.Services
{
    /// <summary>
    /// TOTP-based Two-Factor Authentication using the Otp.NET library.
    /// Secrets are stored in SecureStorage keyed per userId.
    /// Compatible with Google Authenticator, Authy, and any RFC 6238 app.
    /// </summary>
    public class TwoFactorService : ITwoFactorService
    {
        private readonly ILogger<TwoFactorService> _logger;
        private const string StorageKeyPrefix = "2fa_secret_";

        public TwoFactorService(ILogger<TwoFactorService> logger)
        {
            _logger = logger;
        }

        public string GenerateSecretKey()
        {
            var key = KeyGeneration.GenerateRandomKey(20); // 160-bit key
            return Base32Encoding.ToString(key);
        }

        public string GetOtpAuthUri(string secretKey, string userEmail, string issuer = "EventHub")
        {
            var encodedIssuer = Uri.EscapeDataString(issuer);
            var encodedEmail = Uri.EscapeDataString(userEmail);
            return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secretKey}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
        }

        public bool ValidateCode(string secretKey, string code)
        {
            try
            {
                var keyBytes = Base32Encoding.ToBytes(secretKey);
                var totp = new Totp(keyBytes);

                // Allow 1-step window (±30 seconds) to account for clock drift
                return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TOTP validation error");
                return false;
            }
        }

        public async Task SaveSecretAsync(string userId, string secretKey)
        {
            await SecureStorage.SetAsync($"{StorageKeyPrefix}{userId}", secretKey);
            _logger.LogInformation("2FA secret saved for user {UserId}", userId);
        }

        public async Task<string?> GetSecretAsync(string userId) =>
            await SecureStorage.GetAsync($"{StorageKeyPrefix}{userId}");

        public async Task DisableAsync(string userId)
        {
            SecureStorage.Remove($"{StorageKeyPrefix}{userId}");
            _logger.LogInformation("2FA disabled for user {UserId}", userId);
            await Task.CompletedTask;
        }

        public async Task<bool> IsEnabledAsync(string userId)
        {
            var secret = await GetSecretAsync(userId);
            return !string.IsNullOrEmpty(secret);
        }
    }
}
