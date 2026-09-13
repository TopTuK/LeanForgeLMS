using LF.AppDomain.Entities.News;
using LF.WebApi.Endpoints;

namespace LF.WebApiTests.Endpoints;

public class AdminNewsModelsValidatorTests
{
    private static SaveNewsPostRequest Request(
        string title = "Launch",
        string html = "<p>Hello</p>",
        string visibility = "Public",
        IReadOnlyList<int>? imageIds = null) =>
        new(title, html, visibility, IsPublished: true, imageIds);

    private static bool IsValid(SaveNewsPostRequest request) =>
        new SaveNewsPostRequestValidator().Validate(request).IsValid;

    [Fact]
    public void SaveNewsPost_ValidRequest_Passes()
    {
        Assert.True(IsValid(Request(imageIds: [1, 2, 3])));
    }

    [Fact]
    public void SaveNewsPost_NullImageList_Passes()
    {
        Assert.True(IsValid(Request(imageIds: null)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SaveNewsPost_BlankTitle_Fails(string title)
    {
        Assert.False(IsValid(Request(title: title)));
    }

    [Fact]
    public void SaveNewsPost_TitleTooLong_Fails()
    {
        Assert.False(IsValid(Request(title: new string('a', NewsPost.MaxTitleLength + 1))));
    }

    [Fact]
    public void SaveNewsPost_BlankHtml_Fails()
    {
        Assert.False(IsValid(Request(html: "")));
    }

    [Fact]
    public void SaveNewsPost_HtmlTooLong_Fails()
    {
        Assert.False(IsValid(Request(html: new string('a', SaveNewsPostRequestValidator.MaxHtmlLength + 1))));
    }

    [Theory]
    [InlineData("Public")]
    [InlineData("MembersOnly")]
    [InlineData("membersonly")]
    public void SaveNewsPost_KnownVisibility_Passes(string visibility)
    {
        Assert.True(IsValid(Request(visibility: visibility)));
    }

    [Theory]
    [InlineData("Secret")]
    [InlineData("7")]
    [InlineData("")]
    public void SaveNewsPost_UnknownVisibility_Fails(string visibility)
    {
        Assert.False(IsValid(Request(visibility: visibility)));
    }

    [Fact]
    public void SaveNewsPost_TooManyImages_Fails()
    {
        Assert.False(IsValid(Request(imageIds: [.. Enumerable.Range(1, NewsPost.MaxImages + 1)])));
    }

    [Fact]
    public void SaveNewsPost_DuplicateImages_Fails()
    {
        Assert.False(IsValid(Request(imageIds: [1, 1])));
    }

    [Fact]
    public void SaveNewsPost_NonPositiveImageId_Fails()
    {
        Assert.False(IsValid(Request(imageIds: [0])));
    }
}
