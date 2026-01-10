using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public class PaystackPaymentService : IPaymentService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.paystack.co";
        private const string SecretKey = "YOUR_PAYSTACK_SECRET_KEY"; // Replace with your actual Paystack secret key

        public PaystackPaymentService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl)
            };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SecretKey);
        }

        public async Task<(bool Success, string? Error, string? TransactionId)> InitiatePaymentAsync(decimal amount, string email, string reference)
        {
            try
            {
                var payload = new
                {
                    email = email,
                    amount = (int)(amount * 100), // Convert to kobo/cents
                    reference = reference,
                    currency = "GHS", // Change to your preferred currency (NGN for Naira, GHS for Ghana Cedis, etc.)
                    callback_url = "eventhub://payment-callback"
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transaction/initialize", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<PaystackInitializeResponse>(responseContent);

                    if (result?.Status == true && result.Data != null)
                    {
                        // Open the authorization URL in browser
                        await Browser.OpenAsync(result.Data.AuthorizationUrl, BrowserLaunchMode.SystemPreferred);

                        return (true, null, result.Data.Reference);
                    }
                }

                return (false, "Failed to initialize payment", null);
            }
            catch (Exception ex)
            {
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
                    {
                        return (true, null);
                    }
                }

                return (false, "Payment verification failed");
            }
            catch (Exception ex)
            {
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
            [JsonProperty("status")]
            public bool Status { get; set; }

            [JsonProperty("message")]
            public string? Message { get; set; }

            [JsonProperty("data")]
            public PaystackInitializeData? Data { get; set; }
        }

        private class PaystackInitializeData
        {
            [JsonProperty("authorization_url")]
            public string AuthorizationUrl { get; set; } = string.Empty;

            [JsonProperty("access_code")]
            public string AccessCode { get; set; } = string.Empty;

            [JsonProperty("reference")]
            public string Reference { get; set; } = string.Empty;
        }

        private class PaystackVerifyResponse
        {
            [JsonProperty("status")]
            public bool Status { get; set; }

            [JsonProperty("message")]
            public string? Message { get; set; }

            [JsonProperty("data")]
            public PaystackVerifyData? Data { get; set; }
        }

        private class PaystackVerifyData
        {
            [JsonProperty("status")]
            public string Status { get; set; } = string.Empty;

            [JsonProperty("reference")]
            public string Reference { get; set; } = string.Empty;

            [JsonProperty("amount")]
            public int Amount { get; set; }

            [JsonProperty("currency")]
            public string Currency { get; set; } = string.Empty;
        }
    }
}
