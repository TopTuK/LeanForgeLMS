using LF.AppDomain.Entities.News;
using LF.AppDomain.Models.News.Enums;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.News;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.News;

internal sealed class NewsService(
    ILogger<NewsService> logger,
    IAppDbContext dbContext,
    TimeProvider timeProvider) : INewsService
{
    private readonly ILogger<NewsService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<PagedNewsPostsDto> ListPublishedAsync(bool includeMembersOnly, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("NewsService::ListPublishedAsync: called with IncludeMembersOnly={IncludeMembersOnly} Page={Page} PageSize={PageSize}",
            includeMembersOnly, page, pageSize);

        var query = PublishedQuery(includeMembersOnly);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.PublishedAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(NewsPostProjection.ToDto)
            .ToListAsync(cancellationToken);

        return new PagedNewsPostsDto { Items = items, TotalCount = totalCount };
    }

    public async Task<NewsPostDto?> GetPublishedAsync(int id, bool includeMembersOnly, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("NewsService::GetPublishedAsync: called with Id={NewsPostId} IncludeMembersOnly={IncludeMembersOnly}", id, includeMembersOnly);

        return await PublishedQuery(includeMembersOnly)
            .Where(p => p.Id == id)
            .Select(NewsPostProjection.ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<NotificationFeedDto> GetFeedAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("NewsService::GetFeedAsync: called with UserId={UserId} Page={Page} PageSize={PageSize}", userId, page, pageSize);

        var paged = await ListPublishedAsync(includeMembersOnly: true, page, pageSize, cancellationToken);
        var lastSeenAt = await GetLastSeenAtAsync(userId, cancellationToken);

        return new NotificationFeedDto { Items = paged.Items, TotalCount = paged.TotalCount, LastSeenAt = lastSeenAt };
    }

    public async Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        var query = PublishedQuery(includeMembersOnly: true);

        var lastSeenAt = await GetLastSeenAtAsync(userId, cancellationToken);
        if (lastSeenAt is { } seenAt)
            query = query.Where(p => p.PublishedAt > seenAt);

        return await query.CountAsync(cancellationToken);
    }

    public async Task MarkAllSeenAsync(int userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("NewsService::MarkAllSeenAsync: called with UserId={UserId}", userId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var marker = await _dbContext.NewsReadMarkers.FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);

        if (marker is null)
            _dbContext.NewsReadMarkers.Add(NewsReadMarker.Create(userId, now));
        else
            marker.MarkSeen(now);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Two tabs opening Notifications at once race to insert the first marker; the loser's
            // row already exists with an equivalent timestamp, so there's nothing left to do.
            _logger.LogWarning(ex, "NewsService::MarkAllSeenAsync: concurrent marker insert for user {UserId}", userId);
        }
    }

    public async Task<string?> GetImageObjectKeyAsync(int newsPostId, int imageId, bool isAuthenticated, bool isAdmin, CancellationToken cancellationToken = default)
    {
        return await _dbContext.NewsPosts
            .AsNoTracking()
            .Where(p => p.Id == newsPostId)
            .Where(p => isAdmin || (p.IsPublished && (p.Visibility == NewsVisibility.Public || isAuthenticated)))
            .SelectMany(p => p.Images)
            .Where(i => i.Id == imageId)
            .Select(i => i.StorageObject.ObjectKey)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private IQueryable<NewsPost> PublishedQuery(bool includeMembersOnly)
    {
        var query = _dbContext.NewsPosts.AsNoTracking().Where(p => p.IsPublished);
        return includeMembersOnly ? query : query.Where(p => p.Visibility == NewsVisibility.Public);
    }

    private async Task<DateTime?> GetLastSeenAtAsync(int userId, CancellationToken cancellationToken) =>
        await _dbContext.NewsReadMarkers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => (DateTime?)m.LastSeenAt)
            .FirstOrDefaultAsync(cancellationToken);
}
