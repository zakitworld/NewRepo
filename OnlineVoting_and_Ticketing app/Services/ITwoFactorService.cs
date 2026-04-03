namespace OnlineVoting_and_Ticketing_app.Services
{
    public interface ITwoFactorService
    {
        /// <summary>Generates a new TOTP secret key (Base32) for the user.</summary>
        string GenerateSecretKey();

        /// <summary>Returns the otpauth:// URI used to generate a QR code for authenticator apps.</summary>
        string GetOtpAuthUri(string secretKey, string userEmail, string issuer = "EventHub");

        /// <summary>Validates a 6-digit TOTP code against the user's secret.</summary>
        bool ValidateCode(string secretKey, string code);

        /// <summary>Saves the TOTP secret in encrypted secure storage for the current user.</summary>
        Task SaveSecretAsync(string userId, string secretKey);

        /// <summary>Retrieves the stored TOTP secret for a user, or null if 2FA is not set up.</summary>
        Task<string?> GetSecretAsync(string userId);

        /// <summary>Removes the stored TOTP secret, effectively disabling 2FA for the user.</summary>
        Task DisableAsync(string userId);

        /// <summary>Returns true if the user has 2FA configured.</summary>
        Task<bool> IsEnabledAsync(string userId);
    }
}
