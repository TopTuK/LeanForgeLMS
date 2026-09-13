using System.Linq.Expressions;
using LF.AppDomain.Entities.News;
using LF.Application.ModelDto.News;

namespace LF.Application.Services.News;

internal static class NewsPostProjection
{
    public static readonly Expression<Func<NewsPost, NewsPostDto>> ToDto = p => new NewsPostDto
    {
        Id = p.Id,
        Title = p.Title,
        Html = p.Html,
        Visibility = p.Visibility,
        IsPublished = p.IsPublished,
        PublishedAt = p.PublishedAt,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        Images = p.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => new NewsImageDto { Id = i.Id, StorageObjectId = i.StorageObjectId })
            .ToList(),
    };

    public static readonly Func<NewsPost, NewsPostDto> ToDtoCompiled = ToDto.Compile();
}
