using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public interface IEventService
    {
        /// <param name="page">1-based page number</param>
        /// <param name="pageSize">Items per page (default 20)</param>
        Task<List<Event>> GetAllEventsAsync(int page = 1, int pageSize = 20);

        Task<List<Event>> GetUpcomingEventsAsync(int page = 1, int pageSize = 20);
        Task<List<Event>> GetEventsByCategoryAsync(EventCategory category, int page = 1, int pageSize = 20);
        Task<Event?> GetEventByIdAsync(string eventId);
        Task<List<Event>> GetEventsByOrganizerAsync(string organizerId, int page = 1, int pageSize = 20);
        Task<(bool Success, string? Error, string? EventId)> CreateEventAsync(Event eventData);
        Task<(bool Success, string? Error)> UpdateEventAsync(Event eventData);
        Task<bool> DeleteEventAsync(string eventId);
        Task<List<Event>> SearchEventsAsync(string query, int page = 1, int pageSize = 20);
        Task<int> GetTotalEventsCountAsync(string? query = null, EventCategory? category = null);
    }
}
