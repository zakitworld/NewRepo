namespace OnlineVoting_and_Ticketing_app.Models
{
    public class Poll
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string CreatorId { get; set; } = string.Empty;
        public string CreatorName { get; set; } = string.Empty;
        public string? EventId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public PollStatus Status { get; set; } = PollStatus.Draft;
        public PollType Type { get; set; }
        public bool AllowMultipleChoices { get; set; }
        public bool IsAnonymous { get; set; }
        public bool RequireAuthentication { get; set; } = true;
        public List<PollOption> Options { get; set; } = new();
        public int TotalVotes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class PollOption
    {
        public string Id { get; set; } = string.Empty;
        public string PollId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int VoteCount { get; set; }
        public int Order { get; set; }
    }

    public class Vote
    {
        public string Id { get; set; } = string.Empty;
        public string PollId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public List<string> SelectedOptionIds { get; set; } = new();
        public DateTime VotedAt { get; set; } = DateTime.UtcNow;
        public string? IpAddress { get; set; }
    }

    public enum PollStatus
    {
        Draft,
        Active,
        Closed,
        Archived
    }

    public enum PollType
    {
        SingleChoice,
        MultipleChoice,
        Rating,
        OpenEnded
    }
}
