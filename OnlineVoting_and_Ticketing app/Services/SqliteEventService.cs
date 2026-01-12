using Microsoft.EntityFrameworkCore;
using OnlineVoting_and_Ticketing_app.Data;
using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public class SqliteEventService : IEventService
    {
        private readonly AppDbContext _context;

        public SqliteEventService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Event>> GetAllEventsAsync()
        {
            try
            {
                return await _context.Events
                    .Include(e => e.TicketTypes)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();
            }
            catch
            {
                return new List<Event>();
            }
        }

        public async Task<List<Event>> GetUpcomingEventsAsync()
        {
            try
            {
                return await _context.Events
                    .Include(e => e.TicketTypes)
                    .Where(e => e.IsPublished && e.StartDate > DateTime.UtcNow)
                    .OrderBy(e => e.StartDate)
                    .ToListAsync();
            }
            catch
            {
                return new List<Event>();
            }
        }

        public async Task<List<Event>> GetEventsByCategoryAsync(EventCategory category)
        {
            try
            {
                return await _context.Events
                    .Include(e => e.TicketTypes)
                    .Where(e => e.Category == category && e.IsPublished)
                    .ToListAsync();
            }
            catch
            {
                return new List<Event>();
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
            catch
            {
                return null;
            }
        }

        public async Task<List<Event>> GetEventsByOrganizerAsync(string organizerId)
        {
            try
            {
                return await _context.Events
                    .Include(e => e.TicketTypes)
                    .Where(e => e.OrganizerId == organizerId)
                    .ToListAsync();
            }
            catch
            {
                return new List<Event>();
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

                return (true, null, eventData.Id);
            }
            catch (Exception ex)
            {
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
                return (false, ex.Message);
            }
        }

        public async Task<bool> DeleteEventAsync(string eventId)
        {
            try
            {
                var eventToDelete = await _context.Events.FindAsync(eventId);
                if (eventToDelete == null)
                    return false;

                _context.Events.Remove(eventToDelete);
                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Event>> SearchEventsAsync(string query)
        {
            try
            {
                query = query.ToLower();

                return await _context.Events
                    .Include(e => e.TicketTypes)
                    .Where(e => e.IsPublished &&
                           (e.Title.ToLower().Contains(query) ||
                            e.Description.ToLower().Contains(query) ||
                            e.Location.ToLower().Contains(query)))
                    .ToListAsync();
            }
            catch
            {
                return new List<Event>();
            }
        }
    }
}
