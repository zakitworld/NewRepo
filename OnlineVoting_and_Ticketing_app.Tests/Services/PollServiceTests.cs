using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OnlineVoting_and_Ticketing_app.Data;
using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Services;
using OnlineVoting_and_Ticketing_app.Tests.Helpers;
using Xunit;

namespace OnlineVoting_and_Ticketing_app.Tests.Services
{
    public class PollServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly SqlitePollService _pollService;

        public PollServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _pollService = new SqlitePollService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region GetAllPollsAsync Tests

        [Fact]
        public async Task GetAllPollsAsync_WhenNoPolls_ReturnsEmptyList()
        {
            // Act
            var result = await _pollService.GetAllPollsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllPollsAsync_WhenPollsExist_ReturnsAllPolls()
        {
            // Arrange
            var poll1 = TestDataGenerator.CreatePoll(title: "Poll 1");
            var poll2 = TestDataGenerator.CreatePoll(title: "Poll 2");
            _context.Polls.AddRange(poll1, poll2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _pollService.GetAllPollsAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllPollsAsync_IncludesOptions()
        {
            // Arrange
            var poll = TestDataGenerator.CreatePoll();
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            // Act
            var result = await _pollService.GetAllPollsAsync();

            // Assert
            result.First().Options.Should().NotBeEmpty();
        }

        #endregion

        #region GetActivePollsAsync Tests

        [Fact]
        public async Task GetActivePollsAsync_ReturnsOnlyActiveAndCurrentPolls()
        {
            // Arrange
            var activePoll = TestDataGenerator.CreatePoll(
                title: "Active Poll",
                status: PollStatus.Active,
                startDate: DateTime.UtcNow.AddDays(-1),
                endDate: DateTime.UtcNow.AddDays(7));

            var closedPoll = TestDataGenerator.CreatePoll(
                title: "Closed Poll",
                status: PollStatus.Closed);

            var futurePoll = TestDataGenerator.CreatePoll(
                title: "Future Poll",
                status: PollStatus.Active,
                startDate: DateTime.UtcNow.AddDays(5),
                endDate: DateTime.UtcNow.AddDays(10));

            var pastPoll = TestDataGenerator.CreatePoll(
                title: "Past Poll",
                status: PollStatus.Active,
                startDate: DateTime.UtcNow.AddDays(-10),
                endDate: DateTime.UtcNow.AddDays(-5));

            _context.Polls.AddRange(activePoll, closedPoll, futurePoll, pastPoll);
            await _context.SaveChangesAsync();

            // Act
            var result = await _pollService.GetActivePollsAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Active Poll");
        }

        #endregion

        #region GetPollByIdAsync Tests

        [Fact]
        public async Task GetPollByIdAsync_WhenPollExists_ReturnsPoll()
        {
            // Arrange
            var pollId = Guid.NewGuid().ToString();
            var poll = TestDataGenerator.CreatePoll(id: pollId, title: "Test Poll");
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            // Act
            var result = await _pollService.GetPollByIdAsync(pollId);

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("Test Poll");
        }

        [Fact]
        public async Task GetPollByIdAsync_WhenPollDoesNotExist_ReturnsNull()
        {
            // Act
            var result = await _pollService.GetPollByIdAsync("non-existent-id");

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetPollsByCreatorAsync Tests

        [Fact]
        public async Task GetPollsByCreatorAsync_ReturnsOnlyCreatorPolls()
        {
            // Arrange
            var creator1Poll = TestDataGenerator.CreatePoll(title: "Creator 1 Poll", creatorId: "creator-1");
            var creator2Poll = TestDataGenerator.CreatePoll(title: "Creator 2 Poll", creatorId: "creator-2");

            _context.Polls.AddRange(creator1Poll, creator2Poll);
            await _context.SaveChangesAsync();

            // Act
            var result = await _pollService.GetPollsByCreatorAsync("creator-1");

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Creator 1 Poll");
        }

        #endregion

        #region CreatePollAsync Tests

        [Fact]
        public async Task CreatePollAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var newPoll = TestDataGenerator.CreatePoll(title: "New Poll");

            // Act
            var (success, error, pollId) = await _pollService.CreatePollAsync(newPoll);

            // Assert
            success.Should().BeTrue();
            error.Should().BeNull();
            pollId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CreatePollAsync_GeneratesNewIdsForPollAndOptions()
        {
            // Arrange
            var newPoll = TestDataGenerator.CreatePoll(id: "original-id");
            var originalOptionIds = newPoll.Options.Select(o => o.Id).ToList();

            // Act
            var (success, _, pollId) = await _pollService.CreatePollAsync(newPoll);

            // Assert
            pollId.Should().NotBe("original-id");

            var createdPoll = await _pollService.GetPollByIdAsync(pollId!);
            createdPoll!.Options.Select(o => o.Id).Should().NotContain(originalOptionIds);
        }

        [Fact]
        public async Task CreatePollAsync_SetsInitialVoteCountToZero()
        {
            // Arrange
            var newPoll = TestDataGenerator.CreatePoll();
            newPoll.TotalVotes = 100; // Set non-zero value

            // Act
            var (_, _, pollId) = await _pollService.CreatePollAsync(newPoll);
            var createdPoll = await _pollService.GetPollByIdAsync(pollId!);

            // Assert
            createdPoll!.TotalVotes.Should().Be(0);
            createdPoll.Options.All(o => o.VoteCount == 0).Should().BeTrue();
        }

        [Fact]
        public async Task CreatePollAsync_SetsOptionOrderAndPollId()
        {
            // Arrange
            var newPoll = TestDataGenerator.CreatePoll();

            // Act
            var (_, _, pollId) = await _pollService.CreatePollAsync(newPoll);
            var createdPoll = await _pollService.GetPollByIdAsync(pollId!);

            // Assert
            createdPoll!.Options.Should().BeInAscendingOrder(o => o.Order);
            createdPoll.Options.All(o => o.PollId == pollId).Should().BeTrue();
        }

        #endregion

        #region DeletePollAsync Tests

        [Fact]
        public async Task DeletePollAsync_WhenPollExists_ReturnsTrue()
        {
            // Arrange
            var poll = TestDataGenerator.CreatePoll();
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            // Act
            var result = await _pollService.DeletePollAsync(poll.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeletePollAsync_WhenPollDoesNotExist_ReturnsFalse()
        {
            // Act
            var result = await _pollService.DeletePollAsync("non-existent-id");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeletePollAsync_RemovesPollFromDatabase()
        {
            // Arrange
            var poll = TestDataGenerator.CreatePoll();
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            // Act
            await _pollService.DeletePollAsync(poll.Id);

            // Assert
            var deletedPoll = await _context.Polls.FindAsync(poll.Id);
            deletedPoll.Should().BeNull();
        }

        [Fact]
        public async Task DeletePollAsync_RemovesAssociatedVotes()
        {
            // Arrange
            var poll = TestDataGenerator.CreatePoll();
            _context.Polls.Add(poll);

            var vote = TestDataGenerator.CreateVote(pollId: poll.Id, userId: "user-1");
            _context.Votes.Add(vote);
            await _context.SaveChangesAsync();

            // Act
            await _pollService.DeletePollAsync(poll.Id);

            // Assert
            var votesForPoll = await _context.Votes.Where(v => v.PollId == poll.Id).ToListAsync();
            votesForPoll.Should().BeEmpty();
        }

        #endregion

        #region CastVoteAsync Tests

        [Fact]
        public async Task CastVoteAsync_WithValidVote_ReturnsSuccess()
        {
            // Arrange
            var poll = TestDataGenerator.CreatePoll(
                status: PollStatus.Active,
                startDate: DateTime.UtcNow.AddDays(-1),
                endDate: DateTime.UtcNow.AddDays(7));
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            var optionId = poll.Options.First().Id;

            // Act
            var (success, error) = await _pollService.CastVoteAsync(poll.Id, "user-1", new List<string> { optionId });

            // Assert
            success.Should().BeTrue();
            error.Should().BeNull();
        }

        [Fact]
        public async Task CastVoteAsync_WhenUserAlreadyVoted_ReturnsError()
        {
            // Arrange
            var poll = TestDataGenerator.CreatePoll(
                status: PollStatus.Active,
                startDate: DateTime.UtcNow.AddDays(-1),
                endDate: DateTime.UtcNow.AddDays(7));
            _context.Polls.Add(poll);

            var existingVote = TestDataGenerator.CreateVote(pollId: poll.Id, userId: "user-1");
            _context.Votes.Add(existingVote);
            await _context.SaveChangesAsync();

            var optionId = poll.Options.First().Id;

            // Act
            var (success, error) = await _pollService.CastVoteAsync(poll.Id, "user-1", new List<string> { optionId });

            // Assert
            success.Should().BeFalse();
            error.Should().Contain("already voted");
        }

        [Fact]
        public async Task CastVoteAsync_WhenPollNotActive_ReturnsError()
        {
            // Arrange
            var poll = TestDataGenerator.CreatePoll(status: PollStatus.Closed);
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            // Act
            var (success, error) = await _pollService.CastVoteAsync(poll.Id, "user-1", new List<string> { "option-1" });

            // Assert
            success.Should().BeFalse();
            error.Should().Contain("not active");
        }

        [Fact]
        public async Task CastVoteAsync_WhenPollNotStarted_ReturnsError()
        {
            // Arrange
            var poll = TestDataGenerator.CreatePoll(
                status: PollStatus.Active,
                startDate: DateTime.UtcNow.AddDays(5),
                endDate: DateTime.UtcNow.AddDays(10));
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            var optionId = poll.Options.First().Id;

            // Act
            var (success, error) = await _pollService.CastVoteAsync(poll.Id, "user-1", new List<string> { optionId });

            // Assert
            success.Should().BeFalse();
            error.Should().Contain("not started");
        }

        [Fact]
        public async Task CastVoteAsync_WhenPollEnded_ReturnsError()
        {
            // Arrange
            var poll = TestDataGenerator.CreatePoll(
                status: PollStatus.Active,
                startDate: DateTime.UtcNow.AddDays(-10),
                endDate: DateTime.UtcNow.AddDays(-5));
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            var optionId = poll.Options.First().Id;

            // Act
            var (success, error) = await _pollService.CastVoteAsync(poll.Id, "user-1", new List<string> { optionId });

            // Assert
            success.Should().BeFalse();
            error.Should().Contain("ended");
        }

        [Fact]
        public async Task CastVoteAsync_WhenSingleChoiceAndMultipleSelected_ReturnsError()
        {
            // Arrange
            var poll = TestDataGenerator.CreatePoll(
                status: PollStatus.Active,
                startDate: DateTime.UtcNow.AddDays(-1),
                endDate: DateTime.UtcNow.AddDays(7),
                allowMultipleChoices: false);
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            var optionIds = poll.Options.Select(o => o.Id).ToList();

            // Act
            var (success, error) = await _pollService.CastVoteAsync(poll.Id, "user-1", optionIds);

            // Assert
            success.Should().BeFalse();
            error.Should().Contain("one option");
        }

        [Fact]
        public async Task CastVoteAsync_WhenMultipleChoiceAllowed_SucceedsWithMultipleOptions()
        {
            // Arrange
            var poll = TestDataGenerator.CreatePoll(
                status: PollStatus.Active,
                startDate: DateTime.UtcNow.AddDays(-1),
                endDate: DateTime.UtcNow.AddDays(7),
                allowMultipleChoices: true);
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            var optionIds = poll.Options.Select(o => o.Id).ToList();

            // Act
            var (success, error) = await _pollService.CastVoteAsync(poll.Id, "user-1", optionIds);

            // Assert
            success.Should().BeTrue();
            error.Should().BeNull();
        }

        [Fact]
        public async Task CastVoteAsync_IncrementsVoteCounts()
        {
            // Arrange
            var poll = TestDataGenerator.CreatePoll(
                status: PollStatus.Active,
                startDate: DateTime.UtcNow.AddDays(-1),
                endDate: DateTime.UtcNow.AddDays(7));
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            var optionId = poll.Options.First().Id;

            // Act
            await _pollService.CastVoteAsync(poll.Id, "user-1", new List<string> { optionId });

            // Assert
            var updatedPoll = await _pollService.GetPollByIdAsync(poll.Id);
            updatedPoll!.TotalVotes.Should().Be(1);
            updatedPoll.Options.First(o => o.Id == optionId).VoteCount.Should().Be(1);
        }

        [Fact]
        public async Task CastVoteAsync_CreatesVoteRecord()
        {
            // Arrange
            var poll = TestDataGenerator.CreatePoll(
                status: PollStatus.Active,
                startDate: DateTime.UtcNow.AddDays(-1),
                endDate: DateTime.UtcNow.AddDays(7));
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            var optionId = poll.Options.First().Id;

            // Act
            await _pollService.CastVoteAsync(poll.Id, "user-1", new List<string> { optionId });

            // Assert
            var vote = await _context.Votes.FirstOrDefaultAsync(v => v.PollId == poll.Id && v.UserId == "user-1");
            vote.Should().NotBeNull();
            vote!.SelectedOptionIds.Should().Contain(optionId);
        }

        #endregion

        #region HasUserVotedAsync Tests

        [Fact]
        public async Task HasUserVotedAsync_WhenUserHasVoted_ReturnsTrue()
        {
            // Arrange
            var vote = TestDataGenerator.CreateVote(pollId: "poll-1", userId: "user-1");
            _context.Votes.Add(vote);
            await _context.SaveChangesAsync();

            // Act
            var result = await _pollService.HasUserVotedAsync("poll-1", "user-1");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task HasUserVotedAsync_WhenUserHasNotVoted_ReturnsFalse()
        {
            // Act
            var result = await _pollService.HasUserVotedAsync("poll-1", "user-1");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetPollResultsAsync Tests

        [Fact]
        public async Task GetPollResultsAsync_ReturnsOptionTextAndVoteCounts()
        {
            // Arrange
            var poll = TestDataGenerator.CreatePoll();
            poll.Options[0].Text = "Option A";
            poll.Options[0].VoteCount = 10;
            poll.Options[1].Text = "Option B";
            poll.Options[1].VoteCount = 5;

            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            // Act
            var result = await _pollService.GetPollResultsAsync(poll.Id);

            // Assert
            result.Should().ContainKey("Option A").WhoseValue.Should().Be(10);
            result.Should().ContainKey("Option B").WhoseValue.Should().Be(5);
        }

        [Fact]
        public async Task GetPollResultsAsync_WhenPollDoesNotExist_ReturnsEmptyDictionary()
        {
            // Act
            var result = await _pollService.GetPollResultsAsync("non-existent-id");

            // Assert
            result.Should().BeEmpty();
        }

        #endregion
    }
}
