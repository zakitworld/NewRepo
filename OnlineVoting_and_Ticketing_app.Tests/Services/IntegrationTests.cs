using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Repositories;
using OnlineVoting_and_Ticketing_app.Services;
using OnlineVoting_and_Ticketing_app.Tests.Helpers;
using Xunit;

namespace OnlineVoting_and_Ticketing_app.Tests.Services
{
    /// <summary>
    /// Integration tests that verify service-to-database contracts using a real
    /// in-memory SQLite database (not EF InMemory) for accurate SQL behaviour.
    /// </summary>
    public class IntegrationTests : IDisposable
    {
        private readonly OnlineVoting_and_Ticketing_app.Data.AppDbContext _context;
        private readonly SqliteEventService _eventService;
        private readonly SqlitePollService _pollService;
        private readonly SqliteTicketService _ticketService;
        private readonly Mock<IEventService> _mockEventSvc;

        public IntegrationTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _eventService = new SqliteEventService(_context, NullLogger<SqliteEventService>.Instance);
            _pollService = new SqlitePollService(_context, NullLogger<SqlitePollService>.Instance);
            _mockEventSvc = new Mock<IEventService>();
            _ticketService = new SqliteTicketService(
                _context, _mockEventSvc.Object, NullLogger<SqliteTicketService>.Instance);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ── Pagination ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllEventsAsync_Pagination_ReturnsCorrectPage()
        {
            // Arrange — seed 5 events
            for (int i = 1; i <= 5; i++)
            {
                var ev = TestDataGenerator.CreateEvent(title: $"Event {i}");
                ev.CreatedAt = DateTime.UtcNow.AddMinutes(-i); // stagger timestamps
                _context.Events.Add(ev);
            }
            await _context.SaveChangesAsync();

            // Act — page 1 with size 3
            var page1 = await _eventService.GetAllEventsAsync(page: 1, pageSize: 3);
            var page2 = await _eventService.GetAllEventsAsync(page: 2, pageSize: 3);

            // Assert
            page1.Should().HaveCount(3);
            page2.Should().HaveCount(2);
            page1.Select(e => e.Title).Should().NotIntersectWith(page2.Select(e => e.Title));
        }

        [Fact]
        public async Task GetAllEventsAsync_Pagination_EmptyPageBeyondResults()
        {
            _context.Events.Add(TestDataGenerator.CreateEvent());
            await _context.SaveChangesAsync();

            var page2 = await _eventService.GetAllEventsAsync(page: 2, pageSize: 20);

            page2.Should().BeEmpty();
        }

