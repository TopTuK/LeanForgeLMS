namespace LF.Application.ModelDto.Enrollment;

public sealed class EnrollmentChapterDto
{
    public int Id { get; init; }
    public string Title { get; init; } = null!;
    public int SortOrder { get; init; }
    public IReadOnlyList<EnrollmentLessonDto> Lessons { get; init; } = [];
}
