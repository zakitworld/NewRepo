namespace OnlineVoting_and_Ticketing_app.Services
{
    public interface IPaymentService
    {
        Task<(bool Success, string? Error, string? TransactionId)> InitiatePaymentAsync(decimal amount, string email, string reference);
        Task<(bool Success, string? Error)> VerifyPaymentAsync(string transactionId);
        Task<string> GeneratePaymentReferenceAsync();
    }
}
