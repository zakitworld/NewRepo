using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public class PaystackPaymentService : IPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly string _currency;
        private readonly string _callbackUrl;
        private readonly ILogger<PaystackPaymentService> _logger;
        private const string BaseUrl = "https://api.paystack.co";

        public PaystackPaymentService(IConfiguration configuration, ILogger<PaystackPaymentService> logger)
        {
            _logger = logger;

            var secretKey = configuration["Paystack:SecretKey"]
                ?? throw new InvalidOperationException(
                    "Paystack:SecretKey is not configured. Add it to appsettings.json or user secrets.");

            _currency = configuration["Paystack:Currency"] ?? "GHS";
            _callbackUrl = configuration["Paystack:CallbackUrl"] ?? "eventhub://payment-callback";

            _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", secretKey);
        }

        public async Task<(bool Success, string? Error, string? TransactionId)> InitiatePaymentAsync(
            decimal amount, string email, string reference)
        {
            try
            {
                var payload = new
                {
                    email,
                    amount = (int)(amount * 100), // Convert to kobo/pesewas
                    reference,
                    currency = _currency,
                    callback_url = _callbackUrl
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transaction/initialize", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<PaystackInitializeResponse>(responseContent);
                    if (result?.Status == true && result.Data != null)
                    {
                        await Browser.OpenAsync(result.Data.AuthorizationUrl, BrowserLaunchMode.SystemPreferred);
                        return (true, null, result.Data.Reference);
                    }
                }

                _logger.LogWarning("Paystack initialize failed for ref {Reference}: {Body}", reference, responseContent);
                return (false, "Failed to initialize payment", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Paystack payment for ref {Reference}", reference);
                return (false, ex.Message, null);
            }
        }

        public async Task<(bool Success, string? Error)> VerifyPaymentAsync(string reference)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/transaction/verify/{reference}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<PaystackVerifyResponse>(responseContent);
                    if (result?.Status == true && result.Data?.Status == "success")
                        return (true, null);
                }

                _logger.LogWarning("Paystack verification failed for ref {Reference}: {Body}", reference, responseContent);
                return (false, "Payment verification failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Paystack payment ref {Reference}", reference);
                return (false, ex.Message);
            }
        }

        public Task<string> GeneratePaymentReferenceAsync()
        {
            var reference = $"EVH_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
            return Task.FromResult(reference);
        }

        private class PaystackInitializeResponse
        {
            [JsonProperty("status")] public bool Status { get; set; }
            [JsonProperty("message")] public string? Message { get; set; }
            [JsonProperty("data")] public PaystackInitializeData? Data { get; set; }
        }

        private class PaystackInitializeData
        {
            [JsonProperty("authorization_url")] public string AuthorizationUrl { get; set; } = string.Empty;
            [JsonProperty("access_code")] public string AccessCode { get; set; } = string.Empty;
            [JsonProperty("reference")] public string Reference { get; set; } = string.Empty;
        }

        private class PaystackVerifyResponse
        {
            [JsonProperty("status")] public bool Status { get; set; }
            [JsonProperty("message")] public string? Message { get; set; }
            [JsonProperty("data")] public PaystackVerifyData? Data { get; set; }
        }

        private class PaystackVerifyData
        {
            [JsonProperty("status")] public string Status { get; set; } = string.Empty;
            [JsonProperty("reference")] public string Reference { get; set; } = string.Empty;
            [JsonProperty("amount")] public int Amount { get; set; }
            [JsonProperty("currency")] public string Currency { get; set; } = string.Empty;
        }
    }
}
