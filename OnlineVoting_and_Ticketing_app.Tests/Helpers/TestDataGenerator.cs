using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Data;

namespace OnlineVoting_and_Ticketing_app.Tests.Helpers
{
    public static class TestDataGenerator
    {
        public static Event CreateEvent(
            string? id = null,
            string title = "Test Event",
            string organizerId = "organizer-1",
            EventCategory category = EventCategory.Music,
            EventStatus status = EventStatus.Published,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            return new Event
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Title = title,
                Description = "Test event description",
                Location = "Test Location",
                ImageUrl = "https://example.com/image.jpg",
                OrganizerId = organizerId,
                OrganizerName = "Test Organizer",
                StartDate = startDate ?? DateTime.UtcNow.AddDays(7),
                EndDate = endDate ?? DateTime.UtcNow.AddDays(8),
                Category = category,
                Type = EventType.InPerson,
                Status = status,
                IsPublished = status == EventStatus.Published,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TicketTypes = new List<TicketType>
                {
                    CreateTicketType()
                },
                TotalTickets = 100,
                AvailableTickets = 100
            };
        }

        public static TicketType CreateTicketType(
            string? id = null,
            string name = "General Admission",
            decimal price = 50.00m,
            int quantity = 100)
        {
            return new TicketType
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Name = name,
                Description = "Standard ticket",
                Price = price,
                TotalQuantity = quantity,
                AvailableQuantity = quantity
            };
        }

        public static Poll CreatePoll(
            string? id = null,
            string title = "Test Poll",
            string creatorId = "creator-1",
            PollStatus status = PollStatus.Active,
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool allowMultipleChoices = false)
        {
            var poll = new Poll
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Title = title,
                Description = "Test poll description",
                CreatorId = creatorId,
                CreatorName = "Test Creator",
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-1),
                EndDate = endDate ?? DateTime.UtcNow.AddDays(7),
                Status = status,
                Type = allowMultipleChoices ? PollType.MultipleChoice : PollType.SingleChoice,
                AllowMultipleChoices = allowMultipleChoices,
                IsAnonymous = false,
                RequireAuthentication = true,
                TotalVotes = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Options = new List<PollOption>()
            };

            // Add default options
            poll.Options.Add(CreatePollOption(pollId: poll.Id, text: "Option 1", order: 0));
            poll.Options.Add(CreatePollOption(pollId: poll.Id, text: "Option 2", order: 1));

            return poll;
        }

        public static PollOption CreatePollOption(
            string? id = null,
            string pollId = "",
            string text = "Test Option",
            int order = 0,
            int voteCount = 0)
        {
            return new PollOption
            {
                Id = id ?? Guid.NewGuid().ToString(),
                PollId = pollId,
                Text = text,
                Order = order,
                VoteCount = voteCount
            };
        }

        public static Vote CreateVote(
            string? id = null,
            string pollId = "poll-1",
            string userId = "user-1",
            List<string>? selectedOptionIds = null)
        {
            return new Vote
            {
                Id = id ?? Guid.NewGuid().ToString(),
                PollId = pollId,
                UserId = userId,
                SelectedOptionIds = selectedOptionIds ?? new List<string> { "option-1" },
                VotedAt = DateTime.UtcNow
            };
        }

        public static Ticket CreateTicket(
            string? id = null,
            string eventId = "event-1",
            string userId = "user-1",
            string ticketTypeId = "ticket-type-1",
            TicketStatus status = TicketStatus.Active,
            PaymentStatus paymentStatus = PaymentStatus.Completed)
        {
            return new Ticket
            {
                Id = id ?? Guid.NewGuid().ToString(),
                EventId = eventId,
                EventTitle = "Test Event",
                UserId = userId,
                UserName = "Test User",
                UserEmail = "test@example.com",
                TicketTypeId = ticketTypeId,
                TicketTypeName = "General Admission",
                Price = 50.00m,
                QRCode = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                Status = status,
                PurchasedAt = DateTime.UtcNow,
                TransactionId = Guid.NewGuid().ToString(),
                PaymentStatus = paymentStatus
            };
        }

        public static User CreateUser(
            string? id = null,
            string email = "test@example.com",
            string fullName = "Test User")
        {
            return new User
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Email = email,
                FullName = fullName,
                PhoneNumber = "+1234567890",
                Role = UserRole.User,
                IsActive = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static ApplicationUser CreateApplicationUser(
            string? id = null,
            string email = "test@example.com",
            string fullName = "Test User")
        {
            return new ApplicationUser
            {
                Id = id ?? Guid.NewGuid().ToString(),
                UserName = email,
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                NormalizedUserName = email.ToUpperInvariant(),
                FullName = fullName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString()
            };
        }
    }
}
