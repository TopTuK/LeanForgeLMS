using LF.AppDomain.Models.Qna.Enums;

namespace LF.Application.ModelDto.Qna;

public class LessonQuestionSummaryDto
{
    public int Id { get; init; }
    public int CourseId { get; init; }
    public string CourseTitle { get; init; } = null!;
    public int LessonId { get; init; }
    public string LessonTitle { get; init; } = null!;
    public int StudentUserId { get; init; }
    public string StudentName { get; init; } = null!;
    public string Title { get; init; } = null!;
    public LessonQuestionStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastMessageAt { get; init; }
    public int MessageCount { get; init; }

    // Relative to the caller who ran the query, not an absolute property of the thread.
    public bool HasUnread { get; init; }

    // A short excerpt of the newest message, for inbox rows. Null when that message was deleted.
    public string? LastMessagePreview { get; init; }
    public QuestionAuthorRole LastMessageAuthorRole { get; init; }

    // The asking student's enrollment in the course, so the student's own inbox can link straight back
    // to the lesson. Staff cannot open another student's enrollment, so clients only use it for their own threads.
    public int? StudentEnrollmentId { get; init; }

    // True when the viewer is the student who asked; relative to the viewer, like HasUnread.
    public bool AskedByViewer { get; init; }
}
