using Microsoft.EntityFrameworkCore;
using OnlineVoting_and_Ticketing_app.Data;
using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public class SqlitePollService : IPollService
    {
        private readonly AppDbContext _context;

        public SqlitePollService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Poll>> GetAllPollsAsync()
        {
            try
            {
                return await _context.Polls
                    .Include(p => p.Options)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }
            catch
            {
                return new List<Poll>();
            }
        }

        public async Task<List<Poll>> GetActivePollsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;

                return await _context.Polls
                    .Include(p => p.Options)
                    .Where(p => p.Status == PollStatus.Active &&
                               p.StartDate <= now &&
                               p.EndDate >= now)
                    .ToListAsync();
            }
            catch
            {
                return new List<Poll>();
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
            catch
            {
                return null;
            }
        }

        public async Task<List<Poll>> GetPollsByCreatorAsync(string creatorId)
        {
            try
            {
                return await _context.Polls
                    .Include(p => p.Options)
                    .Where(p => p.CreatorId == creatorId)
                    .ToListAsync();
            }
            catch
            {
                return new List<Poll>();
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
            catch
            {
                return new List<Poll>();
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

                return (true, null, pollData.Id);
            }
            catch (Exception ex)
            {
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
                return (false, ex.Message);
            }
        }

        public async Task<bool> DeletePollAsync(string pollId)
        {
            try
            {
                var poll = await _context.Polls.FindAsync(pollId);
                if (poll == null)
                    return false;

                _context.Polls.Remove(poll);

                // Remove associated votes
                var votes = await _context.Votes.Where(v => v.PollId == pollId).ToListAsync();
                _context.Votes.RemoveRange(votes);

                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(bool Success, string? Error)> CastVoteAsync(string pollId, string userId, List<string> selectedOptionIds)
        {
            try
            {
                var hasVoted = await HasUserVotedAsync(pollId, userId);
                if (hasVoted)
                    return (false, "You have already voted in this poll");

                var poll = await GetPollByIdAsync(pollId);
                if (poll == null)
                    return (false, "Poll not found");

                if (poll.Status != PollStatus.Active)
                    return (false, "Poll is not active");

                var now = DateTime.UtcNow;
                if (now < poll.StartDate)
                    return (false, "Poll has not started yet");

                if (now > poll.EndDate)
                    return (false, "Poll has ended");

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

                // Update vote counts
                foreach (var optionId in selectedOptionIds)
                {
                    var option = poll.Options.FirstOrDefault(o => o.Id == optionId);
                    if (option != null)
                    {
                        option.VoteCount++;
                    }
                }

                poll.TotalVotes++;
                await UpdatePollAsync(poll);

                await _context.SaveChangesAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<bool> HasUserVotedAsync(string pollId, string userId)
        {
            try
            {
                return await _context.Votes
                    .AnyAsync(v => v.PollId == pollId && v.UserId == userId);
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<string, int>> GetPollResultsAsync(string pollId)
        {
            try
            {
                var poll = await GetPollByIdAsync(pollId);
                if (poll == null)
                    return new Dictionary<string, int>();

                return poll.Options.ToDictionary(o => o.Text, o => o.VoteCount);
            }
            catch
            {
                return new Dictionary<string, int>();
            }
        }
    }
}
