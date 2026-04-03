namespace OnlineVoting_and_Ticketing_app.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends a password-reset email with a one-time token link.
        /// </summary>
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken);

        /// <summary>
        /// Sends a ticket confirmation email with QR code attachment.
        /// </summary>
        Task<bool> SendTicketConfirmationAsync(string toEmail, string toName, string eventTitle, string ticketId, string qrCodeBase64);

        /// <summary>
        /// Sends a 2FA TOTP setup confirmation email.
        /// </summary>
        Task<bool> SendTwoFactorSetupEmailAsync(string toEmail, string toName);
    }
}
