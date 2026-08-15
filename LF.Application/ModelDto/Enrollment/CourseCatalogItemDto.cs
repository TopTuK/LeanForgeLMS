namespace LF.Application.ModelDto.Enrollment;

public sealed class CourseCatalogItemDto
{
    public int Id { get; init; }
    public string Title { get; init; } = null!;
    public string ShortIntroduction { get; init; } = null!;
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = null!;
    public int LessonCount { get; init; }
}
