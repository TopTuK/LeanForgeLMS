using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.User;
using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.Services.CourseTeaching;
using LF.ApplicationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

using DomainCourse = LF.AppDomain.Entities.Course.Course;

namespace LF.ApplicationTests.Services.CourseTeaching;

public class CourseTeachingTeamServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);

    private const int CourseId = 7;
    private const int CreatorId = 200;
    private const int InstructorId = 201;
    private const int OtherInstructorId = 202;
    private const int StudentId = 100;
    private const int AdminId = 300;

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private sealed class Harness
    {
        public required CourseTeachingTeamService Service { get; init; }
        public required List<CourseInstructor> Assignments { get; init; }
        public required Mock<DbSet<CourseInstructor>> AssignmentsSet { get; init; }
    }

    private static Harness CreateHarness(IEnumerable<CourseInstructor>? assignments = null, bool courseExists = true)
    {
        var assignmentList = assignments?.ToList() ?? [];
        var assignmentsSet = assignmentList.BuildMockDbSet();

        assignmentsSet.Setup(s => s.Add(It.IsAny<CourseInstructor>()))
            .Callback<CourseInstructor>(a =>
            {
                EntityIdSetter.SetId(a, assignmentList.Count + 1);
                assignmentList.Add(a);
            });

        assignmentsSet.Setup(s => s.Remove(It.IsAny<CourseInstructor>()))
            .Callback<CourseInstructor>(a => assignmentList.Remove(a));

        var dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.CourseInstructors).Returns(assignmentsSet.Object);
        dbContextMock.SetupGet(c => c.Courses).Returns((courseExists ? new List<DomainCourse> { BuildCourse() } : []).BuildMockDbSet().Object);
        dbContextMock.SetupGet(c => c.Users).Returns(Users().BuildMockDbSet().Object);
        dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new Harness
        {
            Service = new CourseTeachingTeamService(
                NullLogger<CourseTeachingTeamService>.Instance, dbContextMock.Object, new FixedTimeProvider(Now)),
            Assignments = assignmentList,
            AssignmentsSet = assignmentsSet,
        };
    }

    private static List<DbUser> Users() =>
    [
        new() { Id = StudentId, Email = "student@lf.test", FirstName = "Sasha", LastName = "Student", Role = UserRole.Student, CreatedAt = Now },
        new() { Id = CreatorId, Email = "creator@lf.test", FirstName = "Kim", LastName = "Creator", Role = UserRole.CourseCreator, CreatedAt = Now },
        new() { Id = InstructorId, Email = "Assigned@LF.test", FirstName = "Ira", LastName = "Assigned", Role = UserRole.Instructor, CreatedAt = Now },
        new() { Id = OtherInstructorId, Email = "other@lf.test", FirstName = "Ulya", LastName = "Other", Role = UserRole.Instructor, CreatedAt = Now },
        new() { Id = AdminId, Email = "admin@lf.test", FirstName = "Ada", LastName = "Admin", Role = UserRole.Admin, CreatedAt = Now },
    ];

    private static DomainCourse BuildCourse()
    {
        var category = Category.Create("General", isDefault: true);
        EntityIdSetter.SetId(category, 1);

        var course = DomainCourse.Create("Kotlin Basics", "Intro", "Description", category, CreatorId, Now);
        EntityIdSetter.SetId(course, CourseId);
        return course;
    }

    private static CourseInstructor Assignment(int userId = InstructorId)
    {
        var assignment = CourseInstructor.Create(CourseId, userId, CreatorId, Now);
        EntityIdSetter.SetId(assignment, userId);
        return assignment;
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // The creator has no LFCourseInstructors row but is still teaching staff, so the list has to synthesise them.
    [Fact]
    public async Task ListAsync_IncludesTheCreatorFlaggedAsSuch()
    {
        var harness = CreateHarness([Assignment()]);

        var team = await harness.Service.ListAsync(CourseId, CreatorId, isAdmin: false, Ct);

        Assert.NotNull(team);
        Assert.Equal(2, team.Count);

        var creator = team.Single(m => m.UserId == CreatorId);
        Assert.True(creator.IsCreator);
        Assert.Equal("creator@lf.test", creator.Email);

        var instructor = team.Single(m => m.UserId == InstructorId);
        Assert.False(instructor.IsCreator);
    }

    [Fact]
    public async Task ListAsync_UnknownCourse_ReturnsNull() =>
        Assert.Null(await CreateHarness(courseExists: false).Service.ListAsync(CourseId, CreatorId, isAdmin: false, Ct));

    [Fact]
    public async Task ListAsync_Admin_BypassesOwnership()
    {
        var harness = CreateHarness();

        Assert.NotNull(await harness.Service.ListAsync(CourseId, AdminId, isAdmin: true, Ct));
    }

    // Holding the Instructor role is not the same as owning the course.
    [Fact]
    public async Task ListAsync_SomeoneElsesCourse_Throws()
    {
        var harness = CreateHarness();

        await Assert.ThrowsAsync<QuestionAuthorizationException>(
            () => harness.Service.ListAsync(CourseId, OtherInstructorId, isAdmin: false, Ct));
    }

    // An already-assigned instructor must not be able to grow the team.
    [Fact]
    public async Task AssignAsync_ByAnAssignedInstructor_Throws()
    {
        var harness = CreateHarness([Assignment()]);

        await Assert.ThrowsAsync<QuestionAuthorizationException>(
            () => harness.Service.AssignAsync(CourseId, "other@lf.test", InstructorId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task AssignAsync_ByCreator_AddsAssignment()
    {
        var harness = CreateHarness();

        var team = await harness.Service.AssignAsync(CourseId, "other@lf.test", CreatorId, isAdmin: false, Ct);

        Assert.NotNull(team);
        Assert.Contains(team, m => m.UserId == OtherInstructorId);

        var assignment = Assert.Single(harness.Assignments);
        Assert.Equal(CourseId, assignment.CourseId);
        Assert.Equal(OtherInstructorId, assignment.UserId);
        Assert.Equal(CreatorId, assignment.AssignedByUserId);
        Assert.Equal(Now, assignment.AssignedAt);
    }

    [Theory]
    [InlineData("assigned@lf.test")]
    [InlineData("ASSIGNED@LF.TEST")]
    [InlineData("  assigned@lf.test  ")]
    public async Task AssignAsync_MatchesEmailCaseInsensitivelyAndTrimmed(string email)
    {
        var harness = CreateHarness();

        var team = await harness.Service.AssignAsync(CourseId, email, CreatorId, isAdmin: false, Ct);

        Assert.Contains(team!, m => m.UserId == InstructorId);
    }

    [Fact]
    public async Task AssignAsync_UnknownEmail_Throws()
    {
        var harness = CreateHarness();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Service.AssignAsync(CourseId, "nobody@lf.test", CreatorId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task AssignAsync_UserWithoutTeachingRole_Throws()
    {
        var harness = CreateHarness();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Service.AssignAsync(CourseId, "student@lf.test", CreatorId, isAdmin: false, Ct));

        Assert.Empty(harness.Assignments);
    }

    [Fact]
    public async Task AssignAsync_TheCreator_Throws()
    {
        var harness = CreateHarness();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Service.AssignAsync(CourseId, "creator@lf.test", CreatorId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task AssignAsync_AlreadyAssigned_Throws()
    {
        var harness = CreateHarness([Assignment()]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Service.AssignAsync(CourseId, "assigned@lf.test", CreatorId, isAdmin: false, Ct));

        Assert.Single(harness.Assignments);
    }

    [Fact]
    public async Task AssignAsync_BlankEmail_Throws()
    {
        var harness = CreateHarness();

        await Assert.ThrowsAsync<ArgumentException>(
            () => harness.Service.AssignAsync(CourseId, "   ", CreatorId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task AssignAsync_UnknownCourse_ReturnsNull() =>
        Assert.Null(await CreateHarness(courseExists: false).Service.AssignAsync(CourseId, "other@lf.test", CreatorId, isAdmin: false, Ct));

    [Fact]
    public async Task RemoveAsync_ByCreator_DropsAssignment()
    {
        var harness = CreateHarness([Assignment()]);

        var team = await harness.Service.RemoveAsync(CourseId, InstructorId, CreatorId, isAdmin: false, Ct);

        Assert.NotNull(team);
        Assert.DoesNotContain(team, m => m.UserId == InstructorId);
        Assert.Empty(harness.Assignments);
    }

    [Fact]
    public async Task RemoveAsync_NotAssigned_ReturnsNull()
    {
        var harness = CreateHarness();

        Assert.Null(await harness.Service.RemoveAsync(CourseId, InstructorId, CreatorId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task RemoveAsync_SomeoneElsesCourse_Throws()
    {
        var harness = CreateHarness([Assignment()]);

        await Assert.ThrowsAsync<QuestionAuthorizationException>(
            () => harness.Service.RemoveAsync(CourseId, InstructorId, OtherInstructorId, isAdmin: false, Ct));
    }
}
