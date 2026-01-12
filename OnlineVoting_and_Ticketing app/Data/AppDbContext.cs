using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnlineVoting_and_Ticketing_app.Models;

namespace OnlineVoting_and_Ticketing_app.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Event> Events { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Poll> Polls { get; set; }
        public DbSet<Vote> Votes { get; set; }
        public DbSet<PollOption> PollOptions { get; set; }
        public DbSet<TicketType> TicketTypes { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Event configuration
            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Location).IsRequired().HasMaxLength(300);
                entity.Property(e => e.OrganizerId).IsRequired();

                // One-to-many relationship with TicketTypes
                entity.HasMany(e => e.TicketTypes)
                    .WithOne()
                    .HasForeignKey("EventId")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Ticket configuration
            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.EventId).IsRequired();
                entity.Property(t => t.UserId).IsRequired();
            });

            // Poll configuration
            modelBuilder.Entity<Poll>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Title).IsRequired().HasMaxLength(300);
                entity.Property(p => p.CreatorId).IsRequired();

                // One-to-many relationship with PollOptions
                entity.HasMany(p => p.Options)
                    .WithOne()
                    .HasForeignKey("PollId")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Vote configuration
            modelBuilder.Entity<Vote>(entity =>
            {
                entity.HasKey(v => v.Id);
                entity.Property(v => v.PollId).IsRequired();
                entity.Property(v => v.UserId).IsRequired();

                // Store selected option IDs as JSON
                entity.Property(v => v.SelectedOptionIds)
                    .HasConversion(
                        v => string.Join(",", v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                    );
            });

            // PollOption configuration
            modelBuilder.Entity<PollOption>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Text).IsRequired().HasMaxLength(500);
            });

            // TicketType configuration
            modelBuilder.Entity<TicketType>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            });

            // Seed admin user
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var hasher = new PasswordHasher<ApplicationUser>();

            var adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "admin@eventhub.com",
                NormalizedUserName = "ADMIN@EVENTHUB.COM",
                Email = "admin@eventhub.com",
                NormalizedEmail = "ADMIN@EVENTHUB.COM",
                EmailConfirmed = true,
                FullName = "Admin User",
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin@123");

            modelBuilder.Entity<ApplicationUser>().HasData(adminUser);
        }
    }

    // Extended Identity User with custom properties
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
