namespace OnlineVoting_and_Ticketing_app.Helpers
{
    public static class DateTimeHelper
    {
        public static string GetRelativeTime(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalSeconds < 60)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)}w ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)}mo ago";

            return $"{(int)(timeSpan.TotalDays / 365)}y ago";
        }

        public static string FormatEventDate(DateTime startDate, DateTime endDate)
        {
            if (startDate.Date == endDate.Date)
            {
                return $"{startDate:MMM dd, yyyy} • {startDate:h:mm tt} - {endDate:h:mm tt}";
            }
            else
            {
                return $"{startDate:MMM dd} - {endDate:MMM dd, yyyy}";
            }
        }

        public static bool IsEventUpcoming(DateTime startDate)
        {
            return startDate > DateTime.UtcNow;
        }

        public static bool IsEventOngoing(DateTime startDate, DateTime endDate)
        {
            var now = DateTime.UtcNow;
            return now >= startDate && now <= endDate;
        }

        public static bool IsEventPast(DateTime endDate)
        {
            return endDate < DateTime.UtcNow;
        }
    }
}
