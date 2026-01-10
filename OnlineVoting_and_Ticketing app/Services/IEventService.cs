using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public interface IEventService
    {
        Task<List<Event>> GetAllEventsAsync();
        Task<List<Event>> GetUpcomingEventsAsync();
        Task<List<Event>> GetEventsByCategoryAsync(EventCategory category);
        Task<Event?> GetEventByIdAsync(string eventId);
        Task<List<Event>> GetEventsByOrganizerAsync(string organizerId);
        Task<(bool Success, string? Error, string? EventId)> CreateEventAsync(Event eventData);
        Task<(bool Success, string? Error)> UpdateEventAsync(Event eventData);
        Task<bool> DeleteEventAsync(string eventId);
        Task<List<Event>> SearchEventsAsync(string query);
    }
}
