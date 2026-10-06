namespace LF.AppDomain.Entities.Groups;

// One row per (group, viewer). Tracks the highest message id seen rather than a timestamp: ids are
// monotonic, so there's no clock-skew window where a just-posted message reads as already seen.
public sealed class GroupChatReadMarker
{
    private GroupChatReadMarker()
    {
    }

    public int Id { get; private set; }
    public int GroupId { get; private set; }
    public int UserId { get; private set; }
    public int LastSeenMessageId { get; private set; }

    public static GroupChatReadMarker Create(int groupId, int userId, int lastSeenMessageId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(groupId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(userId, 0);
        ArgumentOutOfRangeException.ThrowIfNegative(lastSeenMessageId);

        return new GroupChatReadMarker
        {
            GroupId = groupId,
            UserId = userId,
            LastSeenMessageId = lastSeenMessageId,
        };
    }

    // Never moves backwards, so an out-of-order request can't turn read messages unread again.
    public bool MarkSeen(int messageId)
    {
        if (messageId <= LastSeenMessageId)
            return false;

        LastSeenMessageId = messageId;
        return true;
    }
}
