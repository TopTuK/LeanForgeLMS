namespace LF.Application.ModelDto.Course;

public sealed class UpdateLessonDto
{
    public string Title { get; init; } = null!;
    public string? Content { get; init; }
    public bool IncludeInPreview { get; init; }
}
