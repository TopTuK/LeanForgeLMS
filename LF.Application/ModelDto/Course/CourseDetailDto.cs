using LF.AppDomain.Models.Course.Enums;

namespace LF.Application.ModelDto.Course;

public sealed class CourseDetailDto
{
    public int Id { get; init; }
    public string Title { get; init; } = null!;
    public string ShortIntroduction { get; init; } = null!;
    public string Description { get; init; } = null!;
    public CourseCoverType CoverType { get; init; }
    public CourseCoverColor? CoverColor { get; init; }
    public string? CoverImageKey { get; init; }
    public string? CoverImageContentType { get; init; }
    public bool IsPublished { get; init; }
    public CoursePricingType PricingType { get; init; }
    public decimal? Price { get; init; }
    public CourseEnrollmentMode EnrollmentMode { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = null!;
    public int CreatedByUserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<ChapterDto> Chapters { get; init; } = [];
}
