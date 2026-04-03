using Microsoft.EntityFrameworkCore;
using OnlineVoting_and_Ticketing_app.Data;

namespace OnlineVoting_and_Ticketing_app.Tests.Helpers
{
    public static class TestDbContextFactory
    {
        /// <summary>
        /// Creates an in-memory EF Core context isolated per test by a unique DB name.
        /// </summary>
        public static AppDbContext CreateInMemoryContext(string? databaseName = null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        /// <summary>
        /// Creates a real SQLite in-memory context (shared-cache URI).
        /// Use this for tests that need SQL features not supported by EF InMemory (e.g. transactions).
        /// </summary>
        public static AppDbContext CreateSqliteInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            var context = new AppDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();
            return context;
        }
    }
}
