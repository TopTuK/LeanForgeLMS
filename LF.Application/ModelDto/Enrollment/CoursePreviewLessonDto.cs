using LF.Application.ModelDto.Course;

namespace LF.Application.ModelDto.Enrollment;

public sealed class CoursePreviewLessonDto
{
    public int Id { get; init; }
    public string Title { get; init; } = null!;
    public int SortOrder { get; init; }
    public bool IncludeInPreview { get; init; }
    public string? Content { get; init; }
    public IReadOnlyList<LessonPartDto> Parts { get; init; } = [];
}
