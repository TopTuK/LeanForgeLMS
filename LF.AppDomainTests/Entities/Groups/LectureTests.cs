using LF.AppDomain.Entities.Groups;

namespace LF.AppDomainTests.Entities.Groups;

public class LectureTests
{
    private static readonly DateTime Now = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);

    private static Lecture Schedule(string? meetingUrl = "https://meet.example/abc", int duration = 90, params int[] groupIds) =>
        Lecture.Schedule(7, "Coroutines", null, Now.AddDays(1), duration, meetingUrl, groupIds.Length == 0 ? [1] : groupIds, 3, Now);

    [Fact]
    public void Schedule_SetsTimeAndGroups()
    {
        var lecture = Schedule(groupIds: [1, 2, 2]);

        Assert.Equal(Now.AddDays(1), lecture.StartsAt);
        Assert.Equal(Now.AddDays(1).AddMinutes(90), lecture.EndsAt);
        Assert.Equal([1, 2], lecture.Groups.Select(g => g.GroupId));
        Assert.False(lecture.IsCancelled);
    }

    [Fact]
    public void Schedule_NoGroups_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Lecture.Schedule(7, "Coroutines", null, Now.AddDays(1), 90, null, [], 3, Now));

    [Fact]
    public void Schedule_NonUtcStart_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            Lecture.Schedule(7, "Coroutines", null, DateTime.SpecifyKind(Now, DateTimeKind.Unspecified), 90, null, [1], 3, Now));

    [Theory]
    [InlineData(Lecture.MinDurationMinutes - 1)]
    [InlineData(Lecture.MaxDurationMinutes + 1)]
    public void Schedule_DurationOutOfRange_Throws(int duration) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Schedule(duration: duration));

    // The URL is rendered as a link for students, so script/data URLs must never get through.
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,hi")]
    [InlineData("meet.example/abc")]
    [InlineData("ftp://meet.example/abc")]
    public void Schedule_UnsafeOrRelativeMeetingUrl_Throws(string url) =>
        Assert.Throws<ArgumentException>(() => Schedule(meetingUrl: url));

    [Fact]
    public void Schedule_BlankMeetingUrl_IsAllowed() =>
        Assert.Null(Schedule(meetingUrl: "  ").MeetingUrl);

    [Fact]
    public void AssignGroups_ReplacesTheSet_AndReportsChanges()
    {
        var lecture = Schedule(groupIds: [1, 2]);

        Assert.False(lecture.AssignGroups([2, 1]));
        Assert.True(lecture.AssignGroups([2, 3]));
        Assert.Equal([2, 3], lecture.Groups.Select(g => g.GroupId).Order());
    }

    [Fact]
    public void Cancel_Twice_Throws()
    {
        var lecture = Schedule();
        lecture.Cancel();

        Assert.True(lecture.IsCancelled);
        Assert.Throws<InvalidOperationException>(lecture.Cancel);
    }
}
