namespace LF.Application.ModelDto.Course;

public sealed class AddLessonDto
{
    public string Title { get; init; } = null!;
    public string? Content { get; init; }
    public bool IncludeInPreview { get; init; }
}
