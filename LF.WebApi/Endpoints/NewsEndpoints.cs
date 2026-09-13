using System.Security.Claims;
using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Interfaces;
using LF.Application.Services.News;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

public sealed class NewsEndpoints : IEndpointGroup
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;

    public void Map(IEndpointRouteBuilder app)
    {
        // No RequireAuthorization: public news is readable without signing in. These routes only
        // ever list Public posts — members-only ones are served through /api/notifications — but
        // the image route also backs the Notifications feed and the admin editor, so it gates each
        // request on whatever identity the session cookie carries.
        var group = app.MapGroup("/api/news").WithTags("News");

        group.MapGet("/", async Task<Ok<PagedNewsResponse>>
            (int? page, int? pageSize, INewsService newsService, CancellationToken ct) =>
        {
            var effectivePage = page is > 0 ? page.Value : 1;
            var effectivePageSize = pageSize is > 0 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize;

            var result = await newsService.ListPublishedAsync(includeMembersOnly: false, effectivePage, effectivePageSize, ct);
            return TypedResults.Ok(new PagedNewsResponse(
                [.. result.Items.Select(NewsResponseMapper.ToResponse)], result.TotalCount, effectivePage, effectivePageSize));
        });

        group.MapGet("/{id:int}", async Task<Results<Ok<NewsPostResponse>, NotFound>>
            (int id, INewsService newsService, CancellationToken ct) =>
        {
            var post = await newsService.GetPublishedAsync(id, includeMembersOnly: false, ct);
            return post is null ? TypedResults.NotFound() : TypedResults.Ok(NewsResponseMapper.ToResponse(post));
        });

        group.MapGet("/{id:int}/images/{imageId:int}", async Task<Results<FileStreamHttpResult, NotFound>>
            (int id, int imageId, ClaimsPrincipal user, INewsService newsService, [FromKeyedServices("storage")] IFileStorageService fileStorageService, CancellationToken ct) =>
        {
            var objectKey = await newsService.GetImageObjectKeyAsync(
                id,
                imageId,
                isAuthenticated: user.Identity?.IsAuthenticated == true,
                isAdmin: user.IsInRole(nameof(UserRole.Admin)),
                ct);

            if (objectKey is null) return TypedResults.NotFound();

            var download = await fileStorageService.DownloadAsync(objectKey, ct);
            return download is null ? TypedResults.NotFound() : TypedResults.Stream(download.Content, download.ContentType);
        });
    }
}
