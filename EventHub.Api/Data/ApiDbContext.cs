using EventHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Api.Data;

public class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options) { }

    public DbSet<ApiUser>   Users   { get; set; }
    public DbSet<ApiEvent>  Events  { get; set; }
    public DbSet<ApiPoll>   Polls   { get; set; }
    public DbSet<ApiOption> Options { get; set; }
    public DbSet<ApiVote>   Votes   { get; set; }
    public DbSet<ApiTicket> Tickets { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<ApiOption>()
          .HasOne<ApiPoll>()
          .WithMany(p => p.Options)
          .HasForeignKey(o => o.PollId)
          .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<ApiVote>()
          .HasIndex(v => new { v.PollId, v.UserId });   // fast lookup for free polls
    }
}
