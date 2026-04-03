using OnlineVoting_and_Ticketing_app.Data;
using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private bool _disposed;

        public IRepository<Event> Events { get; }
        public IRepository<Ticket> Tickets { get; }
        public IRepository<TicketType> TicketTypes { get; }
        public IRepository<Poll> Polls { get; }
        public IRepository<PollOption> PollOptions { get; }
        public IRepository<Vote> Votes { get; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Events = new Repository<Event>(context);
            Tickets = new Repository<Ticket>(context);
            TicketTypes = new Repository<TicketType>(context);
            Polls = new Repository<Poll>(context);
            PollOptions = new Repository<PollOption>(context);
            Votes = new Repository<Vote>(context);
        }

        public Task<int> SaveChangesAsync() =>
            _context.SaveChangesAsync();

        public void Dispose()
        {
            if (!_disposed)
            {
                _context.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
