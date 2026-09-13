namespace LF.Application.ModelDto.News;

public sealed class PagedNewsPostsDto
{
    public IReadOnlyList<NewsPostDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
}
