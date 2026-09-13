namespace LF.AppDomain.Entities.News;

// One row per user: everything published up to LastSeenAt counts as read. A single timestamp is
// enough because the Notifications view shows the whole feed and marks it all seen at once.
public sealed class NewsReadMarker
{
    private NewsReadMarker()
    {
    }

    public int UserId { get; private set; }
    public DateTime LastSeenAt { get; private set; }

    public static NewsReadMarker Create(int userId, DateTime seenAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(userId, 0);

        return new NewsReadMarker
        {
            UserId = userId,
            LastSeenAt = seenAt,
        };
    }

    // Never moves backwards, so a stale or out-of-order request can't turn read news unread again.
    public void MarkSeen(DateTime seenAt)
    {
        if (seenAt > LastSeenAt)
            LastSeenAt = seenAt;
    }
}
