using LF.AppDomain.Models.Course.Enums;

namespace LF.AppDomain.Entities.Course;

// A student-visible lesson change on a published course, waiting to be announced to enrolled students.
// At most one pending row exists per lesson: repeated saves touch it instead of adding rows, so an
// editing session collapses into a single entry in one digest email.
public sealed class CourseContentChange
{
    private CourseContentChange()
    {
    }

    public int Id { get; private set; }
    public int CourseId { get; private set; }
    public int LessonId { get; private set; }

    // Navigation so a just-added lesson (Id still 0) is linked in the same SaveChanges, and so
    // deleting the lesson cascades its pending changes away instead of announcing a missing lesson.
    public Lesson Lesson { get; private set; } = null!;

    public CourseContentChangeKind Kind { get; private set; }
    public DateTime FirstChangedAt { get; private set; }
    public DateTime LastChangedAt { get; private set; }
    public DateTime? NotifiedAt { get; private set; }

    public bool IsPending => NotifiedAt is null;

    public static CourseContentChange Create(int courseId, Lesson lesson, CourseContentChangeKind kind, DateTime changedAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(courseId, 0);
        ArgumentNullException.ThrowIfNull(lesson);

        if (!Enum.IsDefined(kind))
            throw new ArgumentException("Unknown change kind.", nameof(kind));

        return new CourseContentChange
        {
            CourseId = courseId,
            Lesson = lesson,
            LessonId = lesson.Id,
            Kind = kind,
            FirstChangedAt = changedAt,
            LastChangedAt = changedAt,
        };
    }

    // A lesson that is still pending as "added" stays "added" however often it is edited afterwards.
    public void Touch(CourseContentChangeKind kind, DateTime changedAt)
    {
        if (!IsPending)
            throw new InvalidOperationException("Cannot update a change that has already been announced.");

        if (Kind != CourseContentChangeKind.LessonAdded)
            Kind = kind;

        if (changedAt > LastChangedAt)
            LastChangedAt = changedAt;
    }

    public void MarkNotified(DateTime nowUtc)
    {
        if (!IsPending)
            throw new InvalidOperationException("Change has already been announced.");

        NotifiedAt = nowUtc;
    }
}
