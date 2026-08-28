using LF.AppDomain.Models.Storage.Enums;
using LF.WebApi.Endpoints;

namespace LF.WebApiTests.Endpoints;

public class UploadRulesTests
{
    [Fact]
    public void AvatarUpload_MaxSize_Is5Mb()
    {
        Assert.Equal(5 * 1024 * 1024, AvatarUpload.MaxSizeBytes);
    }

    [Theory]
    [InlineData("image/png", ".png")]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/webp", ".webp")]
    public void AvatarUpload_AllowedContentTypes_MapToExpectedExtension(string contentType, string expectedExtension)
    {
        Assert.True(AvatarUpload.AllowedContentTypes.TryGetValue(contentType, out var extension));
        Assert.Equal(expectedExtension, extension);
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("application/pdf")]
    public void AvatarUpload_RejectsUnsupportedContentTypes(string contentType)
    {
        Assert.False(AvatarUpload.AllowedContentTypes.ContainsKey(contentType));
    }

    [Fact]
    public void CourseCoverImageUpload_MaxSize_Is5Mb()
    {
        Assert.Equal(5 * 1024 * 1024, CourseCoverImageUpload.MaxSizeBytes);
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("image/webp")]
    public void CourseCoverImageUpload_AllowsImageTypes(string contentType)
    {
        Assert.True(CourseCoverImageUpload.AllowedContentTypes.ContainsKey(contentType));
    }

    [Theory]
    [InlineData("image/png", StorageObjectType.Image, 5 * 1024 * 1024L)]
    [InlineData("video/mp4", StorageObjectType.Video, 200 * 1024 * 1024L)]
    [InlineData("audio/mpeg", StorageObjectType.Audio, 50 * 1024 * 1024L)]
    public void LessonMediaUpload_MapsContentTypeToObjectTypeAndLimit(string contentType, StorageObjectType expectedType, long expectedMaxSize)
    {
        Assert.True(LessonMediaUpload.AllowedContentTypes.TryGetValue(contentType, out var info));
        Assert.Equal(expectedType, info.ObjectType);
        Assert.Equal(expectedMaxSize, info.MaxSizeBytes);
    }

    [Fact]
    public void LessonMediaUpload_RejectsArbitraryContentType()
    {
        Assert.False(LessonMediaUpload.AllowedContentTypes.ContainsKey("application/x-msdownload"));
    }

    [Fact]
    public void LessonFileUpload_MaxSize_Is25Mb()
    {
        Assert.Equal(25 * 1024 * 1024, LessonFileUpload.MaxSizeBytes);
    }
}
