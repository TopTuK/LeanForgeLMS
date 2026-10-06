using LF.AppDomain.Entities.Groups;
using LF.WebApi.Endpoints;

namespace LF.WebApiTests.Endpoints;

public class GroupModelsValidatorTests
{
    private static readonly DateTime Start = new(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc);

    private static LectureRequest Lecture(
        string title = "Coroutines",
        int duration = 90,
        string? meetingUrl = "https://meet.example/abc",
        IReadOnlyList<int>? groupIds = null) =>
        new(title, null, Start, duration, meetingUrl, groupIds ?? [1]);

    private static bool IsValid(LectureRequest request) => new LectureRequestValidator().Validate(request).IsValid;

    [Fact]
    public void StudentGroup_Valid_Passes() =>
        Assert.True(new StudentGroupRequestValidator().Validate(new StudentGroupRequest("Stream A", null)).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void StudentGroup_MissingName_Fails(string? name) =>
        Assert.False(new StudentGroupRequestValidator().Validate(new StudentGroupRequest(name!, null)).IsValid);

    [Fact]
    public void StudentGroup_NameTooLong_Fails() =>
        Assert.False(new StudentGroupRequestValidator()
            .Validate(new StudentGroupRequest(new string('x', StudentGroup.MaxNameLength + 1), null)).IsValid);

    [Fact]
    public void AddMembers_EmptyOrNonPositive_Fails()
    {
        var validator = new AddGroupMembersRequestValidator();

        Assert.False(validator.Validate(new AddGroupMembersRequest([])).IsValid);
        Assert.False(validator.Validate(new AddGroupMembersRequest([1, 0])).IsValid);
        Assert.True(validator.Validate(new AddGroupMembersRequest([1, 2])).IsValid);
    }

    [Fact]
    public void Lecture_Valid_Passes() => Assert.True(IsValid(Lecture()));

    [Fact]
    public void Lecture_WithoutMeetingUrl_Passes() => Assert.True(IsValid(Lecture(meetingUrl: null)));

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("meet.example/abc")]
    public void Lecture_BadMeetingUrl_Fails(string url) => Assert.False(IsValid(Lecture(meetingUrl: url)));

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(601)]
    public void Lecture_DurationOutOfRange_Fails(int duration) => Assert.False(IsValid(Lecture(duration: duration)));

    [Fact]
    public void Lecture_NoGroups_Fails() => Assert.False(IsValid(Lecture(groupIds: [])));

    [Fact]
    public void Lecture_EmptyTitle_Fails() => Assert.False(IsValid(Lecture(title: "")));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ChatMessage_Blank_Fails(string body) =>
        Assert.False(new PostGroupChatMessageRequestValidator().Validate(new PostGroupChatMessageRequest(body)).IsValid);

    [Fact]
    public void ChatMessage_TooLong_Fails() =>
        Assert.False(new PostGroupChatMessageRequestValidator()
            .Validate(new PostGroupChatMessageRequest(new string('x', GroupChatMessage.MaxBodyLength + 1))).IsValid);
}
