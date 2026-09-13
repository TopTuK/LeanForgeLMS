using LF.AppDomain.Entities.News;

namespace LF.AppDomainTests.Entities.News;

public class NewsReadMarkerTests
{
    private static readonly DateTime Now = new(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_SetsUserAndTimestamp()
    {
        var marker = NewsReadMarker.Create(5, Now);

        Assert.Equal(5, marker.UserId);
        Assert.Equal(Now, marker.LastSeenAt);
    }

    [Fact]
    public void Create_NonPositiveUser_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => NewsReadMarker.Create(0, Now));

    [Fact]
    public void MarkSeen_LaterTimestamp_Advances()
    {
        var marker = NewsReadMarker.Create(5, Now);

        marker.MarkSeen(Now.AddMinutes(10));

        Assert.Equal(Now.AddMinutes(10), marker.LastSeenAt);
    }

    [Fact]
    public void MarkSeen_EarlierTimestamp_KeepsLatest()
    {
        var marker = NewsReadMarker.Create(5, Now);

        marker.MarkSeen(Now.AddMinutes(-10));

        Assert.Equal(Now, marker.LastSeenAt);
    }
}
