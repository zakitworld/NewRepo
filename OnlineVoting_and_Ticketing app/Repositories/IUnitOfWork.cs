using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Repositories
{
    /// <summary>
    /// Unit of Work coordinates multiple repositories in a single database transaction.
    /// Call SaveChangesAsync() once after all modifications to commit atomically.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Event> Events { get; }
        IRepository<Ticket> Tickets { get; }
        IRepository<TicketType> TicketTypes { get; }
        IRepository<Poll> Polls { get; }
        IRepository<PollOption> PollOptions { get; }
        IRepository<Vote> Votes { get; }

        Task<int> SaveChangesAsync();
    }
}
