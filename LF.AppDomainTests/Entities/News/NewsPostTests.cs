using LF.AppDomain.Entities.News;
using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.News.Enums;
using LF.AppDomain.Models.Storage.Enums;
using LF.AppDomainTests.TestSupport;

namespace LF.AppDomainTests.Entities.News;

public class NewsPostTests
{
    private static readonly DateTime Now = new(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    private static NewsPost CreatePost(NewsVisibility visibility = NewsVisibility.Public) =>
        NewsPost.Create("Launch", "<p>Hello</p>", visibility, createdByUserId: 1, Now);

    private static StorageObject Image(int id, StorageObjectType type = StorageObjectType.Image)
    {
        var storageObject = StorageObject.Create(type, $"news/{id}.png", "image/png", 10, createdByUserId: 1, Now);
        EntityIdSetter.SetId(storageObject, id);
        return storageObject;
    }

    [Fact]
    public void Create_ValidInput_CreatesTrimmedUnpublishedDraft()
    {
        var post = NewsPost.Create("  Launch  ", " <p>Hello</p> ", NewsVisibility.MembersOnly, createdByUserId: 7, Now);

        Assert.Equal("Launch", post.Title);
        Assert.Equal("<p>Hello</p>", post.Html);
        Assert.Equal(NewsVisibility.MembersOnly, post.Visibility);
        Assert.Equal(7, post.CreatedByUserId);
        Assert.Equal(Now, post.CreatedAt);
        Assert.Equal(Now, post.UpdatedAt);
        Assert.False(post.IsPublished);
        Assert.Null(post.PublishedAt);
        Assert.Empty(post.Images);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankTitle_Throws(string title) =>
        Assert.Throws<ArgumentException>(() => NewsPost.Create(title, "<p>Hello</p>", NewsVisibility.Public, 1, Now));

    [Fact]
    public void Create_TitleTooLong_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            NewsPost.Create(new string('a', NewsPost.MaxTitleLength + 1), "<p>Hello</p>", NewsVisibility.Public, 1, Now));

    [Fact]
    public void Create_BlankHtml_Throws() =>
        Assert.Throws<ArgumentException>(() => NewsPost.Create("Launch", "  ", NewsVisibility.Public, 1, Now));

    [Fact]
    public void Create_UnknownVisibility_Throws() =>
        Assert.Throws<ArgumentException>(() => NewsPost.Create("Launch", "<p>Hello</p>", (NewsVisibility)42, 1, Now));

    [Fact]
    public void Create_NonPositiveAuthor_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => NewsPost.Create("Launch", "<p>Hello</p>", NewsVisibility.Public, 0, Now));

    [Fact]
    public void Edit_ReplacesContentAndStampsUpdatedAt()
    {
        var post = CreatePost();
        var later = Now.AddHours(2);

        post.Edit("Relaunch", "<p>Updated</p>", NewsVisibility.MembersOnly, later);

        Assert.Equal("Relaunch", post.Title);
        Assert.Equal("<p>Updated</p>", post.Html);
        Assert.Equal(NewsVisibility.MembersOnly, post.Visibility);
        Assert.Equal(Now, post.CreatedAt);
        Assert.Equal(later, post.UpdatedAt);
    }

    [Fact]
    public void Publish_FirstTime_StampsPublishedAt()
    {
        var post = CreatePost();

        post.Publish(Now);

        Assert.True(post.IsPublished);
        Assert.Equal(Now, post.PublishedAt);
    }

    // PublishedAt drives unread counts, so hiding and re-showing a post must not make it "new" again.
    [Fact]
    public void Publish_AfterUnpublish_KeepsOriginalPublishedAt()
    {
        var post = CreatePost();
        post.Publish(Now);

        post.Unpublish();
        Assert.False(post.IsPublished);

        post.Publish(Now.AddDays(3));

        Assert.True(post.IsPublished);
        Assert.Equal(Now, post.PublishedAt);
    }

    [Fact]
    public void ReplaceImages_AssignsSortOrderInRequestedOrder()
    {
        var post = CreatePost();

        post.ReplaceImages([Image(3), Image(1), Image(2)]);

        Assert.Equal([3, 1, 2], post.Images.Select(i => i.StorageObjectId));
        Assert.Equal([1, 2, 3], post.Images.Select(i => i.SortOrder));
    }

    [Fact]
    public void ReplaceImages_ReordersKeptImagesInPlaceAndReturnsRemovedIds()
    {
        var post = CreatePost();
        var first = Image(1);
        var second = Image(2);
        var third = Image(3);
        post.ReplaceImages([first, second, third]);
        var keptRow = post.Images.Single(i => i.StorageObjectId == 3);

        var removed = post.ReplaceImages([third, first, Image(4)]);

        Assert.Equal([2], removed);
        Assert.Equal([3, 1, 4], post.Images.Select(i => i.StorageObjectId));
        Assert.Same(keptRow, post.Images[0]);
        Assert.Equal(1, keptRow.SortOrder);
    }

    [Fact]
    public void ReplaceImages_EmptyList_RemovesEverything()
    {
        var post = CreatePost();
        post.ReplaceImages([Image(1), Image(2)]);

        var removed = post.ReplaceImages([]);

        Assert.Equal([1, 2], removed);
        Assert.Empty(post.Images);
    }

    [Fact]
    public void ReplaceImages_TooMany_Throws()
    {
        var post = CreatePost();
        var images = Enumerable.Range(1, NewsPost.MaxImages + 1).Select(id => Image(id)).ToList();

        Assert.Throws<ArgumentException>(() => post.ReplaceImages(images));
    }

    [Fact]
    public void ReplaceImages_DuplicateImage_Throws()
    {
        var post = CreatePost();
        var image = Image(1);

        Assert.Throws<ArgumentException>(() => post.ReplaceImages([image, image]));
    }

    [Fact]
    public void ReplaceImages_NonImageStorageObject_Throws()
    {
        var post = CreatePost();

        Assert.Throws<ArgumentException>(() => post.ReplaceImages([Image(1, StorageObjectType.Video)]));
    }

    [Theory]
    [InlineData(NewsVisibility.Public, true, false, true)]
    [InlineData(NewsVisibility.Public, true, true, true)]
    [InlineData(NewsVisibility.MembersOnly, true, false, false)]
    [InlineData(NewsVisibility.MembersOnly, true, true, true)]
    [InlineData(NewsVisibility.Public, false, true, false)]
    public void IsVisibleTo_RespectsPublishStateAndVisibility(NewsVisibility visibility, bool published, bool isAuthenticated, bool expected)
    {
        var post = CreatePost(visibility);
        if (published)
            post.Publish(Now);

        Assert.Equal(expected, post.IsVisibleTo(isAuthenticated));
    }
}
