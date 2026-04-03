using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace OnlineVoting_and_Ticketing_app.Services
{
    /// <summary>
    /// Sends transactional emails via an HTTP relay endpoint.
    ///
    /// Why HTTP instead of SMTP directly from mobile:
    ///   - Mobile carriers commonly block outbound port 587/465
    ///   - Storing SMTP credentials on-device is a security risk
    ///   - An HTTP relay (your own backend, a Supabase Edge Function, or a
    ///     free tier of Resend/SendGrid) keeps credentials server-side
    ///
    /// Configure in appsettings.json:
    ///   "Email": {
    ///     "RelayUrl":    "https://your-api.com/api/send-email",
    ///     "ApiKey":      "your-relay-api-key",
    ///     "SenderEmail": "noreply@eventhub.app",
    ///     "SenderName":  "EventHub"
    ///   }
    ///
    /// The relay endpoint must accept POST with JSON body:
    ///   { "to": "...", "toName": "...", "subject": "...", "htmlBody": "..." }
    /// and an Authorization header: "Bearer {ApiKey}".
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _relayUrl;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _logger = logger;
            _relayUrl = configuration["Email:RelayUrl"];
            _senderEmail = configuration["Email:SenderEmail"] ?? "noreply@eventhub.app";
            _senderName = configuration["Email:SenderName"] ?? "EventHub";

            var apiKey = configuration["Email:ApiKey"] ?? string.Empty;
            _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(apiKey))
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken)
        {
            var subject = "Reset Your EventHub Password";
            var body = $@"
<html>
<body style=""font-family:Arial,sans-serif;background:#0F0F23;color:#fff;padding:32px;"">
  <h2 style=""color:#8B5CF6;"">EventHub — Password Reset</h2>
  <p>Hi {toName},</p>
  <p>Use the code below to reset your password. It expires in <strong>15 minutes</strong>.</p>
  <div style=""background:#1E1E3F;padding:24px;border-radius:12px;text-align:center;margin:24px 0;"">
    <span style=""font-size:32px;font-weight:bold;letter-spacing:8px;color:#8B5CF6;"">{resetToken}</span>
  </div>
  <p style=""color:#94A3B8;"">If you didn't request a reset, you can safely ignore this email.</p>
  <hr style=""border-color:#2D2D5E;""/>
  <p style=""font-size:12px;color:#6B7280;"">EventHub — Connecting People Through Events</p>
</body>
</html>";
            return await SendAsync(toEmail, toName, subject, body);
        }

        public async Task<bool> SendTicketConfirmationAsync(
            string toEmail, string toName, string eventTitle, string ticketId, string qrCodeBase64)
        {
            var subject = $"Your Ticket for {eventTitle}";
            var body = $@"
<html>
<body style=""font-family:Arial,sans-serif;background:#0F0F23;color:#fff;padding:32px;"">
  <h2 style=""color:#8B5CF6;"">EventHub — Ticket Confirmed!</h2>
  <p>Hi {toName},</p>
  <p>Your ticket for <strong>{eventTitle}</strong> is confirmed.</p>
  <p>Ticket ID: <code style=""color:#10B981;"">{ticketId}</code></p>
  <p>Present this QR code at the entrance:</p>
  <div style=""text-align:center;margin:24px 0;"">
    <img src=""{qrCodeBase64}"" alt=""QR Code"" style=""width:200px;height:200px;""/>
  </div>
  <p style=""color:#94A3B8;"">Save this email or screenshot the QR code before attending.</p>
  <hr style=""border-color:#2D2D5E;""/>
  <p style=""font-size:12px;color:#6B7280;"">EventHub — See you there!</p>
</body>
</html>";
            return await SendAsync(toEmail, toName, subject, body);
        }

        public async Task<bool> SendTwoFactorSetupEmailAsync(string toEmail, string toName)
        {
            var subject = "Two-Factor Authentication Enabled";
            var body = $@"
<html>
<body style=""font-family:Arial,sans-serif;background:#0F0F23;color:#fff;padding:32px;"">
  <h2 style=""color:#8B5CF6;"">EventHub — 2FA Enabled</h2>
  <p>Hi {toName},</p>
  <p>Two-Factor Authentication has been successfully enabled on your EventHub account.</p>
  <p>Each login will now require a 6-digit code from your authenticator app.</p>
  <p style=""color:#94A3B8;"">If you didn't enable 2FA, contact support immediately.</p>
  <hr style=""border-color:#2D2D5E;""/>
  <p style=""font-size:12px;color:#6B7280;"">EventHub Security Team</p>
</body>
</html>";
            return await SendAsync(toEmail, toName, subject, body);
        }

        private async Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(_relayUrl) || _relayUrl.StartsWith("REPLACE"))
            {
                _logger.LogWarning(
                    "Email relay not configured. Set Email:RelayUrl in appsettings.json. " +
                    "Skipping send to {Email}", toEmail);
                return false;
            }

            try
            {
                var payload = new
                {
                    to = toEmail,
                    toName,
                    subject,
                    htmlBody,
                    from = _senderEmail,
                    fromName = _senderName
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_relayUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email '{Subject}' sent to {Email}", subject, toEmail);
                    return true;
                }

                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Email relay returned {Status} for '{Subject}' to {Email}: {Body}",
                    response.StatusCode, subject, toEmail, body);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email '{Subject}' to {Email}", subject, toEmail);
                return false;
            }
        }
    }
}
