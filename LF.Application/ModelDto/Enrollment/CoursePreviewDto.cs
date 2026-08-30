using LF.AppDomain.Models.Course.Enums;

namespace LF.Application.ModelDto.Enrollment;

public sealed class CoursePreviewDto
{
    public int Id { get; init; }
    public string Title { get; init; } = null!;
    public string ShortIntroduction { get; init; } = null!;
    public string Description { get; init; } = null!;
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = null!;
    public int LessonCount { get; init; }
    public CoursePricingType PricingType { get; init; }
    public decimal? Price { get; init; }
    public CourseCoverType CoverType { get; init; }
    public CourseCoverColor? CoverColor { get; init; }
    public string? CoverImageKey { get; init; }
    public string? CoverImageContentType { get; init; }
    public bool IsEnrolled { get; init; }
    public int? EnrollmentId { get; init; }
    public IReadOnlyList<CoursePreviewChapterDto> Chapters { get; init; } = [];
}
