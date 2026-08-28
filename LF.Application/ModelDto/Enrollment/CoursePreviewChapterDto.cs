namespace LF.Application.ModelDto.Enrollment;

public sealed class CoursePreviewChapterDto
{
    public int Id { get; init; }
    public string Title { get; init; } = null!;
    public int SortOrder { get; init; }
    public IReadOnlyList<CoursePreviewLessonDto> Lessons { get; init; } = [];
}
