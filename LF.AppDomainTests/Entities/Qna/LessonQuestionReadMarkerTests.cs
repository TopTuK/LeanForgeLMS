using LF.AppDomain.Entities.Qna;

namespace LF.AppDomainTests.Entities.Qna;

public class LessonQuestionReadMarkerTests
{
    private static readonly DateTime Now = new(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_SetsThreadUserAndTimestamp()
    {
        var marker = LessonQuestionReadMarker.Create(lessonQuestionId: 4, userId: 5, Now);

        Assert.Equal(4, marker.LessonQuestionId);
        Assert.Equal(5, marker.UserId);
        Assert.Equal(Now, marker.LastSeenAt);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(4, 0)]
    public void Create_NonPositiveIdentifiers_Throw(int questionId, int userId) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => LessonQuestionReadMarker.Create(questionId, userId, Now));

    [Fact]
    public void MarkSeen_LaterTimestamp_Advances()
    {
        var marker = LessonQuestionReadMarker.Create(4, 5, Now);

        marker.MarkSeen(Now.AddMinutes(10));

        Assert.Equal(Now.AddMinutes(10), marker.LastSeenAt);
    }

    [Fact]
    public void MarkSeen_EarlierTimestamp_KeepsLatest()
    {
        var marker = LessonQuestionReadMarker.Create(4, 5, Now);

        marker.MarkSeen(Now.AddMinutes(-10));

        Assert.Equal(Now, marker.LastSeenAt);
    }
}
