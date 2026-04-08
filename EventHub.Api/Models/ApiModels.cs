namespace EventHub.Api.Models;

public class ApiUser
{
    public string Id          { get; set; } = Guid.NewGuid().ToString();
    public string Email       { get; set; } = string.Empty;
    public string PasswordHash{ get; set; } = string.Empty;
    public string FullName    { get; set; } = string.Empty;
    public string Role        { get; set; } = "User";   // User | Admin | EventOrganizer
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ApiEvent
{
    public string   Id          { get; set; } = Guid.NewGuid().ToString();
    public string   Title       { get; set; } = string.Empty;
    public string   Description { get; set; } = string.Empty;
    public string   Location    { get; set; } = string.Empty;
    public string   Category    { get; set; } = string.Empty;
    public string   ImageUrl    { get; set; } = string.Empty;
    public string   OrganizerId { get; set; } = string.Empty;
    public decimal  Price       { get; set; }
    public DateTime StartDate   { get; set; }
    public DateTime EndDate     { get; set; }
    public bool     IsActive    { get; set; } = true;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
}

public class ApiPoll
{
    public string   Id             { get; set; } = Guid.NewGuid().ToString();
    public string   Title          { get; set; } = string.Empty;
    public string   Description    { get; set; } = string.Empty;
    public string   CreatorId      { get; set; } = string.Empty;
    public string   CreatorName    { get; set; } = string.Empty;
    public bool     IsPaidVoting   { get; set; }
    public decimal  VotePriceGhs   { get; set; } = 1m;
    public int      MaxVotesPerUser{ get; set; }
    public bool     IsActive       { get; set; } = true;
    public int      TotalVotes     { get; set; }
    public DateTime StartDate      { get; set; }
    public DateTime EndDate        { get; set; }
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;
    public List<ApiOption> Options { get; set; } = new();
}

public class ApiOption
{
    public string Id                  { get; set; } = Guid.NewGuid().ToString();
    public string PollId              { get; set; } = string.Empty;
    public string Text                { get; set; } = string.Empty;
    public string? ContestantPhotoUrl { get; set; }
    public string? ContestantBio      { get; set; }
    public int    VoteCount           { get; set; }
    public int    Order               { get; set; }
}

public class ApiVote
{
    public string   Id       { get; set; } = Guid.NewGuid().ToString();
    public string   PollId   { get; set; } = string.Empty;
    public string   OptionId { get; set; } = string.Empty;
    public string   UserId   { get; set; } = string.Empty;
    public string?  PaymentRef { get; set; }   // Paystack ref for paid votes
    public DateTime VotedAt  { get; set; } = DateTime.UtcNow;
}

public class ApiTicket
{
    public string        Id            { get; set; } = Guid.NewGuid().ToString();
    public string        EventId       { get; set; } = string.Empty;
    public string        EventTitle    { get; set; } = string.Empty;
    public string        UserId        { get; set; } = string.Empty;
    public string        UserName      { get; set; } = string.Empty;
    public string        UserEmail     { get; set; } = string.Empty;
    public decimal       Price         { get; set; }
    public string        Status        { get; set; } = "Active";
    public string        TransactionId { get; set; } = string.Empty;
    public DateTime      PurchasedAt   { get; set; } = DateTime.UtcNow;
    public DateTime?     CheckedInAt   { get; set; }
}
