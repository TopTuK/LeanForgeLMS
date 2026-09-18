using LF.AppDomain.Entities.Qna;
using LF.AppDomain.Models.Qna.Enums;
using LF.Application.ModelDto.Qna;
using LF.WebApi.Endpoints;

namespace LF.WebApiTests.Endpoints;

public class LessonQuestionModelsValidatorTests
{
    private static AskQuestionRequest AskRequest(
        int lessonId = 42,
        string title = "Why does this fail?",
        string body = "I am stuck on step two.") =>
        new(lessonId, title, body);

    private static bool IsValid(AskQuestionRequest request) =>
        new AskQuestionRequestValidator().Validate(request).IsValid;

    private static bool IsValid(PostQuestionMessageRequest request) =>
        new PostQuestionMessageRequestValidator().Validate(request).IsValid;

    [Fact]
    public void AskQuestion_ValidRequest_Passes() => Assert.True(IsValid(AskRequest()));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AskQuestion_NonPositiveLesson_Fails(int lessonId) => Assert.False(IsValid(AskRequest(lessonId: lessonId)));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AskQuestion_BlankTitle_Fails(string title) => Assert.False(IsValid(AskRequest(title: title)));

    [Fact]
    public void AskQuestion_TitleTooLong_Fails() =>
        Assert.False(IsValid(AskRequest(title: new string('a', LessonQuestion.MaxTitleLength + 1))));

    [Fact]
    public void AskQuestion_TitleAtLimit_Passes() =>
        Assert.True(IsValid(AskRequest(title: new string('a', LessonQuestion.MaxTitleLength))));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AskQuestion_BlankBody_Fails(string body) => Assert.False(IsValid(AskRequest(body: body)));

    [Fact]
    public void AskQuestion_BodyTooLong_Fails() =>
        Assert.False(IsValid(AskRequest(body: new string('a', LessonQuestionMessage.MaxBodyLength + 1))));

    [Fact]
    public void PostMessage_ValidRequest_Passes() => Assert.True(IsValid(new PostQuestionMessageRequest("Try it this way.")));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PostMessage_BlankBody_Fails(string body) => Assert.False(IsValid(new PostQuestionMessageRequest(body)));

    [Fact]
    public void PostMessage_BodyTooLong_Fails() =>
        Assert.False(IsValid(new PostQuestionMessageRequest(new string('a', LessonQuestionMessage.MaxBodyLength + 1))));

    [Theory]
    [InlineData("Open", LessonQuestionStatus.Open)]
    [InlineData("answered", LessonQuestionStatus.Answered)]
    [InlineData("CLOSED", LessonQuestionStatus.Closed)]
    public void TryParseStatus_KnownStatus_Parses(string input, LessonQuestionStatus expected)
    {
        Assert.True(LessonQuestionResponseMapper.TryParseStatus(input, out var parsed));
        Assert.Equal(expected, parsed);
    }

    // Enum.TryParse also accepts arbitrary numbers, so the parsed value has to be range-checked.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("Pending")]
    [InlineData("7")]
    [InlineData("0")]
    public void TryParseStatus_UnknownStatus_Fails(string? input) =>
        Assert.False(LessonQuestionResponseMapper.TryParseStatus(input, out _));

    [Theory]
    [InlineData("staff")]
    [InlineData("STAFF")]
    public void ParseScope_Staff_ReturnsAsStaff(string input) =>
        Assert.Equal(LessonQuestionScope.AsStaff, LessonQuestionResponseMapper.ParseScope(input));

    // Anything unrecognised falls back to the student inbox, which is the less privileged view.
    [Theory]
    [InlineData("student")]
    [InlineData("nonsense")]
    [InlineData("")]
    [InlineData(null)]
    public void ParseScope_AnythingElse_ReturnsAsStudent(string? input) =>
        Assert.Equal(LessonQuestionScope.AsStudent, LessonQuestionResponseMapper.ParseScope(input));
}
