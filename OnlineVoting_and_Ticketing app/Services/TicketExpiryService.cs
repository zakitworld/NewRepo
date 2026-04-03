using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineVoting_and_Ticketing_app.Data;
using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Services
{
    /// <summary>
    /// Runs once at app startup to mark tickets as Expired when their event date has passed.
    /// Call RunAsync() from App.xaml.cs after the database is initialized.
    /// </summary>
    public class TicketExpiryService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TicketExpiryService> _logger;

        public TicketExpiryService(AppDbContext context, ILogger<TicketExpiryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task RunAsync()
        {
            try
            {
                var now = DateTime.UtcNow;

                // Find all active tickets whose event end date is in the past
                var expiredTickets = await _context.Tickets
                    .Join(_context.Events,
                        t => t.EventId,
                        e => e.Id,
                        (t, e) => new { Ticket = t, Event = e })
                    .Where(x => x.Ticket.Status == TicketStatus.Active && x.Event.EndDate < now)
                    .Select(x => x.Ticket)
                    .ToListAsync();

                if (expiredTickets.Count == 0)
                {
                    _logger.LogDebug("Ticket expiry check: no tickets to expire.");
                    return;
                }

                foreach (var ticket in expiredTickets)
                    ticket.Status = TicketStatus.Expired;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Ticket expiry: marked {Count} ticket(s) as Expired.", expiredTickets.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running ticket expiry check.");
            }
        }
    }
}
