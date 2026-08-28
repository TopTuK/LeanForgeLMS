using LF.AppDomain.Models.Course.Enums;
using LF.WebApi.Endpoints;

namespace LF.WebApiTests.Endpoints;

public class CourseModelsValidatorTests
{
    private static CreateCourseRequest ValidCreateCourse() => new(
        Title: "Intro to Lean",
        ShortIntroduction: "A short intro.",
        Description: "The full description.",
        CategoryId: 1,
        CoverType: nameof(CourseCoverType.None),
        CoverColor: null,
        CoverImageStorageObjectId: null);

    [Fact]
    public void CreateCourse_ValidRequest_Passes()
    {
        var result = new CreateCourseRequestValidator().Validate(ValidCreateCourse());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CreateCourse_MissingTitle_Fails(string? title)
    {
        var result = new CreateCourseRequestValidator().Validate(ValidCreateCourse() with { Title = title! });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCourseRequest.Title));
    }

    [Fact]
    public void CreateCourse_TitleTooLong_Fails()
    {
        var result = new CreateCourseRequestValidator().Validate(ValidCreateCourse() with { Title = new string('a', 201) });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateCourse_ShortIntroductionTooLong_Fails()
    {
        var result = new CreateCourseRequestValidator().Validate(ValidCreateCourse() with { ShortIntroduction = new string('a', 501) });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateCourse_CategoryIdNotPositive_Fails()
    {
        var result = new CreateCourseRequestValidator().Validate(ValidCreateCourse() with { CategoryId = 0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCourseRequest.CategoryId));
    }

    [Fact]
    public void CreateCourse_UnknownCoverType_Fails()
    {
        var result = new CreateCourseRequestValidator().Validate(ValidCreateCourse() with { CoverType = "Rainbow" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCourseRequest.CoverType));
    }

    [Fact]
    public void CreateCourse_ColorCoverWithoutValidColor_Fails()
    {
        var request = ValidCreateCourse() with { CoverType = nameof(CourseCoverType.Color), CoverColor = "Purple" };

        var result = new CreateCourseRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCourseRequest.CoverColor));
    }

    [Fact]
    public void CreateCourse_ColorCoverWithValidColor_Passes()
    {
        var request = ValidCreateCourse() with { CoverType = nameof(CourseCoverType.Color), CoverColor = nameof(CourseCoverColor.Ocean) };

        var result = new CreateCourseRequestValidator().Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateCourse_CoverTypeIsCaseSensitive_LowercaseFails()
    {
        // The CoverType rule uses Enum.TryParse without ignoreCase (unlike the endpoint's Enum.Parse),
        // so a lowercased value is rejected at validation time.
        var result = new CreateCourseRequestValidator().Validate(ValidCreateCourse() with { CoverType = "none" });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateCourse_ImageCoverWithNonPositiveStorageObjectId_Fails()
    {
        var request = ValidCreateCourse() with { CoverType = nameof(CourseCoverType.Image), CoverImageStorageObjectId = 0 };

        var result = new CreateCourseRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCourseRequest.CoverImageStorageObjectId));
    }

    [Fact]
    public void CreateCourse_ImageCoverWithStorageObjectId_Passes()
    {
        var request = ValidCreateCourse() with { CoverType = nameof(CourseCoverType.Image), CoverImageStorageObjectId = 42 };

        var result = new CreateCourseRequestValidator().Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void AddChapter_MissingTitle_Fails(string? title)
    {
        var result = new AddChapterRequestValidator().Validate(new AddChapterRequest(title!));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void AddChapter_ValidTitle_Passes()
    {
        var result = new AddChapterRequestValidator().Validate(new AddChapterRequest("Chapter 1"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RenameChapter_TitleTooLong_Fails()
    {
        var result = new RenameChapterRequestValidator().Validate(new RenameChapterRequest(new string('a', 201)));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("Up", true)]
    [InlineData("Down", true)]
    [InlineData("up", false)]
    [InlineData("Sideways", false)]
    [InlineData("", false)]
    public void Move_DirectionValidity(string direction, bool expectedValid)
    {
        var result = new MoveRequestValidator().Validate(new MoveRequest(direction));

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public void AddLesson_MissingTitle_Fails()
    {
        var result = new AddLessonRequestValidator().Validate(new AddLessonRequest("", "content", false));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void AddLesson_NullContent_Passes()
    {
        var result = new AddLessonRequestValidator().Validate(new AddLessonRequest("Lesson 1", null, true));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateLesson_TitleTooLong_Fails()
    {
        var result = new UpdateLessonRequestValidator().Validate(new UpdateLessonRequest(new string('a', 201), null, false));

        Assert.False(result.IsValid);
    }
}
