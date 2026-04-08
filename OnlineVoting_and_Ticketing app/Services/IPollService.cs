using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public interface IPollService
    {
        Task<List<Poll>> GetAllPollsAsync(int page = 1, int pageSize = 20);
        Task<List<Poll>> GetActivePollsAsync(int page = 1, int pageSize = 20);
        Task<Poll?> GetPollByIdAsync(string pollId);
        Task<List<Poll>> GetPollsByCreatorAsync(string creatorId, int page = 1, int pageSize = 20);
        Task<List<Poll>> GetPollsByEventAsync(string eventId);
        Task<(bool Success, string? Error, string? PollId)> CreatePollAsync(Poll pollData);
        Task<(bool Success, string? Error)> UpdatePollAsync(Poll pollData);
        Task<bool> DeletePollAsync(string pollId);
        Task<(bool Success, string? Error)> CastVoteAsync(string pollId, string userId, List<string> selectedOptionIds);
        Task<(bool Success, string? Error)> CastPaidVoteAsync(string pollId, string userId, string optionId, int voteCount);
        Task<bool> HasUserVotedAsync(string pollId, string userId);
        Task<int> GetUserVoteCountAsync(string pollId, string userId);
        Task<Dictionary<string, int>> GetPollResultsAsync(string pollId);
        Task<int> GetTotalPollsCountAsync(bool activeOnly = false);
    }
}
