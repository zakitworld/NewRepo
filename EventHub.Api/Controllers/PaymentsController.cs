using EventHub.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace EventHub.Api.Controllers;

/// <summary>
/// Receives Paystack webhook events.
/// Configure this URL in your Paystack dashboard:
///   https://your-api-domain.com/api/payments/webhook
///
/// Paystack sends a POST with JSON body signed with your webhook secret.
/// We verify the HMAC-SHA512 signature before processing.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly ApiDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(ApiDbContext db, IConfiguration config,
                              ILogger<PaymentsController> logger)
    {
        _db     = db;
        _config = config;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> PaystackWebhook()
    {
        // Read raw body for signature verification
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        // Verify Paystack HMAC-SHA512 signature
        if (!IsValidSignature(body, Request.Headers["x-paystack-signature"].FirstOrDefault() ?? string.Empty))
        {
            _logger.LogWarning("Paystack webhook: invalid signature");
            return BadRequest("Invalid signature");
        }

        var payload = JObject.Parse(body);
        var eventType = payload["event"]?.ToString();

        if (eventType == "charge.success")
        {
            var data      = payload["data"];
            var reference = data?["reference"]?.ToString();
            var amountStr = data?["amount"]?.ToString();      // pesewas
            var email     = data?["customer"]?["email"]?.ToString();

            _logger.LogInformation(
                "Paystack charge.success: ref={Reference} amount={Amount} email={Email}",
                reference, amountStr, email);

            // Mark related ticket as paid (if reference matches a pending ticket)
            if (!string.IsNullOrEmpty(reference))
            {
                var ticket = await _db.Tickets
                    .FirstOrDefaultAsync(t => t.TransactionId == reference);
                if (ticket != null)
                {
                    ticket.Status = "Active";
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Ticket {TicketId} activated via webhook", ticket.Id);
                }
            }
        }

        return Ok();   // Always return 200 so Paystack stops retrying
    }

    private bool IsValidSignature(string body, string signature)
    {
        var webhookSecret = _config["Paystack:WebhookSecret"] ?? string.Empty;
        if (string.IsNullOrEmpty(webhookSecret)) return false;

        using var hmac = new System.Security.Cryptography.HMACSHA512(
            System.Text.Encoding.UTF8.GetBytes(webhookSecret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(body));
        var computed = Convert.ToHexString(hash).ToLowerInvariant();
        return computed == signature.ToLowerInvariant();
    }

    /// <summary>
    /// Initiates a Paystack transaction server-side.
    /// The mobile app calls this endpoint instead of calling Paystack directly,
    /// keeping the secret key off the device.
    /// </summary>
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] InitiatePaymentRequest req)
    {
        var secretKey = _config["Paystack:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
            return StatusCode(503, "Payment service not configured");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secretKey);

        var reference = $"EVH_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
        var payload   = new
        {
            email        = req.Email,
            amount       = (int)(req.AmountGhs * 100),   // convert to pesewas
            reference,
            currency     = "GHS",
            callback_url = _config["Paystack:CallbackUrl"] ?? "eventhub://payment-callback"
        };

        var response = await http.PostAsJsonAsync("https://api.paystack.co/transaction/initialize", payload);
        var content  = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Paystack initiate failed: {Body}", content);
            return BadRequest("Payment initiation failed");
        }

        var json         = JObject.Parse(content);
        var authUrl      = json["data"]?["authorization_url"]?.ToString();
        var returnedRef  = json["data"]?["reference"]?.ToString() ?? reference;

        return Ok(new { authorizationUrl = authUrl, reference = returnedRef });
    }
}

public record InitiatePaymentRequest(string Email, decimal AmountGhs, string? Description);
