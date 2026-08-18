namespace LF.Application.ModelDto.Course;

public sealed class LessonDto
{
    public int Id { get; init; }
    public string Title { get; init; } = null!;
    public string Content { get; init; } = string.Empty;
    public bool IncludeInPreview { get; init; }
    public int SortOrder { get; init; }
    public IReadOnlyList<LessonPartDto> Parts { get; init; } = [];
}
