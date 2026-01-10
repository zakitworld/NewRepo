using Firebase.Database;
using Firebase.Database.Query;
using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public class FirebaseEventService : IEventService
    {
        private readonly FirebaseClient _firebaseClient;

        public FirebaseEventService()
        {
            _firebaseClient = new FirebaseClient(FirebaseConfig.DatabaseUrl);
        }

        public async Task<List<Event>> GetAllEventsAsync()
        {
            try
            {
                var events = await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionEvents)
                    .OnceAsync<Event>();

                return events
                    .Select(e => new Event
                    {
                        Id = e.Key,
                        Title = e.Object.Title,
                        Description = e.Object.Description,
                        Location = e.Object.Location,
                        ImageUrl = e.Object.ImageUrl,
                        OrganizerId = e.Object.OrganizerId,
                        OrganizerName = e.Object.OrganizerName,
                        StartDate = e.Object.StartDate,
                        EndDate = e.Object.EndDate,
                        Category = e.Object.Category,
                        Type = e.Object.Type,
                        Status = e.Object.Status,
                        TicketTypes = e.Object.TicketTypes,
                        TotalTickets = e.Object.TotalTickets,
                        AvailableTickets = e.Object.AvailableTickets,
                        IsPublished = e.Object.IsPublished,
                        CreatedAt = e.Object.CreatedAt,
                        UpdatedAt = e.Object.UpdatedAt
                    })
                    .OrderByDescending(e => e.CreatedAt)
                    .ToList();
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
                var allEvents = await GetAllEventsAsync();
                return allEvents
                    .Where(e => e.IsPublished && e.StartDate > DateTime.UtcNow)
                    .OrderBy(e => e.StartDate)
                    .ToList();
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
                var allEvents = await GetAllEventsAsync();
                return allEvents
                    .Where(e => e.Category == category && e.IsPublished)
                    .ToList();
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
                var eventData = await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionEvents)
                    .Child(eventId)
                    .OnceSingleAsync<Event>();

                if (eventData == null)
                    return null;

                eventData.Id = eventId;
                return eventData;
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
                var allEvents = await GetAllEventsAsync();
                return allEvents
                    .Where(e => e.OrganizerId == organizerId)
                    .ToList();
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
                eventData.CreatedAt = DateTime.UtcNow;
                eventData.UpdatedAt = DateTime.UtcNow;

                var result = await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionEvents)
                    .PostAsync(eventData);

                return (true, null, result.Key);
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

                await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionEvents)
                    .Child(eventData.Id)
                    .PutAsync(eventData);

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
                await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionEvents)
                    .Child(eventId)
                    .DeleteAsync();

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
                var allEvents = await GetAllEventsAsync();
                query = query.ToLower();

                return allEvents
                    .Where(e => e.IsPublished &&
                           (e.Title.ToLower().Contains(query) ||
                            e.Description.ToLower().Contains(query) ||
                            e.Location.ToLower().Contains(query)))
                    .ToList();
            }
            catch
            {
                return new List<Event>();
            }
        }
    }
}
