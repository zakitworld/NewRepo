using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public interface ITicketService
    {
        Task<List<Ticket>> GetUserTicketsAsync(string userId);
        Task<Ticket?> GetTicketByIdAsync(string ticketId);
        Task<(bool Success, string? Error, Ticket? Ticket)> PurchaseTicketAsync(string eventId, string ticketTypeId, string userId);
        Task<bool> ValidateTicketAsync(string ticketId);
        Task<bool> CheckInTicketAsync(string ticketId);
        Task<bool> CancelTicketAsync(string ticketId);
        Task<string> GenerateQRCodeAsync(string ticketId);
        Task<List<Ticket>> GetEventTicketsAsync(string eventId);
    }
}
