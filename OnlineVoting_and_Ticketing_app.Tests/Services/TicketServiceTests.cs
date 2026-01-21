using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OnlineVoting_and_Ticketing_app.Data;
using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Services;
using OnlineVoting_and_Ticketing_app.Tests.Helpers;
using Xunit;

namespace OnlineVoting_and_Ticketing_app.Tests.Services
{
    public class TicketServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IEventService> _mockEventService;
        private readonly SqliteTicketService _ticketService;

        public TicketServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _mockEventService = new Mock<IEventService>();
            _ticketService = new SqliteTicketService(_context, _mockEventService.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region GetUserTicketsAsync Tests

        [Fact]
        public async Task GetUserTicketsAsync_WhenNoTickets_ReturnsEmptyList()
        {
            // Act
            var result = await _ticketService.GetUserTicketsAsync("user-1");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserTicketsAsync_ReturnsOnlyUserTickets()
        {
            // Arrange
            var user1Ticket = TestDataGenerator.CreateTicket(userId: "user-1");
            var user2Ticket = TestDataGenerator.CreateTicket(userId: "user-2");

            _context.Tickets.AddRange(user1Ticket, user2Ticket);
            await _context.SaveChangesAsync();

            // Act
            var result = await _ticketService.GetUserTicketsAsync("user-1");

            // Assert
            result.Should().HaveCount(1);
            result.First().UserId.Should().Be("user-1");
        }

        [Fact]
        public async Task GetUserTicketsAsync_ReturnsTicketsOrderedByPurchaseDateDescending()
        {
            // Arrange
            var olderTicket = TestDataGenerator.CreateTicket(userId: "user-1");
            olderTicket.PurchasedAt = DateTime.UtcNow.AddDays(-10);
            olderTicket.EventTitle = "Older Event";

            var newerTicket = TestDataGenerator.CreateTicket(userId: "user-1");
            newerTicket.PurchasedAt = DateTime.UtcNow;
            newerTicket.EventTitle = "Newer Event";

            _context.Tickets.AddRange(olderTicket, newerTicket);
            await _context.SaveChangesAsync();

            // Act
            var result = await _ticketService.GetUserTicketsAsync("user-1");

            // Assert
            result.First().EventTitle.Should().Be("Newer Event");
            result.Last().EventTitle.Should().Be("Older Event");
        }

        #endregion

        #region GetTicketByIdAsync Tests

        [Fact]
        public async Task GetTicketByIdAsync_WhenTicketExists_ReturnsTicket()
        {
            // Arrange
            var ticketId = Guid.NewGuid().ToString();
            var ticket = TestDataGenerator.CreateTicket(id: ticketId);
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Act
            var result = await _ticketService.GetTicketByIdAsync(ticketId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(ticketId);
        }

        [Fact]
        public async Task GetTicketByIdAsync_WhenTicketDoesNotExist_ReturnsNull()
        {
            // Act
            var result = await _ticketService.GetTicketByIdAsync("non-existent-id");

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region PurchaseTicketAsync Tests

        [Fact]
        public async Task PurchaseTicketAsync_WithValidData_ReturnsSuccessAndTicket()
        {
            // Arrange
            var eventId = "event-1";
            var ticketTypeId = "ticket-type-1";
            var userId = "user-1";

            var eventData = TestDataGenerator.CreateEvent(id: eventId);
            eventData.TicketTypes = new List<TicketType>
            {
                TestDataGenerator.CreateTicketType(id: ticketTypeId, quantity: 10)
            };

            _mockEventService.Setup(s => s.GetEventByIdAsync(eventId))
                .ReturnsAsync(eventData);
            _mockEventService.Setup(s => s.UpdateEventAsync(It.IsAny<Event>()))
                .ReturnsAsync((true, null));

            // Act
            var (success, error, ticket) = await _ticketService.PurchaseTicketAsync(eventId, ticketTypeId, userId);

            // Assert
            success.Should().BeTrue();
            error.Should().BeNull();
            ticket.Should().NotBeNull();
            ticket!.EventId.Should().Be(eventId);
            ticket.UserId.Should().Be(userId);
        }

        [Fact]
        public async Task PurchaseTicketAsync_WhenEventNotFound_ReturnsError()
        {
            // Arrange
            _mockEventService.Setup(s => s.GetEventByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Event?)null);

            // Act
            var (success, error, ticket) = await _ticketService.PurchaseTicketAsync("event-1", "ticket-type-1", "user-1");

            // Assert
            success.Should().BeFalse();
            error.Should().Contain("Event not found");
            ticket.Should().BeNull();
        }

        [Fact]
        public async Task PurchaseTicketAsync_WhenTicketTypeNotFound_ReturnsError()
        {
            // Arrange
            var eventData = TestDataGenerator.CreateEvent();
            eventData.TicketTypes = new List<TicketType>(); // Empty ticket types

            _mockEventService.Setup(s => s.GetEventByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(eventData);

            // Act
            var (success, error, ticket) = await _ticketService.PurchaseTicketAsync(eventData.Id, "non-existent-type", "user-1");

            // Assert
            success.Should().BeFalse();
            error.Should().Contain("Ticket type not found");
        }

        [Fact]
        public async Task PurchaseTicketAsync_WhenSoldOut_ReturnsError()
        {
            // Arrange
            var eventId = "event-1";
            var ticketTypeId = "ticket-type-1";

            var eventData = TestDataGenerator.CreateEvent(id: eventId);
            eventData.TicketTypes = new List<TicketType>
            {
                TestDataGenerator.CreateTicketType(id: ticketTypeId, quantity: 0)
            };
            eventData.TicketTypes.First().AvailableQuantity = 0;

            _mockEventService.Setup(s => s.GetEventByIdAsync(eventId))
                .ReturnsAsync(eventData);

            // Act
            var (success, error, ticket) = await _ticketService.PurchaseTicketAsync(eventId, ticketTypeId, "user-1");

            // Assert
            success.Should().BeFalse();
            error.Should().Contain("sold out");
        }

        [Fact]
        public async Task PurchaseTicketAsync_GeneratesQRCode()
        {
            // Arrange
            var eventId = "event-1";
            var ticketTypeId = "ticket-type-1";

            var eventData = TestDataGenerator.CreateEvent(id: eventId);
            eventData.TicketTypes = new List<TicketType>
            {
                TestDataGenerator.CreateTicketType(id: ticketTypeId, quantity: 10)
            };

            _mockEventService.Setup(s => s.GetEventByIdAsync(eventId))
                .ReturnsAsync(eventData);
            _mockEventService.Setup(s => s.UpdateEventAsync(It.IsAny<Event>()))
                .ReturnsAsync((true, null));

            // Act
            var (success, _, ticket) = await _ticketService.PurchaseTicketAsync(eventId, ticketTypeId, "user-1");

            // Assert
            success.Should().BeTrue();
            ticket!.QRCode.Should().NotBeNullOrEmpty();
            ticket.QRCode.Should().StartWith("data:image/png;base64,");
        }

        [Fact]
        public async Task PurchaseTicketAsync_DecrementsAvailableQuantity()
        {
            // Arrange
            var eventId = "event-1";
            var ticketTypeId = "ticket-type-1";

            var eventData = TestDataGenerator.CreateEvent(id: eventId);
            eventData.TicketTypes = new List<TicketType>
            {
                TestDataGenerator.CreateTicketType(id: ticketTypeId, quantity: 10)
            };
            eventData.AvailableTickets = 10;

            _mockEventService.Setup(s => s.GetEventByIdAsync(eventId))
                .ReturnsAsync(eventData);
            _mockEventService.Setup(s => s.UpdateEventAsync(It.IsAny<Event>()))
                .ReturnsAsync((true, null));

            // Act
            await _ticketService.PurchaseTicketAsync(eventId, ticketTypeId, "user-1");

            // Assert
            _mockEventService.Verify(s => s.UpdateEventAsync(It.Is<Event>(e =>
                e.AvailableTickets == 9 &&
                e.TicketTypes.First().AvailableQuantity == 9)), Times.Once);
        }

        #endregion

        #region ValidateTicketAsync Tests

        [Fact]
        public async Task ValidateTicketAsync_WhenTicketActiveAndExists_ReturnsTrue()
        {
            // Arrange
            var ticket = TestDataGenerator.CreateTicket(status: TicketStatus.Active);
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Act
            var result = await _ticketService.ValidateTicketAsync(ticket.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateTicketAsync_WhenTicketDoesNotExist_ReturnsFalse()
        {
            // Act
            var result = await _ticketService.ValidateTicketAsync("non-existent-id");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateTicketAsync_WhenTicketUsed_ReturnsFalse()
        {
            // Arrange
            var ticket = TestDataGenerator.CreateTicket(status: TicketStatus.Used);
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Act
            var result = await _ticketService.ValidateTicketAsync(ticket.Id);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateTicketAsync_WhenTicketCancelled_ReturnsFalse()
        {
            // Arrange
            var ticket = TestDataGenerator.CreateTicket(status: TicketStatus.Cancelled);
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Act
            var result = await _ticketService.ValidateTicketAsync(ticket.Id);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region CheckInTicketAsync Tests

        [Fact]
        public async Task CheckInTicketAsync_WhenTicketActive_ReturnsTrue()
        {
            // Arrange
            var ticket = TestDataGenerator.CreateTicket(status: TicketStatus.Active);
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Act
            var result = await _ticketService.CheckInTicketAsync(ticket.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CheckInTicketAsync_UpdatesStatusToUsed()
        {
            // Arrange
            var ticket = TestDataGenerator.CreateTicket(status: TicketStatus.Active);
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Act
            await _ticketService.CheckInTicketAsync(ticket.Id);

            // Assert
            var updatedTicket = await _context.Tickets.FindAsync(ticket.Id);
            updatedTicket!.Status.Should().Be(TicketStatus.Used);
        }

        [Fact]
        public async Task CheckInTicketAsync_SetsCheckedInAt()
        {
            // Arrange
            var ticket = TestDataGenerator.CreateTicket(status: TicketStatus.Active);
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            var beforeCheckIn = DateTime.UtcNow;

            // Act
            await _ticketService.CheckInTicketAsync(ticket.Id);

            // Assert
            var updatedTicket = await _context.Tickets.FindAsync(ticket.Id);
            updatedTicket!.CheckedInAt.Should().NotBeNull();
            updatedTicket.CheckedInAt.Should().BeOnOrAfter(beforeCheckIn);
        }

        [Fact]
        public async Task CheckInTicketAsync_WhenTicketDoesNotExist_ReturnsFalse()
        {
            // Act
            var result = await _ticketService.CheckInTicketAsync("non-existent-id");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CheckInTicketAsync_WhenTicketAlreadyUsed_ReturnsFalse()
        {
            // Arrange
            var ticket = TestDataGenerator.CreateTicket(status: TicketStatus.Used);
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Act
            var result = await _ticketService.CheckInTicketAsync(ticket.Id);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region CancelTicketAsync Tests

        [Fact]
        public async Task CancelTicketAsync_WhenTicketExists_ReturnsTrue()
        {
            // Arrange
            var ticket = TestDataGenerator.CreateTicket();
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            _mockEventService.Setup(s => s.GetEventByIdAsync(ticket.EventId))
                .ReturnsAsync((Event?)null);

            // Act
            var result = await _ticketService.CancelTicketAsync(ticket.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CancelTicketAsync_UpdatesStatusToCancelled()
        {
            // Arrange
            var ticket = TestDataGenerator.CreateTicket(status: TicketStatus.Active);
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            _mockEventService.Setup(s => s.GetEventByIdAsync(ticket.EventId))
                .ReturnsAsync((Event?)null);

            // Act
            await _ticketService.CancelTicketAsync(ticket.Id);

            // Assert
            var cancelledTicket = await _context.Tickets.FindAsync(ticket.Id);
            cancelledTicket!.Status.Should().Be(TicketStatus.Cancelled);
        }

        [Fact]
        public async Task CancelTicketAsync_RestoresTicketAvailability()
        {
            // Arrange
            var eventId = "event-1";
            var ticketTypeId = "ticket-type-1";

            var ticket = TestDataGenerator.CreateTicket(eventId: eventId, ticketTypeId: ticketTypeId);
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            var eventData = TestDataGenerator.CreateEvent(id: eventId);
            eventData.TicketTypes = new List<TicketType>
            {
                TestDataGenerator.CreateTicketType(id: ticketTypeId, quantity: 10)
            };
            eventData.TicketTypes.First().AvailableQuantity = 5;
            eventData.AvailableTickets = 5;

            _mockEventService.Setup(s => s.GetEventByIdAsync(eventId))
                .ReturnsAsync(eventData);
            _mockEventService.Setup(s => s.UpdateEventAsync(It.IsAny<Event>()))
                .ReturnsAsync((true, null));

            // Act
            await _ticketService.CancelTicketAsync(ticket.Id);

            // Assert
            _mockEventService.Verify(s => s.UpdateEventAsync(It.Is<Event>(e =>
                e.AvailableTickets == 6 &&
                e.TicketTypes.First().AvailableQuantity == 6)), Times.Once);
        }

        [Fact]
        public async Task CancelTicketAsync_WhenTicketDoesNotExist_ReturnsFalse()
        {
            // Act
            var result = await _ticketService.CancelTicketAsync("non-existent-id");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GenerateQRCodeAsync Tests

        [Fact]
        public async Task GenerateQRCodeAsync_ReturnsBase64EncodedPng()
        {
            // Act
            var result = await _ticketService.GenerateQRCodeAsync("ticket-123");

            // Assert
            result.Should().StartWith("data:image/png;base64,");
            result.Length.Should().BeGreaterThan(50); // Should be a substantial string
        }

        [Fact]
        public async Task GenerateQRCodeAsync_ReturnsUniqueCodesForDifferentIds()
        {
            // Act
            var code1 = await _ticketService.GenerateQRCodeAsync("ticket-1");
            var code2 = await _ticketService.GenerateQRCodeAsync("ticket-2");

            // Assert
            code1.Should().NotBe(code2);
        }

        #endregion

        #region GetEventTicketsAsync Tests

        [Fact]
        public async Task GetEventTicketsAsync_ReturnsOnlyEventTickets()
        {
            // Arrange
            var event1Ticket = TestDataGenerator.CreateTicket(eventId: "event-1");
            var event2Ticket = TestDataGenerator.CreateTicket(eventId: "event-2");

            _context.Tickets.AddRange(event1Ticket, event2Ticket);
            await _context.SaveChangesAsync();

            // Act
            var result = await _ticketService.GetEventTicketsAsync("event-1");

            // Assert
            result.Should().HaveCount(1);
            result.First().EventId.Should().Be("event-1");
        }

        [Fact]
        public async Task GetEventTicketsAsync_WhenNoTickets_ReturnsEmptyList()
        {
            // Act
            var result = await _ticketService.GetEventTicketsAsync("event-1");

            // Assert
            result.Should().BeEmpty();
        }

        #endregion
    }
}
