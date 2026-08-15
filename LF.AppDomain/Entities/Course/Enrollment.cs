namespace LF.AppDomain.Entities.Course;

public sealed class Enrollment
{
    private Enrollment()
    {
    }

    public int Id { get; private set; }
    public int CourseId { get; private set; }
    public int UserId { get; private set; }
    public DateTime EnrolledAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int[] CompletedLessonIds { get; private set; } = [];

    public static Enrollment Create(int courseId, int userId, DateTime enrolledAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(courseId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(userId, 0);

        return new Enrollment
        {
            CourseId = courseId,
            UserId = userId,
            EnrolledAt = enrolledAt,
        };
    }

    /// <returns>true if the lesson was newly marked complete; false if it already was.</returns>
    public bool CompleteLesson(int lessonId)
    {
        if (CompletedLessonIds.Contains(lessonId))
            return false;

        CompletedLessonIds = [.. CompletedLessonIds, lessonId];
        return true;
    }

    // Reopens a previously-completed enrollment if an author adds lessons after the fact,
    // so CompletedAt never claims 100% progress that no longer matches CompletedLessonIds.
    public void RecalculateCompletion(int totalLessonCount, DateTime nowUtc)
    {
        var isComplete = totalLessonCount > 0 && CompletedLessonIds.Length >= totalLessonCount;

        if (isComplete && CompletedAt is null)
            CompletedAt = nowUtc;
        else if (!isComplete && CompletedAt is not null)
            CompletedAt = null;
    }

    public int ProgressPercent(int totalLessonCount)
    {
        if (totalLessonCount <= 0)
            return 0;

        return (int)Math.Round(CompletedLessonIds.Length * 100.0 / totalLessonCount, MidpointRounding.AwayFromZero);
    }
}
