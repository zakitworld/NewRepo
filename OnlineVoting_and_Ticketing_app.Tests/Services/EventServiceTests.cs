using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OnlineVoting_and_Ticketing_app.Data;
using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Services;
using OnlineVoting_and_Ticketing_app.Tests.Helpers;
using Xunit;

namespace OnlineVoting_and_Ticketing_app.Tests.Services
{
    public class EventServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly SqliteEventService _eventService;

        public EventServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _eventService = new SqliteEventService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region GetAllEventsAsync Tests

        [Fact]
        public async Task GetAllEventsAsync_WhenNoEvents_ReturnsEmptyList()
        {
            // Act
            var result = await _eventService.GetAllEventsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllEventsAsync_WhenEventsExist_ReturnsAllEvents()
        {
            // Arrange
            var event1 = TestDataGenerator.CreateEvent(title: "Event 1");
            var event2 = TestDataGenerator.CreateEvent(title: "Event 2");
            _context.Events.AddRange(event1, event2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetAllEventsAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(e => e.Title == "Event 1");
            result.Should().Contain(e => e.Title == "Event 2");
        }

        [Fact]
        public async Task GetAllEventsAsync_ReturnsEventsOrderedByCreatedAtDescending()
        {
            // Arrange
            var olderEvent = TestDataGenerator.CreateEvent(title: "Older Event");
            olderEvent.CreatedAt = DateTime.UtcNow.AddDays(-10);

            var newerEvent = TestDataGenerator.CreateEvent(title: "Newer Event");
            newerEvent.CreatedAt = DateTime.UtcNow;

            _context.Events.AddRange(olderEvent, newerEvent);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetAllEventsAsync();

            // Assert
            result.First().Title.Should().Be("Newer Event");
            result.Last().Title.Should().Be("Older Event");
        }

        [Fact]
        public async Task GetAllEventsAsync_IncludesTicketTypes()
        {
            // Arrange
            var eventWithTickets = TestDataGenerator.CreateEvent();
            eventWithTickets.TicketTypes.Add(TestDataGenerator.CreateTicketType(name: "VIP"));
            _context.Events.Add(eventWithTickets);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetAllEventsAsync();

            // Assert
            result.First().TicketTypes.Should().NotBeEmpty();
        }

        #endregion

        #region GetUpcomingEventsAsync Tests

        [Fact]
        public async Task GetUpcomingEventsAsync_ReturnsOnlyFuturePublishedEvents()
        {
            // Arrange
            var futurePublished = TestDataGenerator.CreateEvent(
                title: "Future Published",
                startDate: DateTime.UtcNow.AddDays(7),
                status: EventStatus.Published);
            futurePublished.IsPublished = true;

            var pastEvent = TestDataGenerator.CreateEvent(
                title: "Past Event",
                startDate: DateTime.UtcNow.AddDays(-7),
                status: EventStatus.Published);
            pastEvent.IsPublished = true;

            var futureDraft = TestDataGenerator.CreateEvent(
                title: "Future Draft",
                startDate: DateTime.UtcNow.AddDays(7),
                status: EventStatus.Draft);
            futureDraft.IsPublished = false;

            _context.Events.AddRange(futurePublished, pastEvent, futureDraft);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetUpcomingEventsAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Future Published");
        }

        [Fact]
        public async Task GetUpcomingEventsAsync_ReturnsEventsOrderedByStartDate()
        {
            // Arrange
            var laterEvent = TestDataGenerator.CreateEvent(
                title: "Later Event",
                startDate: DateTime.UtcNow.AddDays(14));
            laterEvent.IsPublished = true;

            var soonerEvent = TestDataGenerator.CreateEvent(
                title: "Sooner Event",
                startDate: DateTime.UtcNow.AddDays(3));
            soonerEvent.IsPublished = true;

            _context.Events.AddRange(laterEvent, soonerEvent);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetUpcomingEventsAsync();

            // Assert
            result.First().Title.Should().Be("Sooner Event");
            result.Last().Title.Should().Be("Later Event");
        }

        #endregion

        #region GetEventsByCategoryAsync Tests

        [Fact]
        public async Task GetEventsByCategoryAsync_ReturnsOnlyMatchingCategory()
        {
            // Arrange
            var musicEvent = TestDataGenerator.CreateEvent(title: "Music Event", category: EventCategory.Music);
            musicEvent.IsPublished = true;

            var sportsEvent = TestDataGenerator.CreateEvent(title: "Sports Event", category: EventCategory.Sports);
            sportsEvent.IsPublished = true;

            _context.Events.AddRange(musicEvent, sportsEvent);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetEventsByCategoryAsync(EventCategory.Music);

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Music Event");
        }

        [Fact]
        public async Task GetEventsByCategoryAsync_ReturnsOnlyPublishedEvents()
        {
            // Arrange
            var publishedMusic = TestDataGenerator.CreateEvent(title: "Published Music", category: EventCategory.Music);
            publishedMusic.IsPublished = true;

            var draftMusic = TestDataGenerator.CreateEvent(title: "Draft Music", category: EventCategory.Music);
            draftMusic.IsPublished = false;

            _context.Events.AddRange(publishedMusic, draftMusic);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetEventsByCategoryAsync(EventCategory.Music);

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Published Music");
        }

        #endregion

        #region GetEventByIdAsync Tests

        [Fact]
        public async Task GetEventByIdAsync_WhenEventExists_ReturnsEvent()
        {
            // Arrange
            var eventId = Guid.NewGuid().ToString();
            var testEvent = TestDataGenerator.CreateEvent(id: eventId, title: "Test Event");
            _context.Events.Add(testEvent);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetEventByIdAsync(eventId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(eventId);
            result.Title.Should().Be("Test Event");
        }

        [Fact]
        public async Task GetEventByIdAsync_WhenEventDoesNotExist_ReturnsNull()
        {
            // Act
            var result = await _eventService.GetEventByIdAsync("non-existent-id");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetEventByIdAsync_IncludesTicketTypes()
        {
            // Arrange
            var eventId = Guid.NewGuid().ToString();
            var testEvent = TestDataGenerator.CreateEvent(id: eventId);
            _context.Events.Add(testEvent);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetEventByIdAsync(eventId);

            // Assert
            result.Should().NotBeNull();
            result!.TicketTypes.Should().NotBeEmpty();
        }

        #endregion

        #region GetEventsByOrganizerAsync Tests

        [Fact]
        public async Task GetEventsByOrganizerAsync_ReturnsOnlyOrganizerEvents()
        {
            // Arrange
            var organizer1Event = TestDataGenerator.CreateEvent(title: "Organizer 1 Event", organizerId: "org-1");
            var organizer2Event = TestDataGenerator.CreateEvent(title: "Organizer 2 Event", organizerId: "org-2");

            _context.Events.AddRange(organizer1Event, organizer2Event);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.GetEventsByOrganizerAsync("org-1");

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Organizer 1 Event");
        }

        [Fact]
        public async Task GetEventsByOrganizerAsync_WhenNoEvents_ReturnsEmptyList()
        {
            // Act
            var result = await _eventService.GetEventsByOrganizerAsync("non-existent-organizer");

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region CreateEventAsync Tests

        [Fact]
        public async Task CreateEventAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var newEvent = TestDataGenerator.CreateEvent(title: "New Event");

            // Act
            var (success, error, eventId) = await _eventService.CreateEventAsync(newEvent);

            // Assert
            success.Should().BeTrue();
            error.Should().BeNull();
            eventId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CreateEventAsync_GeneratesNewId()
        {
            // Arrange
            var newEvent = TestDataGenerator.CreateEvent(id: "original-id");

            // Act
            var (success, error, eventId) = await _eventService.CreateEventAsync(newEvent);

            // Assert
            success.Should().BeTrue();
            eventId.Should().NotBe("original-id");
        }

        [Fact]
        public async Task CreateEventAsync_SetsCreatedAtAndUpdatedAt()
        {
            // Arrange
            var newEvent = TestDataGenerator.CreateEvent();
            var beforeCreate = DateTime.UtcNow;

            // Act
            var (success, _, eventId) = await _eventService.CreateEventAsync(newEvent);
            var createdEvent = await _eventService.GetEventByIdAsync(eventId!);

            // Assert
            createdEvent!.CreatedAt.Should().BeOnOrAfter(beforeCreate);
            createdEvent.UpdatedAt.Should().BeOnOrAfter(beforeCreate);
        }

        [Fact]
        public async Task CreateEventAsync_PersistsEventToDatabase()
        {
            // Arrange
            var newEvent = TestDataGenerator.CreateEvent(title: "Persisted Event");

            // Act
            var (_, _, eventId) = await _eventService.CreateEventAsync(newEvent);

            // Assert
            var persistedEvent = await _context.Events.FindAsync(eventId);
            persistedEvent.Should().NotBeNull();
            persistedEvent!.Title.Should().Be("Persisted Event");
        }

        #endregion

        #region UpdateEventAsync Tests

        [Fact]
        public async Task UpdateEventAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var existingEvent = TestDataGenerator.CreateEvent(title: "Original Title");
            _context.Events.Add(existingEvent);
            await _context.SaveChangesAsync();
            _context.Entry(existingEvent).State = EntityState.Detached;

            existingEvent.Title = "Updated Title";

            // Act
            var (success, error) = await _eventService.UpdateEventAsync(existingEvent);

            // Assert
            success.Should().BeTrue();
            error.Should().BeNull();
        }

        [Fact]
        public async Task UpdateEventAsync_UpdatesUpdatedAtTimestamp()
        {
            // Arrange
            var existingEvent = TestDataGenerator.CreateEvent();
            existingEvent.UpdatedAt = DateTime.UtcNow.AddDays(-5);
            _context.Events.Add(existingEvent);
            await _context.SaveChangesAsync();
            _context.Entry(existingEvent).State = EntityState.Detached;

            var beforeUpdate = DateTime.UtcNow;
            existingEvent.Title = "Updated";

            // Act
            await _eventService.UpdateEventAsync(existingEvent);

            // Assert
            var updatedEvent = await _context.Events.FindAsync(existingEvent.Id);
            updatedEvent!.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
        }

        #endregion

        #region DeleteEventAsync Tests

        [Fact]
        public async Task DeleteEventAsync_WhenEventExists_ReturnsTrue()
        {
            // Arrange
            var eventToDelete = TestDataGenerator.CreateEvent();
            _context.Events.Add(eventToDelete);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.DeleteEventAsync(eventToDelete.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteEventAsync_WhenEventDoesNotExist_ReturnsFalse()
        {
            // Act
            var result = await _eventService.DeleteEventAsync("non-existent-id");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteEventAsync_RemovesEventFromDatabase()
        {
            // Arrange
            var eventToDelete = TestDataGenerator.CreateEvent();
            _context.Events.Add(eventToDelete);
            await _context.SaveChangesAsync();

            // Act
            await _eventService.DeleteEventAsync(eventToDelete.Id);

            // Assert
            var deletedEvent = await _context.Events.FindAsync(eventToDelete.Id);
            deletedEvent.Should().BeNull();
        }

        #endregion

        #region SearchEventsAsync Tests

        [Fact]
        public async Task SearchEventsAsync_FindsEventsByTitle()
        {
            // Arrange
            var targetEvent = TestDataGenerator.CreateEvent(title: "Jazz Concert");
            targetEvent.IsPublished = true;

            var otherEvent = TestDataGenerator.CreateEvent(title: "Rock Show");
            otherEvent.IsPublished = true;

            _context.Events.AddRange(targetEvent, otherEvent);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.SearchEventsAsync("Jazz");

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Jazz Concert");
        }

        [Fact]
        public async Task SearchEventsAsync_FindsEventsByDescription()
        {
            // Arrange
            var targetEvent = TestDataGenerator.CreateEvent(title: "Event 1");
            targetEvent.Description = "A wonderful evening of classical music";
            targetEvent.IsPublished = true;

            _context.Events.Add(targetEvent);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.SearchEventsAsync("classical");

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task SearchEventsAsync_FindsEventsByLocation()
        {
            // Arrange
            var targetEvent = TestDataGenerator.CreateEvent(title: "Event");
            targetEvent.Location = "Madison Square Garden";
            targetEvent.IsPublished = true;

            _context.Events.Add(targetEvent);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.SearchEventsAsync("Madison");

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task SearchEventsAsync_IsCaseInsensitive()
        {
            // Arrange
            var targetEvent = TestDataGenerator.CreateEvent(title: "JAZZ Concert");
            targetEvent.IsPublished = true;

            _context.Events.Add(targetEvent);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.SearchEventsAsync("jazz");

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task SearchEventsAsync_ReturnsOnlyPublishedEvents()
        {
            // Arrange
            var publishedEvent = TestDataGenerator.CreateEvent(title: "Jazz Published");
            publishedEvent.IsPublished = true;

            var draftEvent = TestDataGenerator.CreateEvent(title: "Jazz Draft");
            draftEvent.IsPublished = false;

            _context.Events.AddRange(publishedEvent, draftEvent);
            await _context.SaveChangesAsync();

            // Act
            var result = await _eventService.SearchEventsAsync("Jazz");

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Jazz Published");
        }

        #endregion
    }
}
