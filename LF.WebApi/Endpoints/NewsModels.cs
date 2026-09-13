using LF.Application.ModelDto.News;

namespace LF.WebApi.Endpoints;

public sealed record NewsImageResponse(int Id, string Url);

public sealed record NewsPostResponse(
    int Id,
    string Title,
    string Html,
    string Visibility,
    DateTime? PublishedAt,
    IReadOnlyList<NewsImageResponse> Images);

public sealed record PagedNewsResponse(IReadOnlyList<NewsPostResponse> Items, int TotalCount, int Page, int PageSize);

public sealed record NotificationFeedResponse(
    IReadOnlyList<NewsPostResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    DateTime? LastSeenAt);

public sealed record UnreadCountResponse(int Count);

// Shared by the public news group and the notifications feed, which return the same post shape.
internal static class NewsResponseMapper
{
    public static string ImageUrl(int newsPostId, int imageId) => $"/api/news/{newsPostId}/images/{imageId}";

    public static NewsPostResponse ToResponse(NewsPostDto dto) => new(
        dto.Id,
        dto.Title,
        dto.Html,
        dto.Visibility.ToString(),
        dto.PublishedAt,
        [.. dto.Images.Select(i => new NewsImageResponse(i.Id, ImageUrl(dto.Id, i.Id)))]);
}
