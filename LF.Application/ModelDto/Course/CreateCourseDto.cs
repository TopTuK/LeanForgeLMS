namespace LF.Application.ModelDto.Course;

public sealed class CreateCourseDto
{
    public string Title { get; init; } = null!;
    public string ShortIntroduction { get; init; } = null!;
    public string Description { get; init; } = null!;
    public int CategoryId { get; init; }
}
