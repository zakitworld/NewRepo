namespace OnlineVoting_and_Ticketing_app.Models
{
    public class Event
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string OrganizerId { get; set; } = string.Empty;
        public string OrganizerName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public EventCategory Category { get; set; }
        public EventType Type { get; set; }
        public EventStatus Status { get; set; } = EventStatus.Draft;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public List<TicketType> TicketTypes { get; set; } = new();
        public int TotalTickets { get; set; }
        public int AvailableTickets { get; set; }
        public bool IsPublished { get; set; }
    }

    public class TicketType
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int TotalQuantity { get; set; }
        public int AvailableQuantity { get; set; }
    }

    public enum EventCategory
    {
        Music,
        Sports,
        Conference,
        Workshop,
        Exhibition,
        Festival,
        Theater,
        Comedy,
        Other
    }

    public enum EventType
    {
        InPerson,
        Virtual,
        Hybrid
    }

    public enum EventStatus
    {
        Draft,
        Published,
        Ongoing,
        Completed,
        Cancelled
    }
}
