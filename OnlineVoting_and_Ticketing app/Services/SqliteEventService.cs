using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineVoting_and_Ticketing_app.Data;
using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public class SqliteEventService : IEventService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SqliteEventService> _logger;

        public SqliteEventService(AppDbContext context, ILogger<SqliteEventService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Event>> GetAllEventsAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Events
                    .Include(e => e.TicketTypes)
                    .OrderByDescending(e => e.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllEventsAsync failed");
                return [];
            }
        }

        public async Task<List<Event>> GetUpcomingEventsAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Events
                    .Include(e => e.TicketTypes)
                    .Where(e => e.IsPublished && e.StartDate > DateTime.UtcNow)
                    .OrderBy(e => e.StartDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUpcomingEventsAsync failed");
                return [];
            }
        }

        public async Task<List<Event>> GetEventsByCategoryAsync(EventCategory category, int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Events
                    .Include(e => e.TicketTypes)
                    .Where(e => e.Category == category && e.IsPublished)
                    .OrderBy(e => e.StartDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetEventsByCategoryAsync failed for {Category}", category);
                return [];
            }
        }

        public async Task<Event?> GetEventByIdAsync(string eventId)
        {
            try
            {
                return await _context.Events
                    .Include(e => e.TicketTypes)
                    .FirstOrDefaultAsync(e => e.Id == eventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetEventByIdAsync failed for {EventId}", eventId);
                return null;
            }
        }

        public async Task<List<Event>> GetEventsByOrganizerAsync(string organizerId, int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Events
                    .Include(e => e.TicketTypes)
                    .Where(e => e.OrganizerId == organizerId)
                    .OrderByDescending(e => e.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetEventsByOrganizerAsync failed for {OrganizerId}", organizerId);
                return [];
            }
        }

        public async Task<(bool Success, string? Error, string? EventId)> CreateEventAsync(Event eventData)
        {
            try
            {
                eventData.Id = Guid.NewGuid().ToString();
                eventData.CreatedAt = DateTime.UtcNow;
                eventData.UpdatedAt = DateTime.UtcNow;

                _context.Events.Add(eventData);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Event created: {EventId} '{Title}'", eventData.Id, eventData.Title);
                return (true, null, eventData.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateEventAsync failed for '{Title}'", eventData.Title);
                return (false, ex.Message, null);
            }
        }

        public async Task<(bool Success, string? Error)> UpdateEventAsync(Event eventData)
        {
            try
            {
                eventData.UpdatedAt = DateTime.UtcNow;
                _context.Events.Update(eventData);
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateEventAsync failed for {EventId}", eventData.Id);
                return (false, ex.Message);
            }
        }

        public async Task<bool> DeleteEventAsync(string eventId)
        {
            try
            {
                var ev = await _context.Events.FindAsync(eventId);
                if (ev == null) return false;
                _context.Events.Remove(ev);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Event deleted: {EventId}", eventId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteEventAsync failed for {EventId}", eventId);
                return false;
            }
        }

        public async Task<List<Event>> SearchEventsAsync(string query, int page = 1, int pageSize = 20)
        {
            try
            {
                var lower = query.ToLower();
                return await _context.Events
                    .Include(e => e.TicketTypes)
                    .Where(e => e.IsPublished &&
                           (e.Title.ToLower().Contains(lower) ||
                            e.Description.ToLower().Contains(lower) ||
                            e.Location.ToLower().Contains(lower)))
                    .OrderBy(e => e.StartDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SearchEventsAsync failed for query '{Query}'", query);
                return [];
            }
        }

        public async Task<int> GetTotalEventsCountAsync(string? query = null, EventCategory? category = null)
        {
            try
            {
                var q = _context.Events.Where(e => e.IsPublished);

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var lower = query.ToLower();
                    q = q.Where(e => e.Title.ToLower().Contains(lower) ||
                                     e.Description.ToLower().Contains(lower) ||
                                     e.Location.ToLower().Contains(lower));
                }

                if (category.HasValue)
                    q = q.Where(e => e.Category == category.Value);

                return await q.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTotalEventsCountAsync failed");
                return 0;
            }
        }
    }
}
