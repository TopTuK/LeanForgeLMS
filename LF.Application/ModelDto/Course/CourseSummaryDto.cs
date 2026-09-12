using LF.AppDomain.Models.Course.Enums;

namespace LF.Application.ModelDto.Course;

public sealed class CourseSummaryDto
{
    public int Id { get; init; }
    public string Title { get; init; } = null!;
    public string ShortIntroduction { get; init; } = null!;
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
    public int ChapterCount { get; init; }

    // Hydrated from LF.IdentityService only on the admin listing path, where courses from every
    // author are mixed together. Null everywhere else — the authoring list is single-author already.
    public string? AuthorEmail { get; set; }
    public string? AuthorFirstName { get; set; }
    public string? AuthorLastName { get; set; }
}
