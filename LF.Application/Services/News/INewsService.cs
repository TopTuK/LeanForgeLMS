using LF.Application.ModelDto.News;

namespace LF.Application.Services.News;

public interface INewsService
{
    Task<PagedNewsPostsDto> ListPublishedAsync(bool includeMembersOnly, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<NewsPostDto?> GetPublishedAsync(int id, bool includeMembersOnly, CancellationToken cancellationToken = default);

    // Every published post (public and members-only) plus the user's read marker.
    Task<NotificationFeedDto> GetFeedAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);

    Task MarkAllSeenAsync(int userId, CancellationToken cancellationToken = default);

    // Null when the image doesn't exist or the caller may not see its post — callers answer 404
    // either way so a members-only post's existence isn't revealed to anonymous visitors.
    Task<string?> GetImageObjectKeyAsync(int newsPostId, int imageId, bool isAuthenticated, bool isAdmin, CancellationToken cancellationToken = default);
}
