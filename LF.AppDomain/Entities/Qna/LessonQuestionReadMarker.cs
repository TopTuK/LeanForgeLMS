namespace LF.AppDomain.Entities.Qna;

// One row per (thread, viewer): everything posted up to LastSeenAt counts as read. Unlike the news
// feed's single per-user marker, Q&A needs per-thread granularity — a student reading one of their
// threads must not clear the badge on the others.
public sealed class LessonQuestionReadMarker
{
    private LessonQuestionReadMarker()
    {
    }

    public int Id { get; private set; }
    public int LessonQuestionId { get; private set; }
    public int UserId { get; private set; }
    public DateTime LastSeenAt { get; private set; }

    public static LessonQuestionReadMarker Create(int lessonQuestionId, int userId, DateTime seenAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lessonQuestionId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(userId, 0);

        return new LessonQuestionReadMarker
        {
            LessonQuestionId = lessonQuestionId,
            UserId = userId,
            LastSeenAt = seenAt,
        };
    }

    // Never moves backwards, so a stale or out-of-order request can't turn a read thread unread again.
    public void MarkSeen(DateTime seenAt)
    {
        if (seenAt > LastSeenAt)
            LastSeenAt = seenAt;
    }
}
