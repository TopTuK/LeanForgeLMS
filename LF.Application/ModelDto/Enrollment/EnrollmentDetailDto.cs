namespace LF.Application.ModelDto.Enrollment;

public sealed class EnrollmentDetailDto
{
    public int Id { get; init; }
    public int CourseId { get; init; }
    public string CourseTitle { get; init; } = null!;
    public string CourseDescription { get; init; } = null!;
    public DateTime EnrolledAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public IReadOnlyList<EnrollmentChapterDto> Chapters { get; init; } = [];
}
