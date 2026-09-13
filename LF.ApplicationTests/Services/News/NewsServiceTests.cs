using LF.AppDomain.Entities.News;
using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.News.Enums;
using LF.AppDomain.Models.Storage.Enums;
using LF.ApplicationTests.TestSupport;
using LF.Application.Common.Interfaces;
using LF.Application.Services.News;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace LF.ApplicationTests.Services.News;

public class NewsServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private static NewsService CreateService(
        IReadOnlyCollection<NewsPost> posts,
        IReadOnlyCollection<NewsReadMarker> markers,
        out Mock<DbSet<NewsReadMarker>> markersSetMock)
    {
        markersSetMock = markers.ToList().BuildMockDbSet();

        var dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.NewsPosts).Returns(posts.ToList().BuildMockDbSet().Object);
        dbContextMock.SetupGet(c => c.NewsReadMarkers).Returns(markersSetMock.Object);
        dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new NewsService(NullLogger<NewsService>.Instance, dbContextMock.Object, new FixedTimeProvider(Now));
    }

    private static NewsService CreateService(params NewsPost[] posts) => CreateService(posts, [], out _);

    private static NewsPost Post(int id, NewsVisibility visibility, DateTime? publishedAt, params StorageObject[] images)
    {
        var post = NewsPost.Create($"Post {id}", "<p>Body</p>", visibility, createdByUserId: 1, Now.AddDays(-30));
        EntityIdSetter.SetId(post, id);
        post.ReplaceImages(images);
        if (publishedAt is { } at)
            post.Publish(at);
        return post;
    }

    private static StorageObject Image(int id)
    {
        var storageObject = StorageObject.Create(StorageObjectType.Image, $"news/{id}.png", "image/png", 10, createdByUserId: 1, Now);
        EntityIdSetter.SetId(storageObject, id);
        return storageObject;
    }

    [Fact]
    public async Task ListPublishedAsync_PublicOnly_ExcludesDraftsAndMembersOnly_NewestFirst()
    {
        var service = CreateService(
            Post(1, NewsVisibility.Public, Now.AddDays(-3)),
            Post(2, NewsVisibility.Public, Now.AddDays(-1)),
            Post(3, NewsVisibility.MembersOnly, Now.AddDays(-2)),
            Post(4, NewsVisibility.Public, publishedAt: null));

        var result = await service.ListPublishedAsync(includeMembersOnly: false, page: 1, pageSize: 10, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal([2, 1], result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task ListPublishedAsync_IncludeMembersOnly_ReturnsBothVisibilities()
    {
        var service = CreateService(
            Post(1, NewsVisibility.Public, Now.AddDays(-3)),
            Post(2, NewsVisibility.MembersOnly, Now.AddDays(-1)),
            Post(3, NewsVisibility.MembersOnly, publishedAt: null));

        var result = await service.ListPublishedAsync(includeMembersOnly: true, page: 1, pageSize: 10, TestContext.Current.CancellationToken);

        Assert.Equal([2, 1], result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task ListPublishedAsync_PagesResults()
    {
        var service = CreateService(
            Post(1, NewsVisibility.Public, Now.AddDays(-3)),
            Post(2, NewsVisibility.Public, Now.AddDays(-2)),
            Post(3, NewsVisibility.Public, Now.AddDays(-1)));

        var result = await service.ListPublishedAsync(includeMembersOnly: false, page: 2, pageSize: 2, TestContext.Current.CancellationToken);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal([1], result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task ListPublishedAsync_ProjectsImagesInSortOrder()
    {
        var service = CreateService(Post(1, NewsVisibility.Public, Now, Image(20), Image(10)));

        var result = await service.ListPublishedAsync(includeMembersOnly: false, page: 1, pageSize: 10, TestContext.Current.CancellationToken);

        Assert.Equal([20, 10], result.Items.Single().Images.Select(i => i.StorageObjectId));
    }

    [Fact]
    public async Task GetPublishedAsync_MembersOnlyPost_IsHiddenFromPublicQuery()
    {
        var service = CreateService(Post(1, NewsVisibility.MembersOnly, Now));

        Assert.Null(await service.GetPublishedAsync(1, includeMembersOnly: false, TestContext.Current.CancellationToken));
        Assert.NotNull(await service.GetPublishedAsync(1, includeMembersOnly: true, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetUnreadCountAsync_NoMarker_CountsEveryPublishedPost()
    {
        var service = CreateService(
            Post(1, NewsVisibility.Public, Now.AddDays(-3)),
            Post(2, NewsVisibility.MembersOnly, Now.AddDays(-1)),
            Post(3, NewsVisibility.Public, publishedAt: null));

        var count = await service.GetUnreadCountAsync(userId: 7, TestContext.Current.CancellationToken);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetUnreadCountAsync_WithMarker_CountsOnlyPostsPublishedAfterIt()
    {
        var service = CreateService(
            [
                Post(1, NewsVisibility.Public, Now.AddDays(-3)),
                Post(2, NewsVisibility.MembersOnly, Now.AddDays(-1)),
            ],
            [NewsReadMarker.Create(7, Now.AddDays(-2)), NewsReadMarker.Create(8, Now)],
            out _);

        var count = await service.GetUnreadCountAsync(userId: 7, TestContext.Current.CancellationToken);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetFeedAsync_ReturnsAllPublishedPostsAndTheUsersMarker()
    {
        var seenAt = Now.AddDays(-2);
        var service = CreateService(
            [
                Post(1, NewsVisibility.Public, Now.AddDays(-3)),
                Post(2, NewsVisibility.MembersOnly, Now.AddDays(-1)),
            ],
            [NewsReadMarker.Create(7, seenAt)],
            out _);

        var feed = await service.GetFeedAsync(userId: 7, page: 1, pageSize: 10, TestContext.Current.CancellationToken);

        Assert.Equal(2, feed.TotalCount);
        Assert.Equal(seenAt, feed.LastSeenAt);
    }

    [Fact]
    public async Task GetFeedAsync_NoMarker_ReturnsNullLastSeenAt()
    {
        var service = CreateService(Post(1, NewsVisibility.Public, Now));

        var feed = await service.GetFeedAsync(userId: 7, page: 1, pageSize: 10, TestContext.Current.CancellationToken);

        Assert.Null(feed.LastSeenAt);
    }

    [Fact]
    public async Task MarkAllSeenAsync_NoMarker_AddsOneAtCurrentTime()
    {
        var service = CreateService([], [], out var markersSetMock);

        await service.MarkAllSeenAsync(userId: 7, TestContext.Current.CancellationToken);

        markersSetMock.Verify(s => s.Add(It.Is<NewsReadMarker>(m => m.UserId == 7 && m.LastSeenAt == Now)), Times.Once);
    }

    [Fact]
    public async Task MarkAllSeenAsync_ExistingMarker_AdvancesIt()
    {
        var marker = NewsReadMarker.Create(7, Now.AddDays(-5));
        var service = CreateService([], [marker], out var markersSetMock);

        await service.MarkAllSeenAsync(userId: 7, TestContext.Current.CancellationToken);

        Assert.Equal(Now, marker.LastSeenAt);
        markersSetMock.Verify(s => s.Add(It.IsAny<NewsReadMarker>()), Times.Never);
    }

    [Theory]
    [InlineData(NewsVisibility.Public, true, false, false, true)]
    [InlineData(NewsVisibility.MembersOnly, true, false, false, false)]
    [InlineData(NewsVisibility.MembersOnly, true, true, false, true)]
    [InlineData(NewsVisibility.Public, false, true, false, false)]
    [InlineData(NewsVisibility.MembersOnly, false, false, true, true)]
    public async Task GetImageObjectKeyAsync_GatesOnVisibilityPublishStateAndCaller(
        NewsVisibility visibility, bool published, bool isAuthenticated, bool isAdmin, bool expectVisible)
    {
        var post = Post(1, visibility, published ? Now : null, Image(10));
        EntityIdSetter.SetId(post.Images[0], 100);
        var service = CreateService(post);

        var key = await service.GetImageObjectKeyAsync(1, 100, isAuthenticated, isAdmin, TestContext.Current.CancellationToken);

        Assert.Equal(expectVisible ? "news/10.png" : null, key);
    }

    [Fact]
    public async Task GetImageObjectKeyAsync_ImageOfAnotherPost_ReturnsNull()
    {
        var first = Post(1, NewsVisibility.Public, Now, Image(10));
        var second = Post(2, NewsVisibility.Public, Now, Image(20));
        EntityIdSetter.SetId(first.Images[0], 100);
        EntityIdSetter.SetId(second.Images[0], 200);
        var service = CreateService(first, second);

        var key = await service.GetImageObjectKeyAsync(1, 200, isAuthenticated: true, isAdmin: false, TestContext.Current.CancellationToken);

        Assert.Null(key);
    }
}
