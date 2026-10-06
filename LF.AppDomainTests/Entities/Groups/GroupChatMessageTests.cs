using LF.AppDomain.Entities.Groups;

namespace LF.AppDomainTests.Entities.Groups;

public class GroupChatMessageTests
{
    private static readonly DateTime Now = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Post_TrimsBody()
    {
        var message = GroupChatMessage.Post(groupId: 1, authorUserId: 10, "  hi  ", Now);

        Assert.Equal("hi", message.Body);
        Assert.False(message.IsDeleted);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Post_EmptyBody_Throws(string body) =>
        Assert.Throws<ArgumentException>(() => GroupChatMessage.Post(1, 10, body, Now));

    [Fact]
    public void Post_OverlongBody_Throws() =>
        Assert.Throws<ArgumentException>(() => GroupChatMessage.Post(1, 10, new string('x', GroupChatMessage.MaxBodyLength + 1), Now));

    [Fact]
    public void SoftDelete_IsIdempotent()
    {
        var message = GroupChatMessage.Post(1, 10, "hi", Now);

        Assert.True(message.SoftDelete(deletedByUserId: 3, Now));
        Assert.False(message.SoftDelete(3, Now));
        Assert.Equal(3, message.DeletedByUserId);
    }

    [Fact]
    public void ReadMarker_NeverMovesBackwards()
    {
        var marker = GroupChatReadMarker.Create(groupId: 1, userId: 10, lastSeenMessageId: 5);

        Assert.False(marker.MarkSeen(4));
        Assert.True(marker.MarkSeen(9));
        Assert.Equal(9, marker.LastSeenMessageId);
    }
}
