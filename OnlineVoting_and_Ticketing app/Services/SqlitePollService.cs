using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineVoting_and_Ticketing_app.Data;
using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public class SqlitePollService : IPollService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SqlitePollService> _logger;

        public SqlitePollService(AppDbContext context, ILogger<SqlitePollService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Poll>> GetAllPollsAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Polls
                    .Include(p => p.Options)
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllPollsAsync failed");
                return [];
            }
        }

        public async Task<List<Poll>> GetActivePollsAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                var now = DateTime.UtcNow;
                return await _context.Polls
                    .Include(p => p.Options)
                    .Where(p => p.Status == PollStatus.Active &&
                               p.StartDate <= now &&
                               p.EndDate >= now)
                    .OrderBy(p => p.EndDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetActivePollsAsync failed");
                return [];
            }
        }

        public async Task<Poll?> GetPollByIdAsync(string pollId)
        {
            try
            {
                return await _context.Polls
                    .Include(p => p.Options)
                    .FirstOrDefaultAsync(p => p.Id == pollId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPollByIdAsync failed for {PollId}", pollId);
                return null;
            }
        }

        public async Task<List<Poll>> GetPollsByCreatorAsync(string creatorId, int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Polls
                    .Include(p => p.Options)
                    .Where(p => p.CreatorId == creatorId)
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPollsByCreatorAsync failed for {CreatorId}", creatorId);
                return [];
            }
        }

        public async Task<List<Poll>> GetPollsByEventAsync(string eventId)
        {
            try
            {
                return await _context.Polls
                    .Include(p => p.Options)
                    .Where(p => p.EventId == eventId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPollsByEventAsync failed for {EventId}", eventId);
                return [];
            }
        }

        public async Task<(bool Success, string? Error, string? PollId)> CreatePollAsync(Poll pollData)
        {
            try
            {
                pollData.Id = Guid.NewGuid().ToString();
                pollData.CreatedAt = DateTime.UtcNow;
                pollData.UpdatedAt = DateTime.UtcNow;
                pollData.TotalVotes = 0;

                for (int i = 0; i < pollData.Options.Count; i++)
                {
                    pollData.Options[i].Id = Guid.NewGuid().ToString();
                    pollData.Options[i].PollId = pollData.Id;
                    pollData.Options[i].Order = i;
                    pollData.Options[i].VoteCount = 0;
                }

                _context.Polls.Add(pollData);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Poll created: {PollId} '{Title}'", pollData.Id, pollData.Title);
                return (true, null, pollData.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreatePollAsync failed for '{Title}'", pollData.Title);
                return (false, ex.Message, null);
            }
        }

        public async Task<(bool Success, string? Error)> UpdatePollAsync(Poll pollData)
        {
            try
            {
                pollData.UpdatedAt = DateTime.UtcNow;
                _context.Polls.Update(pollData);
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdatePollAsync failed for {PollId}", pollData.Id);
                return (false, ex.Message);
            }
        }

        public async Task<bool> DeletePollAsync(string pollId)
        {
            try
            {
                var poll = await _context.Polls.FindAsync(pollId);
                if (poll == null) return false;

                _context.Polls.Remove(poll);
                var votes = await _context.Votes.Where(v => v.PollId == pollId).ToListAsync();
                _context.Votes.RemoveRange(votes);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Poll deleted: {PollId}", pollId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeletePollAsync failed for {PollId}", pollId);
                return false;
            }
        }

        public async Task<(bool Success, string? Error)> CastVoteAsync(
            string pollId, string userId, List<string> selectedOptionIds)
        {
            try
            {
                if (await HasUserVotedAsync(pollId, userId))
                    return (false, "You have already voted in this poll");

                var poll = await GetPollByIdAsync(pollId);
                if (poll == null) return (false, "Poll not found");
                if (poll.Status != PollStatus.Active) return (false, "Poll is not active");

                var now = DateTime.UtcNow;
                if (now < poll.StartDate) return (false, "Poll has not started yet");
                if (now > poll.EndDate) return (false, "Poll has ended");
                if (!poll.AllowMultipleChoices && selectedOptionIds.Count > 1)
                    return (false, "Only one option can be selected");

                var vote = new Vote
                {
                    Id = Guid.NewGuid().ToString(),
                    PollId = pollId,
                    UserId = userId,
                    SelectedOptionIds = selectedOptionIds,
                    VotedAt = DateTime.UtcNow
                };

                _context.Votes.Add(vote);

                foreach (var optionId in selectedOptionIds)
                {
                    var option = poll.Options.FirstOrDefault(o => o.Id == optionId);
                    if (option != null) option.VoteCount++;
                }

                poll.TotalVotes++;
                await UpdatePollAsync(poll);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Vote cast by {UserId} on poll {PollId}", userId, pollId);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CastVoteAsync failed for poll {PollId}", pollId);
                return (false, ex.Message);
            }
        }

        public async Task<bool> HasUserVotedAsync(string pollId, string userId)
        {
            try
            {
                return await _context.Votes.AnyAsync(v => v.PollId == pollId && v.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HasUserVotedAsync failed");
                return false;
            }
        }

        public async Task<Dictionary<string, int>> GetPollResultsAsync(string pollId)
        {
            try
            {
                var poll = await GetPollByIdAsync(pollId);
                return poll?.Options.ToDictionary(o => o.Text, o => o.VoteCount)
                       ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPollResultsAsync failed for {PollId}", pollId);
                return [];
            }
        }

        public async Task<int> GetTotalPollsCountAsync(bool activeOnly = false)
        {
            try
            {
                if (!activeOnly)
                    return await _context.Polls.CountAsync();

                var now = DateTime.UtcNow;
                return await _context.Polls.CountAsync(
                    p => p.Status == PollStatus.Active && p.StartDate <= now && p.EndDate >= now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTotalPollsCountAsync failed");
                return 0;
            }
        }
    }
}
