using LF.AppDomain.Models.Course.Enums;

namespace LF.Application.ModelDto.Course;

public sealed class UpdateCourseDetailsDto
{
    public string Title { get; init; } = null!;
    public string ShortIntroduction { get; init; } = null!;
    public string Description { get; init; } = null!;
    public int CategoryId { get; init; }
    public CoursePricingType PricingType { get; init; } = CoursePricingType.Free;
    public decimal? Price { get; init; }
    public CourseEnrollmentMode EnrollmentMode { get; init; } = CourseEnrollmentMode.Open;
    public CourseCoverType CoverType { get; init; } = CourseCoverType.None;
    public CourseCoverColor? CoverColor { get; init; }

    // Null with CoverType.Image keeps the course's current image, so an unchanged cover need not be re-uploaded.
    public int? CoverImageStorageObjectId { get; init; }
}
