using LF.AppDomain.Models.Course.Enums;

namespace LF.Application.ModelDto.Teaching;

public sealed class TeachingCourseDto
{
    public int Id { get; init; }
    public string Title { get; init; } = null!;
    public string ShortIntroduction { get; init; } = null!;
    public string CategoryName { get; init; } = null!;
    public bool IsPublished { get; init; }
    public CourseCoverType CoverType { get; init; }
    public CourseCoverColor? CoverColor { get; init; }

    // Only the creator (or an admin) may open the content editor; assigned instructors manage groups
    // and lectures but not the course itself.
    public bool CanEditContent { get; init; }
}