        [Fact]
        public async Task GetActivePollsAsync_Pagination_ReturnsCorrectPage()
        {
            var now = DateTime.UtcNow;
            for (int i = 1; i <= 4; i++)
            {
                var poll = TestDataGenerator.CreatePoll(title: $"Poll {i}");
                poll.Status = PollStatus.Active;
                poll.StartDate = now.AddMinutes(-1);
                poll.EndDate = now.AddDays(7);
                _context.Polls.Add(poll);
            }
            await _context.SaveChangesAsync();

            var page1 = await _pollService.GetActivePollsAsync(page: 1, pageSize: 3);
            var page2 = await _pollService.GetActivePollsAsync(page: 2, pageSize: 3);

            page1.Should().HaveCount(3);
            page2.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetTotalEventsCountAsync_ReturnsCorrectCount()
        {
            for (int i = 0; i < 3; i++)
            {
                var ev = TestDataGenerator.CreateEvent();
                ev.IsPublished = true;
                _context.Events.Add(ev);
            }
            // Add one unpublished — should not count
            _context.Events.Add(TestDataGenerator.CreateEvent());
            await _context.SaveChangesAsync();

            var count = await _eventService.GetTotalEventsCountAsync();

            count.Should().Be(3);
        }

        // ── Repository / Unit of Work ────────────────────────────────────────

        [Fact]
        public async Task Repository_GetPagedAsync_ReturnsOrderedPage()
        {
            var uow = new UnitOfWork(_context);

            for (int i = 1; i <= 6; i++)
            {
                var ev = TestDataGenerator.CreateEvent(title: $"R{i}");
                ev.CreatedAt = DateTime.UtcNow.AddMinutes(-i);
                await uow.Events.AddAsync(ev);
            }
            await uow.SaveChangesAsync();

            var page = await uow.Events.GetPagedAsync(
                page: 2,
                pageSize: 3,
                orderBy: q => q.OrderByDescending(e => e.CreatedAt));

            page.Should().HaveCount(3);
        }

        [Fact]
        public async Task Repository_AnyAsync_ReturnsTrueWhenMatchExists()
        {
            var uow = new UnitOfWork(_context);
            var ev = TestDataGenerator.CreateEvent(title: "Searchable");
            await uow.Events.AddAsync(ev);
            await uow.SaveChangesAsync();

            var exists = await uow.Events.AnyAsync(e => e.Title == "Searchable");

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task UnitOfWork_SaveChangesAsync_CommitsAllRepositoryChanges()
        {
            var uow = new UnitOfWork(_context);

            var ev1 = TestDataGenerator.CreateEvent(title: "Batch 1");
            var ev2 = TestDataGenerator.CreateEvent(title: "Batch 2");
            await uow.Events.AddAsync(ev1);
            await uow.Events.AddAsync(ev2);
            await uow.SaveChangesAsync();

            var all = await uow.Events.GetAllAsync();
            all.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        // ── Ticket QR check-in flow ─────────────────────────────────────────

        [Fact]
        public async Task CheckInByQRCodeAsync_ValidActiveTicket_ReturnsTrue()
        {
            var ticket = new Ticket
            {
                Id = Guid.NewGuid().ToString(),
                EventId = "evt-1",
                EventTitle = "Test Event",
                UserId = "user-1",
                TicketTypeId = "tt-1",
                TicketTypeName = "General",
                Price = 10,
                Status = TicketStatus.Active,
                PaymentStatus = PaymentStatus.Completed,
                PurchasedAt = DateTime.UtcNow
            };
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            var result = await _ticketService.CheckInByQRCodeAsync(ticket.Id);

            result.Should().BeTrue();
            var updated = await _context.Tickets.FindAsync(ticket.Id);
            updated!.Status.Should().Be(TicketStatus.Used);
            updated.CheckedInAt.Should().NotBeNull();
        }

        [Fact]
        public async Task CheckInByQRCodeAsync_AlreadyUsedTicket_ReturnsFalse()
        {
            var ticket = new Ticket
            {
                Id = Guid.NewGuid().ToString(),
                EventId = "evt-1",
                EventTitle = "Test",
                UserId = "user-1",
                TicketTypeId = "tt-1",
                TicketTypeName = "General",
                Price = 10,
                Status = TicketStatus.Used,
                PaymentStatus = PaymentStatus.Completed,
                PurchasedAt = DateTime.UtcNow
            };
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            var result = await _ticketService.CheckInByQRCodeAsync(ticket.Id);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task CheckInByQRCodeAsync_NonExistentTicket_ReturnsFalse()
        {
            var result = await _ticketService.CheckInByQRCodeAsync("does-not-exist");
            result.Should().BeFalse();
        }

        // ── Ticket expiry ───────────────────────────────────────────────────

        [Fact]
        public async Task ExpireOldTicketsAsync_MarksTicketsForPastEvents()
        {
            var pastEvent = TestDataGenerator.CreateEvent(title: "Past");
            pastEvent.EndDate = DateTime.UtcNow.AddDays(-1);
            _context.Events.Add(pastEvent);

            var ticket = new Ticket
            {
                Id = Guid.NewGuid().ToString(),
                EventId = pastEvent.Id,
                EventTitle = pastEvent.Title,
                UserId = "user-1",
                TicketTypeId = "tt-1",
                TicketTypeName = "General",
                Price = 10,
                Status = TicketStatus.Active,
                PaymentStatus = PaymentStatus.Completed,
                PurchasedAt = DateTime.UtcNow.AddDays(-2)
            };
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            await _ticketService.ExpireOldTicketsAsync();

            var expired = await _context.Tickets.FindAsync(ticket.Id);
            expired!.Status.Should().Be(TicketStatus.Expired);
        }

        // ── Poll vote casting ───────────────────────────────────────────────

        [Fact]
        public async Task CastVoteAsync_ValidVote_IncrementsVoteCount()
        {
            var poll = TestDataGenerator.CreatePoll();
            poll.Status = PollStatus.Active;
            poll.StartDate = DateTime.UtcNow.AddMinutes(-1);
            poll.EndDate = DateTime.UtcNow.AddDays(1);
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            var optionId = poll.Options.First().Id;
            var (success, error) = await _pollService.CastVoteAsync(
                poll.Id, "user-99", [optionId]);

            success.Should().BeTrue();
            error.Should().BeNull();

            var updated = await _pollService.GetPollByIdAsync(poll.Id);
            updated!.TotalVotes.Should().Be(1);
            updated.Options.First(o => o.Id == optionId).VoteCount.Should().Be(1);
        }

        [Fact]
        public async Task CastVoteAsync_DuplicateVote_ReturnsFalse()
        {
            var poll = TestDataGenerator.CreatePoll();
            poll.Status = PollStatus.Active;
            poll.StartDate = DateTime.UtcNow.AddMinutes(-1);
            poll.EndDate = DateTime.UtcNow.AddDays(1);
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            var optionId = poll.Options.First().Id;
            await _pollService.CastVoteAsync(poll.Id, "user-dup", [optionId]);
            var (success, error) = await _pollService.CastVoteAsync(poll.Id, "user-dup", [optionId]);

            success.Should().BeFalse();
            error.Should().Contain("already voted");
        }
    }
}
