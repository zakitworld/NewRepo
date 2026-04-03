using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public interface ITicketService
    {
        Task<List<Ticket>> GetUserTicketsAsync(string userId, int page = 1, int pageSize = 20);
        Task<Ticket?> GetTicketByIdAsync(string ticketId);
        Task<(bool Success, string? Error, Ticket? Ticket)> PurchaseTicketAsync(string eventId, string ticketTypeId, string userId);
        Task<bool> ValidateTicketAsync(string ticketId);
        Task<bool> CheckInTicketAsync(string ticketId);
        Task<bool> CheckInByQRCodeAsync(string qrContent);
        Task<bool> CancelTicketAsync(string ticketId);
        Task<string> GenerateQRCodeAsync(string ticketId);
        Task<List<Ticket>> GetEventTicketsAsync(string eventId, int page = 1, int pageSize = 50);
        Task<int> GetUserTicketCountAsync(string userId);
        Task ExpireOldTicketsAsync();
    }
}
