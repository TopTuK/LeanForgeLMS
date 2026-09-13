using System.Security.Claims;
using LF.Application.Services.News;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

public sealed class NotificationEndpoints : IEndpointGroup
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;

    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications").RequireAuthorization();

        group.MapGet("/", async Task<Results<Ok<NotificationFeedResponse>, UnauthorizedHttpResult>>
            (int? page, int? pageSize, ClaimsPrincipal user, INewsService newsService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var effectivePage = page is > 0 ? page.Value : 1;
            var effectivePageSize = pageSize is > 0 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize;

            var feed = await newsService.GetFeedAsync(userId.Value, effectivePage, effectivePageSize, ct);
            return TypedResults.Ok(new NotificationFeedResponse(
                [.. feed.Items.Select(NewsResponseMapper.ToResponse)],
                feed.TotalCount,
                effectivePage,
                effectivePageSize,
                feed.LastSeenAt));
        });

        group.MapGet("/unread-count", async Task<Results<Ok<UnreadCountResponse>, UnauthorizedHttpResult>>
            (ClaimsPrincipal user, INewsService newsService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var count = await newsService.GetUnreadCountAsync(userId.Value, ct);
            return TypedResults.Ok(new UnreadCountResponse(count));
        });

        group.MapPost("/mark-seen", async Task<Results<NoContent, UnauthorizedHttpResult>>
            (ClaimsPrincipal user, INewsService newsService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            await newsService.MarkAllSeenAsync(userId.Value, ct);
            return TypedResults.NoContent();
        });
    }
}
