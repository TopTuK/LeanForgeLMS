namespace LF.Application.ModelDto.Enrollment;

public sealed class PagedCourseEnrollmentsDto
{
    public IReadOnlyList<CourseEnrollmentDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
}
