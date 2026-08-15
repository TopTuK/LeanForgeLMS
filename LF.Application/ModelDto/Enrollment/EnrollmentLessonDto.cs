namespace LF.Application.ModelDto.Enrollment;

public sealed class EnrollmentLessonDto
{
    public int Id { get; init; }
    public string Title { get; init; } = null!;
    public string Content { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsCompleted { get; init; }
}
