namespace LF.Application.ModelDto.Enrollment;

public sealed class PagedCourseCatalogDto
{
    public IReadOnlyList<CourseCatalogItemDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
}
