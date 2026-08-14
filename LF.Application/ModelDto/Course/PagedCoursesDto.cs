namespace LF.Application.ModelDto.Course;

public sealed class PagedCoursesDto
{
    public IReadOnlyList<CourseSummaryDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
}
