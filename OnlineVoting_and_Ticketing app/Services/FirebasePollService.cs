using Firebase.Database;
using Firebase.Database.Query;
using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public class FirebasePollService : IPollService
    {
        private readonly FirebaseClient _firebaseClient;

        public FirebasePollService()
        {
            _firebaseClient = new FirebaseClient(FirebaseConfig.DatabaseUrl);
        }

        public async Task<List<Poll>> GetAllPollsAsync()
        {
            try
            {
                var polls = await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionPolls)
                    .OnceAsync<Poll>();

                return polls
                    .Select(p => new Poll
                    {
                        Id = p.Key,
                        Title = p.Object.Title,
                        Description = p.Object.Description,
                        ImageUrl = p.Object.ImageUrl,
                        CreatorId = p.Object.CreatorId,
                        CreatorName = p.Object.CreatorName,
                        EventId = p.Object.EventId,
                        StartDate = p.Object.StartDate,
                        EndDate = p.Object.EndDate,
                        Status = p.Object.Status,
                        Type = p.Object.Type,
                        AllowMultipleChoices = p.Object.AllowMultipleChoices,
                        IsAnonymous = p.Object.IsAnonymous,
                        RequireAuthentication = p.Object.RequireAuthentication,
                        Options = p.Object.Options,
                        TotalVotes = p.Object.TotalVotes,
                        CreatedAt = p.Object.CreatedAt,
                        UpdatedAt = p.Object.UpdatedAt
                    })
                    .OrderByDescending(p => p.CreatedAt)
                    .ToList();
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
                var allPolls = await GetAllPollsAsync();
                var now = DateTime.UtcNow;

                return allPolls
                    .Where(p => p.Status == PollStatus.Active &&
                               p.StartDate <= now &&
                               p.EndDate >= now)
                    .ToList();
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
                var poll = await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionPolls)
                    .Child(pollId)
                    .OnceSingleAsync<Poll>();

                if (poll == null)
                    return null;

                poll.Id = pollId;
                return poll;
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
                var allPolls = await GetAllPollsAsync();
                return allPolls
                    .Where(p => p.CreatorId == creatorId)
                    .ToList();
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
                var allPolls = await GetAllPollsAsync();
                return allPolls
                    .Where(p => p.EventId == eventId)
                    .ToList();
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
                pollData.CreatedAt = DateTime.UtcNow;
                pollData.UpdatedAt = DateTime.UtcNow;
                pollData.TotalVotes = 0;

                for (int i = 0; i < pollData.Options.Count; i++)
                {
                    pollData.Options[i].Id = Guid.NewGuid().ToString();
                    pollData.Options[i].Order = i;
                    pollData.Options[i].VoteCount = 0;
                }

                var result = await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionPolls)
                    .PostAsync(pollData);

                return (true, null, result.Key);
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

                await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionPolls)
                    .Child(pollData.Id)
                    .PutAsync(pollData);

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
                await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionPolls)
                    .Child(pollId)
                    .DeleteAsync();

                var votes = await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionVotes)
                    .OnceAsync<Vote>();

                var pollVotes = votes.Where(v => v.Object.PollId == pollId);
                foreach (var vote in pollVotes)
                {
                    await _firebaseClient
                        .Child(AppConstants.Firebase.CollectionVotes)
                        .Child(vote.Key)
                        .DeleteAsync();
                }

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
                    PollId = pollId,
                    UserId = userId,
                    SelectedOptionIds = selectedOptionIds,
                    VotedAt = DateTime.UtcNow
                };

                await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionVotes)
                    .PostAsync(vote);

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
                var votes = await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionVotes)
                    .OnceAsync<Vote>();

                return votes.Any(v => v.Object.PollId == pollId && v.Object.UserId == userId);
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
