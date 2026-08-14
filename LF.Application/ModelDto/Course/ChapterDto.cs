namespace LF.Application.ModelDto.Course;

public sealed class ChapterDto
{
    public int Id { get; init; }
    public string Title { get; init; } = null!;
    public int SortOrder { get; init; }
    public IReadOnlyList<LessonDto> Lessons { get; init; } = [];
}
