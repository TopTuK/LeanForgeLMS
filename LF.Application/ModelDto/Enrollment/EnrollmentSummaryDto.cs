using LF.AppDomain.Models.Course.Enums;

namespace LF.Application.ModelDto.Enrollment;

public sealed class EnrollmentSummaryDto
{
    public int Id { get; init; }
    public int CourseId { get; init; }
    public string CourseTitle { get; init; } = null!;
    public string CourseShortIntroduction { get; init; } = null!;
    public string CategoryName { get; init; } = null!;
    public EnrollmentStatus Status { get; init; }
    public decimal PricePaid { get; init; }
    public int TotalLessonCount { get; init; }
    public int CompletedLessonCount { get; init; }
    public int ProgressPercent { get; init; }
    public DateTime EnrolledAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public CourseCoverType CoverType { get; init; }
    public CourseCoverColor? CoverColor { get; init; }
    public string? CoverImageKey { get; init; }
    public string? CoverImageContentType { get; init; }
}
