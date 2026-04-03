using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineVoting_and_Ticketing_app.Data;
using OnlineVoting_and_Ticketing_app.Models;
using QRCoder;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public class SqliteTicketService : ITicketService
    {
        private readonly AppDbContext _context;
        private readonly IEventService _eventService;
        private readonly ILogger<SqliteTicketService> _logger;

        public SqliteTicketService(AppDbContext context, IEventService eventService, ILogger<SqliteTicketService> logger)
        {
            _context = context;
            _eventService = eventService;
            _logger = logger;
        }

        public async Task<List<Ticket>> GetUserTicketsAsync(string userId, int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Tickets
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.PurchasedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserTicketsAsync failed for {UserId}", userId);
                return [];
            }
        }

        public async Task<Ticket?> GetTicketByIdAsync(string ticketId)
        {
            try
            {
                return await _context.Tickets.FindAsync(ticketId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTicketByIdAsync failed for {TicketId}", ticketId);
                return null;
            }
        }

        public async Task<(bool Success, string? Error, Ticket? Ticket)> PurchaseTicketAsync(
            string eventId, string ticketTypeId, string userId)
        {
            try
            {
                var eventData = await _eventService.GetEventByIdAsync(eventId);
                if (eventData == null) return (false, "Event not found", null);

                var ticketType = eventData.TicketTypes.FirstOrDefault(t => t.Id == ticketTypeId);
                if (ticketType == null) return (false, "Ticket type not found", null);
                if (ticketType.AvailableQuantity <= 0) return (false, "Tickets sold out", null);

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

                ticketType.AvailableQuantity--;
                eventData.AvailableTickets--;
                await _eventService.UpdateEventAsync(eventData);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Ticket purchased: {TicketId} for event {EventId} by {UserId}",
                    ticket.Id, eventId, userId);

                return (true, null, ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PurchaseTicketAsync failed for event {EventId}", eventId);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "ValidateTicketAsync failed for {TicketId}", ticketId);
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

                _logger.LogInformation("Ticket checked in: {TicketId}", ticketId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckInTicketAsync failed for {TicketId}", ticketId);
                return false;
            }
        }

        /// <summary>
        /// Validates and checks in a ticket using the raw QR code content (which equals the ticketId).
        /// Used by the QR scanner page to process scanned codes.
        /// </summary>
        public async Task<bool> CheckInByQRCodeAsync(string qrContent)
        {
            // The QR code encodes the ticket ID directly
            if (string.IsNullOrWhiteSpace(qrContent)) return false;

            var isValid = await ValidateTicketAsync(qrContent);
            if (!isValid) return false;

            return await CheckInTicketAsync(qrContent);
        }

        public async Task<bool> CancelTicketAsync(string ticketId)
        {
            try
            {
                var ticket = await GetTicketByIdAsync(ticketId);
                if (ticket == null) return false;

                ticket.Status = TicketStatus.Cancelled;
                _context.Tickets.Update(ticket);

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
                _logger.LogInformation("Ticket cancelled: {TicketId}", ticketId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CancelTicketAsync failed for {TicketId}", ticketId);
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
                return Task.FromResult($"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GenerateQRCodeAsync failed for {TicketId}", ticketId);
                return Task.FromResult(string.Empty);
            }
        }

        public async Task<List<Ticket>> GetEventTicketsAsync(string eventId, int page = 1, int pageSize = 50)
        {
            try
            {
                return await _context.Tickets
                    .Where(t => t.EventId == eventId)
                    .OrderByDescending(t => t.PurchasedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetEventTicketsAsync failed for {EventId}", eventId);
                return [];
            }
        }

        public async Task<int> GetUserTicketCountAsync(string userId)
        {
            try
            {
                return await _context.Tickets.CountAsync(t => t.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserTicketCountAsync failed for {UserId}", userId);
                return 0;
            }
        }

        public async Task ExpireOldTicketsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var expiredTickets = await _context.Tickets
                    .Join(_context.Events, t => t.EventId, e => e.Id, (t, e) => new { Ticket = t, Event = e })
                    .Where(x => x.Ticket.Status == TicketStatus.Active && x.Event.EndDate < now)
                    .Select(x => x.Ticket)
                    .ToListAsync();

                foreach (var ticket in expiredTickets)
                    ticket.Status = TicketStatus.Expired;

                if (expiredTickets.Count > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Expired {Count} ticket(s)", expiredTickets.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExpireOldTicketsAsync failed");
            }
        }
    }
}
