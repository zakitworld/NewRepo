using Microsoft.EntityFrameworkCore;
using OnlineVoting_and_Ticketing_app.Data;
using OnlineVoting_and_Ticketing_app.Models;
using QRCoder;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public class SqliteTicketService : ITicketService
    {
        private readonly AppDbContext _context;
        private readonly IEventService _eventService;

        public SqliteTicketService(AppDbContext context, IEventService eventService)
        {
            _context = context;
            _eventService = eventService;
        }

        public async Task<List<Ticket>> GetUserTicketsAsync(string userId)
        {
            try
            {
                return await _context.Tickets
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.PurchasedAt)
                    .ToListAsync();
            }
            catch
            {
                return new List<Ticket>();
            }
        }

        public async Task<Ticket?> GetTicketByIdAsync(string ticketId)
        {
            try
            {
                return await _context.Tickets.FindAsync(ticketId);
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool Success, string? Error, Ticket? Ticket)> PurchaseTicketAsync(string eventId, string ticketTypeId, string userId)
        {
            try
            {
                var eventData = await _eventService.GetEventByIdAsync(eventId);
                if (eventData == null)
                    return (false, "Event not found", null);

                var ticketType = eventData.TicketTypes.FirstOrDefault(t => t.Id == ticketTypeId);
                if (ticketType == null)
                    return (false, "Ticket type not found", null);

                if (ticketType.AvailableQuantity <= 0)
                    return (false, "Tickets sold out", null);

                var ticket = new Ticket
                {
                    Id = Guid.NewGuid().ToString(),
                    EventId = eventId,
                    EventTitle = eventData.Title,
                    UserId = userId,
                    TicketTypeId = ticketTypeId,
                    TicketTypeName = ticketType.Name,
                    Price = ticketType.Price,
                    Status = TicketStatus.Active,
                    PurchasedAt = DateTime.UtcNow,
                    PaymentStatus = PaymentStatus.Completed
                };

                ticket.QRCode = await GenerateQRCodeAsync(ticket.Id);

                _context.Tickets.Add(ticket);

                // Update ticket availability
                ticketType.AvailableQuantity--;
                eventData.AvailableTickets--;
                await _eventService.UpdateEventAsync(eventData);

                await _context.SaveChangesAsync();

                return (true, null, ticket);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public async Task<bool> ValidateTicketAsync(string ticketId)
        {
            try
            {
                var ticket = await GetTicketByIdAsync(ticketId);
                return ticket != null && ticket.Status == TicketStatus.Active;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CheckInTicketAsync(string ticketId)
        {
            try
            {
                var ticket = await GetTicketByIdAsync(ticketId);
                if (ticket == null || ticket.Status != TicketStatus.Active)
                    return false;

                ticket.Status = TicketStatus.Used;
                ticket.CheckedInAt = DateTime.UtcNow;

                _context.Tickets.Update(ticket);
                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CancelTicketAsync(string ticketId)
        {
            try
            {
                var ticket = await GetTicketByIdAsync(ticketId);
                if (ticket == null)
                    return false;

                ticket.Status = TicketStatus.Cancelled;

                _context.Tickets.Update(ticket);

                // Restore ticket availability
                var eventData = await _eventService.GetEventByIdAsync(ticket.EventId);
                if (eventData != null)
                {
                    var ticketType = eventData.TicketTypes.FirstOrDefault(t => t.Id == ticket.TicketTypeId);
                    if (ticketType != null)
                    {
                        ticketType.AvailableQuantity++;
                        eventData.AvailableTickets++;
                        await _eventService.UpdateEventAsync(eventData);
                    }
                }

                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<string> GenerateQRCodeAsync(string ticketId)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(ticketId, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20);
                var base64String = Convert.ToBase64String(qrCodeBytes);
                return Task.FromResult($"data:image/png;base64,{base64String}");
            }
            catch
            {
                return Task.FromResult(string.Empty);
            }
        }

        public async Task<List<Ticket>> GetEventTicketsAsync(string eventId)
        {
            try
            {
                return await _context.Tickets
                    .Where(t => t.EventId == eventId)
                    .OrderByDescending(t => t.PurchasedAt)
                    .ToListAsync();
            }
            catch
            {
                return new List<Ticket>();
            }
        }
    }
}
