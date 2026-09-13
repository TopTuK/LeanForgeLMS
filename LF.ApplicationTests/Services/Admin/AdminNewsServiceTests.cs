using LF.AppDomain.Entities.News;
using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.News.Enums;
using LF.AppDomain.Models.Storage.Enums;
using LF.ApplicationTests.TestSupport;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.News;
using LF.Application.Services.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace LF.ApplicationTests.Services.Admin;

public class AdminNewsServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private sealed class Harness
    {
        public required AdminNewsService Service { get; init; }
        public required Mock<IAppDbContext> DbContext { get; init; }
        public required Mock<DbSet<NewsPost>> Posts { get; init; }
        public required Mock<DbSet<StorageObject>> StorageObjects { get; init; }
        public required Mock<IFileStorageService> FileStorage { get; init; }
    }

    private static Harness CreateHarness(IReadOnlyCollection<NewsPost>? posts = null, IReadOnlyCollection<StorageObject>? storageObjects = null)
    {
        var postsSetMock = (posts ?? []).ToList().BuildMockDbSet();
        var storageSetMock = (storageObjects ?? []).ToList().BuildMockDbSet();

        var dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.NewsPosts).Returns(postsSetMock.Object);
        dbContextMock.SetupGet(c => c.StorageObjects).Returns(storageSetMock.Object);
        dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sanitizerMock = new Mock<IHtmlSanitizer>();
        sanitizerMock
            .Setup(s => s.Sanitize(It.IsAny<string?>()))
            .Returns((string? html) => (html ?? string.Empty).Replace("<script>alert(1)</script>", string.Empty));

        var fileStorageMock = new Mock<IFileStorageService>();

        var service = new AdminNewsService(
            NullLogger<AdminNewsService>.Instance,
            dbContextMock.Object,
            sanitizerMock.Object,
            fileStorageMock.Object,
            new FixedTimeProvider(Now));

        return new Harness
        {
            Service = service,
            DbContext = dbContextMock,
            Posts = postsSetMock,
            StorageObjects = storageSetMock,
            FileStorage = fileStorageMock,
        };
    }

    private static StorageObject NewsImage(int id, string? key = null)
    {
        var storageObject = StorageObject.Create(StorageObjectType.Image, key ?? $"news/{id}.png", "image/png", 10, createdByUserId: 1, Now);
        EntityIdSetter.SetId(storageObject, id);
        return storageObject;
    }

    private static NewsPost ExistingPost(int id, params StorageObject[] images)
    {
        var post = NewsPost.Create($"Post {id}", "<p>Body</p>", NewsVisibility.Public, createdByUserId: 1, Now.AddDays(-1));
        EntityIdSetter.SetId(post, id);
        post.ReplaceImages(images);
        post.Publish(Now.AddDays(-1));
        return post;
    }

    private static SaveNewsPostDto Dto(params int[] imageIds) => new()
    {
        Title = "Launch",
        Html = "<p>Hello</p><script>alert(1)</script>",
        Visibility = NewsVisibility.MembersOnly,
        IsPublished = true,
        ImageStorageObjectIds = imageIds,
    };

    [Fact]
    public async Task CreateAsync_SanitizesHtmlAttachesImagesInOrderAndPublishes()
    {
        var harness = CreateHarness(storageObjects: [NewsImage(1), NewsImage(2)]);

        var result = await harness.Service.CreateAsync(Dto(2, 1), actingAdminId: 5, TestContext.Current.CancellationToken);

        harness.Posts.Verify(s => s.Add(It.Is<NewsPost>(p =>
            p.Html == "<p>Hello</p>" &&
            p.CreatedByUserId == 5 &&
            p.Visibility == NewsVisibility.MembersOnly &&
            p.IsPublished &&
            p.PublishedAt == Now)), Times.Once);
        harness.DbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal([2, 1], result.Images.Select(i => i.StorageObjectId));
    }

    [Fact]
    public async Task CreateAsync_Draft_LeavesPublishedAtEmpty()
    {
        var harness = CreateHarness();
        var dto = new SaveNewsPostDto { Title = "Draft", Html = "<p>Soon</p>", Visibility = NewsVisibility.Public, IsPublished = false };

        var result = await harness.Service.CreateAsync(dto, actingAdminId: 5, TestContext.Current.CancellationToken);

        Assert.False(result.IsPublished);
        Assert.Null(result.PublishedAt);
    }

    [Fact]
    public async Task CreateAsync_UnknownImageId_ThrowsWithoutSaving()
    {
        var harness = CreateHarness(storageObjects: [NewsImage(1)]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            harness.Service.CreateAsync(Dto(1, 99), actingAdminId: 5, TestContext.Current.CancellationToken));

        harness.DbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Course covers and lesson media live in the same storage table; attaching one would publish it
    // and let a news delete remove a blob the course still uses.
    [Fact]
    public async Task CreateAsync_ImageNotUploadedForNews_Throws()
    {
        var harness = CreateHarness(storageObjects: [NewsImage(1, key: "images/course-cover.png")]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            harness.Service.CreateAsync(Dto(1), actingAdminId: 5, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAsync_ImageAttachedToAnotherPost_Throws()
    {
        var shared = NewsImage(1);
        var harness = CreateHarness(posts: [ExistingPost(10, shared)], storageObjects: [shared]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            harness.Service.CreateAsync(Dto(1), actingAdminId: 5, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateAsync_MissingPost_ReturnsNull()
    {
        var harness = CreateHarness();

        var result = await harness.Service.UpdateAsync(42, Dto(), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_KeepsOwnImagesAndDeletesDroppedOnes()
    {
        var kept = NewsImage(1);
        var dropped = NewsImage(2);
        var added = NewsImage(3);
        var post = ExistingPost(10, kept, dropped);
        var harness = CreateHarness(posts: [post], storageObjects: [kept, dropped, added]);

        var result = await harness.Service.UpdateAsync(10, Dto(3, 1), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal([3, 1], result.Images.Select(i => i.StorageObjectId));
        Assert.Equal("Launch", post.Title);
        harness.StorageObjects.Verify(s => s.RemoveRange(It.Is<IEnumerable<StorageObject>>(objects =>
            objects.Select(o => o.Id).SequenceEqual(new[] { 2 }))), Times.Once);
        harness.FileStorage.Verify(f => f.DeleteAsync("news/2.png", It.IsAny<CancellationToken>()), Times.Once);
        harness.FileStorage.Verify(f => f.DeleteAsync("news/1.png", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Unpublish_KeepsPostButHidesIt()
    {
        var post = ExistingPost(10);
        var harness = CreateHarness(posts: [post]);
        var dto = new SaveNewsPostDto { Title = "Hidden", Html = "<p>Body</p>", Visibility = NewsVisibility.Public, IsPublished = false };

        var result = await harness.Service.UpdateAsync(10, dto, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.False(result.IsPublished);
        Assert.Equal(Now.AddDays(-1), result.PublishedAt);
    }

    [Fact]
    public async Task DeleteAsync_MissingPost_ReturnsFalse()
    {
        var harness = CreateHarness();

        Assert.False(await harness.Service.DeleteAsync(42, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_RemovesPostItsStorageRowsAndBlobs()
    {
        var first = NewsImage(1);
        var second = NewsImage(2);
        var post = ExistingPost(10, first, second);
        var harness = CreateHarness(posts: [post], storageObjects: [first, second]);

        var deleted = await harness.Service.DeleteAsync(10, TestContext.Current.CancellationToken);

        Assert.True(deleted);
        harness.Posts.Verify(s => s.Remove(post), Times.Once);
        harness.StorageObjects.Verify(s => s.RemoveRange(It.Is<IEnumerable<StorageObject>>(objects =>
            objects.Select(o => o.Id).SequenceEqual(new[] { 1, 2 }))), Times.Once);
        harness.FileStorage.Verify(f => f.DeleteAsync("news/1.png", It.IsAny<CancellationToken>()), Times.Once);
        harness.FileStorage.Verify(f => f.DeleteAsync("news/2.png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_BlobDeletionFails_StillReportsSuccess()
    {
        var first = NewsImage(1);
        var second = NewsImage(2);
        var harness = CreateHarness(posts: [ExistingPost(10, first, second)], storageObjects: [first, second]);
        harness.FileStorage
            .Setup(f => f.DeleteAsync("news/1.png", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("minio down"));

        var deleted = await harness.Service.DeleteAsync(10, TestContext.Current.CancellationToken);

        Assert.True(deleted);
        harness.FileStorage.Verify(f => f.DeleteAsync("news/2.png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadImageAsync_StoresBlobUnderNewsPrefixAndPersistsRow()
    {
        var harness = CreateHarness();
        using var content = new MemoryStream([1, 2, 3]);

        await harness.Service.UploadImageAsync(content, "image/webp", 3, actingAdminId: 5, TestContext.Current.CancellationToken);

        harness.FileStorage.Verify(f => f.UploadAsync(
            It.Is<string>(key => key.StartsWith("news/") && key.EndsWith(".webp")), content, "image/webp", It.IsAny<CancellationToken>()), Times.Once);
        harness.StorageObjects.Verify(s => s.Add(It.Is<StorageObject>(o =>
            o.ObjectType == StorageObjectType.Image &&
            o.ObjectKey.StartsWith("news/") &&
            o.CreatedByUserId == 5 &&
            o.SizeBytes == 3)), Times.Once);
    }

    [Fact]
    public async Task UploadImageAsync_SaveFails_DeletesUploadedBlob()
    {
        var harness = CreateHarness();
        string? uploadedKey = null;
        harness.FileStorage
            .Setup(f => f.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string key, Stream _, string _, CancellationToken _) => uploadedKey = key)
            .Returns(Task.CompletedTask);
        harness.DbContext
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("db down"));
        using var content = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            harness.Service.UploadImageAsync(content, "image/png", 3, actingAdminId: 5, TestContext.Current.CancellationToken));

        Assert.NotNull(uploadedKey);
        harness.FileStorage.Verify(f => f.DeleteAsync(uploadedKey, It.IsAny<CancellationToken>()), Times.Once);
    }
}
