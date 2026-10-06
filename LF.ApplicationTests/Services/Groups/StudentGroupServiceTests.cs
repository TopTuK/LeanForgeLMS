using LF.Application.Common.Exceptions;
using LF.Application.Services.Groups;
using Microsoft.Extensions.Logging.Abstractions;

using static LF.ApplicationTests.Services.Groups.GroupTestWorld;

namespace LF.ApplicationTests.Services.Groups;

public class StudentGroupServiceTests
{
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private static StudentGroupService CreateService(GroupTestWorld world) =>
        new(NullLogger<StudentGroupService>.Instance, world.DbContext.Object, new FixedTimeProvider(Now));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(CreatorId, false)]
    [InlineData(InstructorId, false)]
    [InlineData(AdminId, true)]
    public async Task CreateAsync_TeachingStaffOrAdmin_CreatesGroup(int actingUserId, bool isAdmin)
    {
        var world = new GroupTestWorld();

        var group = await CreateService(world).CreateAsync(CourseId, "  Stream A  ", "Mornings", actingUserId, isAdmin, Ct);

        Assert.NotNull(group);
        Assert.Equal("Stream A", group.Name);
        Assert.True(group.CanManage);
        Assert.Single(world.StudentGroups);
    }

    // The Instructor role is global; teaching rights are per course.
    [Theory]
    [InlineData(OtherInstructorId)]
    [InlineData(StudentId)]
    public async Task CreateAsync_NotStaffOfThisCourse_Throws(int actingUserId)
    {
        var world = new GroupTestWorld();

        await Assert.ThrowsAsync<GroupAuthorizationException>(() =>
            CreateService(world).CreateAsync(CourseId, "Stream A", null, actingUserId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task CreateAsync_UnknownCourse_ReturnsNull() =>
        Assert.Null(await CreateService(new GroupTestWorld()).CreateAsync(999, "Stream A", null, CreatorId, isAdmin: false, Ct));

    [Fact]
    public async Task CreateAsync_DuplicateNameInCourse_Throws()
    {
        var world = new GroupTestWorld();
        world.AddGroup("Stream A");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(world).CreateAsync(CourseId, "Stream A", null, CreatorId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task AddMembersAsync_ActiveEnrollees_AreAdded()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup();

        var result = await CreateService(world).AddMembersAsync(group.Id, [StudentId, ClassmateId], InstructorId, isAdmin: false, Ct);

        Assert.NotNull(result);
        Assert.Equal([StudentId, ClassmateId], result.Members.Select(m => m.UserId).Order());
        Assert.All(result.Members, m => Assert.NotNull(m.Email));
    }

    [Fact]
    public async Task AddMembersAsync_ExistingMember_IsNoOp()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId]);

        var result = await CreateService(world).AddMembersAsync(group.Id, [StudentId], CreatorId, isAdmin: false, Ct);

        Assert.NotNull(result);
        Assert.Single(result.Members);
    }

    // Pending-payment and other-course students must not slip into a group.
    [Theory]
    [InlineData(PendingStudentId)]
    [InlineData(OutsiderId)]
    public async Task AddMembersAsync_NotActivelyEnrolledInCourse_Throws(int userId)
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(world).AddMembersAsync(group.Id, [StudentId, userId], CreatorId, isAdmin: false, Ct));

        Assert.Empty(group.Members);
    }

    [Fact]
    public async Task RemoveMemberAsync_RemovesTheMember()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId, ClassmateId]);

        var result = await CreateService(world).RemoveMemberAsync(group.Id, StudentId, CreatorId, isAdmin: false, Ct);

        Assert.NotNull(result);
        Assert.Equal(ClassmateId, Assert.Single(result.Members).UserId);
    }

    [Fact]
    public async Task GetAsync_Member_SeesClassmatesWithoutEmails()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId, ClassmateId]);

        var result = await CreateService(world).GetAsync(group.Id, StudentId, isAdmin: false, Ct);

        Assert.NotNull(result);
        Assert.False(result.CanManage);
        Assert.Equal(2, result.Members.Count);
        Assert.All(result.Members, m => Assert.Null(m.Email));
    }

    [Fact]
    public async Task GetAsync_NonMemberStudent_Throws()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId]);

        await Assert.ThrowsAsync<GroupAuthorizationException>(() =>
            CreateService(world).GetAsync(group.Id, ClassmateId, isAdmin: false, Ct));
    }

    // Enrollment removal happens in LF.CourseService and never touches group rows; access must
    // still disappear with it.
    [Fact]
    public async Task GetAsync_MemberWhoseEnrollmentWasRemoved_Throws()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId]);
        world.Unenroll(StudentId, CourseId);

        await Assert.ThrowsAsync<GroupAuthorizationException>(() =>
            CreateService(world).GetAsync(group.Id, StudentId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task GetAsync_Staff_SeesUnenrolledMembersFlagged()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId, ClassmateId]);
        world.Unenroll(ClassmateId, CourseId);

        var result = await CreateService(world).GetAsync(group.Id, InstructorId, isAdmin: false, Ct);

        Assert.NotNull(result);
        Assert.False(result.Members.Single(m => m.UserId == ClassmateId).IsEnrolled);
        Assert.True(result.Members.Single(m => m.UserId == StudentId).IsEnrolled);
    }

    [Fact]
    public async Task ListMyGroupsAsync_ReturnsOnlyGroupsWithActiveEnrollment()
    {
        var world = new GroupTestWorld();
        world.AddGroup("Stream A", memberIds: [StudentId]);
        world.AddGroup("Stream B", memberIds: [StudentId]);
        world.AddGroup("Stream C", memberIds: [ClassmateId]);
        world.AddLecture(Now.AddDays(2), groupIds: [1]);
        world.AddLecture(Now.AddDays(1), groupIds: [1]);

        var groups = await CreateService(world).ListMyGroupsAsync(StudentId, Ct);

        Assert.Equal(["Stream A", "Stream B"], groups.Select(g => g.Name));
        Assert.All(groups, g => Assert.False(g.IsTeaching));
        Assert.Equal(Now.AddDays(1), groups[0].NextLectureStartsAt);
        Assert.Null(groups[1].NextLectureStartsAt);
    }

    [Fact]
    public async Task ListMyGroupsAsync_Instructor_SeesTaughtGroupsFlagged()
    {
        var world = new GroupTestWorld();
        world.AddGroup("Stream A", memberIds: [StudentId]);
        world.AddGroup("Rust stream", OtherCourseId);

        var groups = await CreateService(world).ListMyGroupsAsync(InstructorId, Ct);

        var group = Assert.Single(groups);
        Assert.Equal("Stream A", group.Name);
        Assert.True(group.IsTeaching);
    }

    [Fact]
    public async Task ListEligibleStudentsAsync_ReturnsActiveEnrolleesOnly()
    {
        var world = new GroupTestWorld();

        var students = await CreateService(world).ListEligibleStudentsAsync(CourseId, CreatorId, isAdmin: false, Ct);

        Assert.NotNull(students);
        Assert.Equal([StudentId, ClassmateId], students.Select(s => s.UserId).Order());
    }

    [Fact]
    public async Task DeleteAsync_Staff_RemovesGroup()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup();

        Assert.True(await CreateService(world).DeleteAsync(group.Id, InstructorId, isAdmin: false, Ct));
        Assert.Empty(world.StudentGroups);
    }
}
