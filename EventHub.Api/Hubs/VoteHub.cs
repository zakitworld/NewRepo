using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EventHub.Api.Hubs;

/// <summary>
/// Real-time hub that pushes live vote count updates to all connected clients.
/// Mobile app connects with: var conn = new HubConnectionBuilder()
///     .WithUrl("https://your-api/hubs/votes?access_token=JWT")
///     .Build();
/// conn.On&lt;string, string, int&gt;("VoteUpdate", (pollId, optionId, newCount) => { ... });
/// </summary>
public class VoteHub : Hub
{
    /// <summary>Clients subscribe to a specific poll's group to receive targeted updates.</summary>
    public async Task JoinPoll(string pollId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"poll_{pollId}");

    public async Task LeavePoll(string pollId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"poll_{pollId}");
}

/// <summary>
/// Injected into controllers that need to broadcast vote updates.
/// </summary>
public interface IVoteNotifier
{
    Task BroadcastVoteUpdateAsync(string pollId, string optionId, int newCount, int totalVotes);
}

public class VoteNotifier : IVoteNotifier
{
    private readonly IHubContext<VoteHub> _hub;

    public VoteNotifier(IHubContext<VoteHub> hub) => _hub = hub;

    public async Task BroadcastVoteUpdateAsync(string pollId, string optionId, int newCount, int totalVotes)
        => await _hub.Clients.Group($"poll_{pollId}")
                     .SendAsync("VoteUpdate", pollId, optionId, newCount, totalVotes);
}
