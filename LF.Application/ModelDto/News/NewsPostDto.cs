using LF.AppDomain.Models.News.Enums;

namespace LF.Application.ModelDto.News;

public sealed record NewsPostDto
{
    public int Id { get; init; }
    public string Title { get; init; } = null!;
    public string Html { get; init; } = null!;
    public NewsVisibility Visibility { get; init; }
    public bool IsPublished { get; init; }
    public DateTime? PublishedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public IReadOnlyList<NewsImageDto> Images { get; init; } = [];
}

public sealed record NewsImageDto
{
    public int Id { get; init; }
    public int StorageObjectId { get; init; }
}
