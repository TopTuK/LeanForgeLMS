namespace LF.Application.ModelDto.Enrollment;

public sealed class EnrollmentSummaryDto
{
    public int Id { get; init; }
    public int CourseId { get; init; }
    public string CourseTitle { get; init; } = null!;
    public string CourseShortIntroduction { get; init; } = null!;
    public string CategoryName { get; init; } = null!;
    public int TotalLessonCount { get; init; }
    public int CompletedLessonCount { get; init; }
    public int ProgressPercent { get; init; }
    public DateTime EnrolledAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
