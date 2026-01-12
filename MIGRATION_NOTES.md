# Migration from Firebase to SQLite + ASP.NET Identity

## Summary of Changes

This document outlines the migration from Firebase to a local SQLite database with ASP.NET Identity for authentication.

## Why SQLite?

- **Offline-First**: Works without internet connection
- **Self-Contained**: No external dependencies or API keys needed
- **Fast**: Local database queries are instant
- **Privacy**: All data stays on the device
- **Cost**: Completely free, no cloud costs
- **Simple Setup**: No configuration required

## Architecture Changes

### Before (Firebase)
```
App → Firebase Services → Firebase Cloud
     ├── FirebaseAuthentication
     ├── FirebaseDatabase
     └── FirebaseStorage
```

### After (SQLite + ASP.NET Identity)
```
App → SQLite Services → Local SQLite Database
     ├── ASP.NET Identity (Authentication)
     ├── Entity Framework Core (ORM)
     └── SQLite (Storage)
```

## Package Changes

### Removed Packages
- ❌ FirebaseAdmin (3.0.1)
- ❌ FirebaseAuthentication.net (4.1.0)
- ❌ FirebaseDatabase.net (4.2.0)
- ❌ FirebaseStorage.net (1.0.3)

### Added Packages
- ✅ Microsoft.EntityFrameworkCore.Sqlite (9.0.0)
- ✅ Microsoft.EntityFrameworkCore.Tools (9.0.0)
- ✅ Microsoft.AspNetCore.Identity.EntityFrameworkCore (9.0.0)
- ✅ Microsoft.Extensions.Identity.Core (9.0.0)
- ✅ BCrypt.Net-Next (4.0.3)

## Code Changes

### 1. Database Context
**New File:** `Data/AppDbContext.cs`
- Extends `IdentityDbContext<ApplicationUser>`
- Configures all entities and relationships
- Seeds admin user on first run
- Handles Entity Framework migrations

### 2. Authentication Service
**Old:** `Services/FirebaseAuthenticationService.cs`
**New:** `Services/SqliteAuthenticationService.cs`

Key differences:
- Uses ASP.NET Identity `UserManager` and `SignInManager`
- Passwords hashed with BCrypt
- Social logins require additional OAuth setup (marked as not implemented)
- Session management via Preferences (unchanged)

### 3. Event Service
**Old:** `Services/FirebaseEventService.cs`
**New:** `Services/SqliteEventService.cs`

Key differences:
- Uses Entity Framework `DbContext` instead of Firebase client
- Supports eager loading with `.Include()` for related data
- LINQ queries for filtering and searching
- Automatic ID generation with `Guid.NewGuid()`

### 4. Ticket Service
**Old:** `Services/FirebaseTicketService.cs`
**New:** `Services/SqliteTicketService.cs`

Key differences:
- Database transactions for ticket purchases
- Referential integrity enforced by SQLite
- QR code generation unchanged

### 5. Poll Service
**Old:** `Services/FirebasePollService.cs`
**New:** `Services/SqlitePollService.cs`

Key differences:
- Poll options stored as related entities
- Vote validation using database queries
- Cascade deletes for poll options and votes

### 6. Application User Model
**New:** `Data/ApplicationUser.cs`
- Extends `IdentityUser` from ASP.NET Identity
- Custom properties: `FullName`, `ProfileImageUrl`, `CreatedAt`, `UpdatedAt`, `IsActive`
- Integrates with ASP.NET Identity roles and claims

## Database Schema

### Tables Created
1. **AspNetUsers** - User accounts
2. **AspNetRoles** - Roles (Admin, User, etc.)
3. **AspNetUserRoles** - User-role mappings
4. **AspNetUserClaims** - User claims
5. **AspNetUserLogins** - External logins
6. **AspNetUserTokens** - Auth tokens
7. **AspNetRoleClaims** - Role claims
8. **Events** - Event information
9. **TicketTypes** - Ticket categories
10. **Tickets** - Purchased tickets
11. **Polls** - Voting polls
12. **PollOptions** - Poll choices
13. **Votes** - Cast votes

### Seeded Data
- **Admin User**
  - Email: admin@eventhub.com
  - Password: Admin@123
  - Created automatically on first run

## Configuration Changes

### MauiProgram.cs
```csharp
// Database setup
var dbPath = Path.Combine(FileSystem.AppDataDirectory, "eventhub.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Identity setup
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Service registration
builder.Services.AddScoped<IAuthenticationService, SqliteAuthenticationService>();
builder.Services.AddScoped<IEventService, SqliteEventService>();
builder.Services.AddScoped<IPollService, SqlitePollService>();
builder.Services.AddScoped<ITicketService, SqliteTicketService>();
```

### App.xaml.cs
```csharp
private async void InitializeDatabaseAsync()
{
    var dbContext = Handler?.MauiContext?.Services.GetRequiredService<AppDbContext>();
    if (dbContext != null)
    {
        await dbContext.Database.MigrateAsync();
    }
}
```

## Password Requirements

With ASP.NET Identity, password requirements are now enforced:
- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- Special characters optional

## Migration Steps for Existing Users

If you had Firebase data:
1. Export data from Firebase console
2. Transform to SQLite format
3. Import using Entity Framework migrations

For new installations:
- Database created automatically on first run
- No manual steps required

## Testing the Migration

### Test Account
Use the seeded admin account:
- Email: `admin@eventhub.com`
- Password: `Admin@123`

### Test Registration
Create a new account:
- Must use valid email format
- Password must meet complexity requirements

### Test Database
SQLite database location:
- **Android**: `/data/data/com.eventhub.votingandticketing/files/eventhub.db`
- **iOS**: `Library/Application Support/eventhub.db`

Use SQLite browser tools to inspect the database.

## Benefits of This Migration

1. **No Internet Required**: App works fully offline
2. **Faster Performance**: Local queries are instant
3. **Better Privacy**: Data stays on device
4. **No API Keys**: No Firebase configuration needed
5. **Industry Standard**: ASP.NET Identity is battle-tested
6. **Better Control**: Full access to database schema
7. **Cost Savings**: No cloud hosting fees

## Limitations

1. **Social Login**: Requires additional OAuth setup
2. **Cloud Sync**: No automatic cloud backup
3. **Cross-Device**: Data not synced between devices
4. **Scalability**: Limited to single device

## Future Enhancements

- [ ] Cloud sync option (optional backend API)
- [ ] Data export/import features
- [ ] OAuth integration for social logins
- [ ] Multi-device sync via REST API
- [ ] Backup and restore functionality

## Support

For issues related to this migration:
1. Check that Entity Framework migrations ran successfully
2. Verify database file exists in AppDataDirectory
3. Check debug output for database errors
4. Ensure all NuGet packages are restored

---

**Migration Date**: January 2026
**Database Version**: 1.0
**Entity Framework Core**: 9.0
