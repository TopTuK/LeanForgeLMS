using LF.Application.Common.Exceptions;
using LF.Application.ModelDto.Lectures;
using LF.Application.Services.Lectures;
using Microsoft.Extensions.Logging.Abstractions;

using static LF.ApplicationTests.Services.Groups.GroupTestWorld;

namespace LF.ApplicationTests.Services.Groups;

public class LectureServiceTests
{
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private static LectureService CreateService(GroupTestWorld world) =>
        new(NullLogger<LectureService>.Instance, world.DbContext.Object, new FixedTimeProvider(Now));

    private static LectureInputDto Input(params int[] groupIds) => new()
    {
        Title = "Coroutines",
        StartsAt = Now.AddDays(3),
        DurationMinutes = 90,
        MeetingUrl = "https://meet.example/kotlin",
        GroupIds = groupIds,
    };

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ScheduleAsync_Staff_SchedulesForCourseGroups()
    {
        var world = new GroupTestWorld();
        var groupA = world.AddGroup("Stream A");
        var groupB = world.AddGroup("Stream B");

        var lecture = await CreateService(world).ScheduleAsync(CourseId, Input(groupA.Id, groupB.Id), InstructorId, isAdmin: false, Ct);

        Assert.NotNull(lecture);
        Assert.Equal(["Stream A", "Stream B"], lecture.Groups.Select(g => g.Name));
        Assert.Equal("https://meet.example/kotlin", lecture.MeetingUrl);
        Assert.True(lecture.IsTeaching);
    }

    [Fact]
    public async Task ScheduleAsync_GroupFromAnotherCourse_Throws()
    {
        var world = new GroupTestWorld();
        var foreignGroup = world.AddGroup("Rust stream", OtherCourseId);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(world).ScheduleAsync(CourseId, Input(foreignGroup.Id), CreatorId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task ScheduleAsync_NotStaff_Throws()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup();

        await Assert.ThrowsAsync<GroupAuthorizationException>(() =>
            CreateService(world).ScheduleAsync(CourseId, Input(group.Id), OtherInstructorId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task CancelAsync_HidesMeetingUrl_AndBlocksFurtherEdits()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup();
        var lecture = world.AddLecture(Now.AddDays(1), groupIds: [group.Id]);
        var service = CreateService(world);

        var cancelled = await service.CancelAsync(lecture.Id, CreatorId, isAdmin: false, Ct);

        Assert.NotNull(cancelled);
        Assert.True(cancelled.IsCancelled);
        Assert.Null(cancelled.MeetingUrl);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(lecture.Id, Input(group.Id), CreatorId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task ListMyScheduleAsync_Student_SeesOnlyTheirGroupsLectures()
    {
        var world = new GroupTestWorld();
        var mine = world.AddGroup("Stream A", memberIds: [StudentId]);
        var other = world.AddGroup("Stream B", memberIds: [ClassmateId]);
        world.AddLecture(Now.AddDays(1), groupIds: [mine.Id]);
        world.AddLecture(Now.AddDays(2), groupIds: [other.Id]);
        world.AddLecture(Now.AddDays(3), groupIds: [mine.Id, other.Id]);

        var schedule = await CreateService(world).ListMyScheduleAsync(StudentId, Now, Now.AddDays(30), Ct);

        Assert.Equal([1, 3], schedule.Select(l => l.Id));
        Assert.All(schedule, l => Assert.False(l.IsTeaching));
    }

    [Fact]
    public async Task ListMyScheduleAsync_Instructor_SeesEveryLectureOfTheirCourses()
    {
        var world = new GroupTestWorld();
        var groupA = world.AddGroup("Stream A");
        var groupB = world.AddGroup("Stream B");
        world.AddLecture(Now.AddDays(1), groupIds: [groupA.Id]);
        world.AddLecture(Now.AddDays(2), groupIds: [groupB.Id]);

        var schedule = await CreateService(world).ListMyScheduleAsync(InstructorId, Now, Now.AddDays(30), Ct);

        Assert.Equal(2, schedule.Count);
        Assert.All(schedule, l => Assert.True(l.IsTeaching));
    }

    [Fact]
    public async Task ListMyScheduleAsync_RemovedEnrollment_LosesTheSchedule()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId]);
        world.AddLecture(Now.AddDays(1), groupIds: [group.Id]);
        world.Unenroll(StudentId, CourseId);

        Assert.Empty(await CreateService(world).ListMyScheduleAsync(StudentId, Now, Now.AddDays(30), Ct));
    }

    [Fact]
    public async Task ListMyScheduleAsync_OversizedWindow_Throws() =>
        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(new GroupTestWorld()).ListMyScheduleAsync(StudentId, Now, Now.AddYears(2), Ct));
}
