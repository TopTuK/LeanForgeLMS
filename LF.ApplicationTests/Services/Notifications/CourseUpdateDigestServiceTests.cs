using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Email;
using LF.AppDomain.Entities.User;
using LF.AppDomain.Models.Course.Enums;
using LF.Application.Common.Email;
using LF.Application.Common.Interfaces;
using LF.Application.Common.Options;
using LF.Application.Services.Notifications;
using LF.ApplicationTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using DomainCourse = LF.AppDomain.Entities.Course.Course;
using DomainEnrollment = LF.AppDomain.Entities.Course.Enrollment;

namespace LF.ApplicationTests.Services.Notifications;

public class CourseUpdateDigestServiceTests
{
    private static readonly DateTime Now = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromMinutes(30);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private readonly List<EmailMessage> _queued = [];
    private Mock<IAppDbContext> _dbContextMock = null!;

    private CourseUpdateDigestService CreateService(
        IReadOnlyCollection<DomainCourse> courses,
        IReadOnlyCollection<CourseContentChange> changes,
        IReadOnlyCollection<DomainEnrollment> enrollments,
        IReadOnlyCollection<DbUser> users)
    {
        var emailsMock = new List<EmailMessage>().BuildMockDbSet();
        emailsMock.Setup(s => s.Add(It.IsAny<EmailMessage>())).Callback<EmailMessage>(_queued.Add);

        _dbContextMock = new Mock<IAppDbContext>();
        _dbContextMock.SetupGet(c => c.Courses).Returns(courses.ToList().BuildMockDbSet().Object);
        _dbContextMock.SetupGet(c => c.CourseContentChanges).Returns(changes.ToList().BuildMockDbSet().Object);
        _dbContextMock.SetupGet(c => c.Enrollments).Returns(enrollments.ToList().BuildMockDbSet().Object);
        _dbContextMock.SetupGet(c => c.Users).Returns(users.ToList().BuildMockDbSet().Object);
        _dbContextMock.SetupGet(c => c.EmailMessages).Returns(emailsMock.Object);
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new CourseUpdateDigestService(
            NullLogger<CourseUpdateDigestService>.Instance,
            _dbContextMock.Object,
            new EmailTemplateRenderer(),
            Microsoft.Extensions.Options.Options.Create(new AppUrlOptions { PublicBaseUrl = "https://lms.example.com" }),
            new FixedTimeProvider(Now));
    }

    private static (DomainCourse Course, Lesson First, Lesson Second) CreateCourse(int id = 1, bool published = true)
    {
        var course = DomainCourse.Create("Lean Basics", "Short", "Description", Category.Create("Backend"), createdByUserId: 1, Now.AddDays(-30));
        EntityIdSetter.SetId(course, id);
        var chapter = course.AddChapter("Chapter 1");
        EntityIdSetter.SetId(chapter, id * 10);
        var first = chapter.AddLesson("Value streams");
        EntityIdSetter.SetId(first, id * 100 + 1);
        var second = chapter.AddLesson("Kaizen <basics>");
        EntityIdSetter.SetId(second, id * 100 + 2);
        if (published)
            course.Publish();
        return (course, first, second);
    }

    private static DomainEnrollment Enrollment(int id, int courseId, int userId, EnrollmentStatus status = EnrollmentStatus.Active, DateTime? enrolledAt = null)
    {
        var enrollment = DomainEnrollment.Create(courseId, userId, enrolledAt ?? Now.AddDays(-10), status, status == EnrollmentStatus.Active ? 0m : 1000m);
        EntityIdSetter.SetId(enrollment, id);
        return enrollment;
    }

    private static DbUser User(int id, string email, string firstName, string? language)
    {
        var user = new DbUser { Id = id, Email = email, FirstName = firstName };
        if (language is not null)
            user.SetPreferredLanguage(language);
        return user;
    }

    [Fact]
    public async Task SendDue_QuietCourse_QueuesOneDigestPerStudentInTheirLanguage()
    {
        var (course, first, second) = CreateCourse();
        var added = CourseContentChange.Create(course.Id, second, CourseContentChangeKind.LessonAdded, Now.AddHours(-2));
        var updated = CourseContentChange.Create(course.Id, first, CourseContentChangeKind.LessonUpdated, Now.AddHours(-1));
        var service = CreateService(
            [course], [added, updated],
            [Enrollment(501, course.Id, userId: 7), Enrollment(502, course.Id, userId: 8)],
            [User(7, "ivan@example.com", "Иван", "ru"), User(8, "ann@example.com", "Ann", "en")]);

        var result = await service.SendDueDigestsAsync(QuietPeriod, maxCourses: 10);

        Assert.Equal(new CourseUpdateDigestResult(1, 2), result);
        Assert.Equal(2, _queued.Count);

        var ru = _queued.Single(m => m.ToAddress == "ivan@example.com");
        Assert.Equal("Обновления в курсе «Lean Basics»", ru.Subject);
        Assert.Contains("Новые уроки", ru.HtmlBody);
        Assert.Contains("https://lms.example.com/courses/learn/501", ru.HtmlBody);

        var en = _queued.Single(m => m.ToAddress == "ann@example.com");
        Assert.Equal("“Lean Basics” has been updated", en.Subject);
        Assert.Contains("<li>Kaizen &lt;basics&gt;</li>", en.HtmlBody);
        Assert.Contains("Updated lessons", en.HtmlBody);
        Assert.Contains("- Value streams", en.TextBody);

        Assert.False(added.IsPending);
        Assert.False(updated.IsPending);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendDue_RecentChange_WaitsForQuietPeriod()
    {
        var (course, first, _) = CreateCourse();
        var change = CourseContentChange.Create(course.Id, first, CourseContentChangeKind.LessonUpdated, Now.AddMinutes(-5));
        var service = CreateService([course], [change], [Enrollment(501, course.Id, 7)], [User(7, "ivan@example.com", "Ivan", "en")]);

        var result = await service.SendDueDigestsAsync(QuietPeriod, maxCourses: 10);

        Assert.Equal(0, result.CoursesProcessed);
        Assert.Empty(_queued);
        Assert.True(change.IsPending);
    }

    [Fact]
    public async Task SendDue_OneRecentEditInSession_HoldsTheWholeCourse()
    {
        var (course, first, second) = CreateCourse();
        var old = CourseContentChange.Create(course.Id, first, CourseContentChangeKind.LessonUpdated, Now.AddHours(-3));
        var fresh = CourseContentChange.Create(course.Id, second, CourseContentChangeKind.LessonUpdated, Now.AddMinutes(-1));
        var service = CreateService([course], [old, fresh], [Enrollment(501, course.Id, 7)], [User(7, "ivan@example.com", "Ivan", "en")]);

        await service.SendDueDigestsAsync(QuietPeriod, maxCourses: 10);

        Assert.Empty(_queued);
        Assert.True(old.IsPending);
    }

    [Fact]
    public async Task SendDue_SkipsPendingPaymentAndLateEnrollees()
    {
        var (course, first, _) = CreateCourse();
        var change = CourseContentChange.Create(course.Id, first, CourseContentChangeKind.LessonUpdated, Now.AddHours(-2));
        var service = CreateService(
            [course], [change],
            [
                Enrollment(501, course.Id, 7),
                Enrollment(502, course.Id, 8, EnrollmentStatus.PendingPayment),
                Enrollment(503, course.Id, 9, enrolledAt: Now.AddHours(-1)),
            ],
            [User(7, "a@example.com", "A", "en"), User(8, "b@example.com", "B", "en"), User(9, "c@example.com", "C", "en")]);

        await service.SendDueDigestsAsync(QuietPeriod, maxCourses: 10);

        Assert.Equal("a@example.com", Assert.Single(_queued).ToAddress);
    }

    [Fact]
    public async Task SendDue_UnpublishedCourse_DiscardsChangesWithoutEmail()
    {
        var (course, first, _) = CreateCourse(published: false);
        var change = CourseContentChange.Create(course.Id, first, CourseContentChangeKind.LessonUpdated, Now.AddHours(-2));
        var service = CreateService([course], [change], [Enrollment(501, course.Id, 7)], [User(7, "a@example.com", "A", "en")]);

        var result = await service.SendDueDigestsAsync(QuietPeriod, maxCourses: 10);

        Assert.Equal(new CourseUpdateDigestResult(1, 0), result);
        Assert.Empty(_queued);
        Assert.False(change.IsPending);
    }

    [Fact]
    public async Task SendDue_RespectsMaxCourses()
    {
        var (courseA, lessonA, _) = CreateCourse(id: 1);
        var (courseB, lessonB, _) = CreateCourse(id: 2);
        var changeA = CourseContentChange.Create(courseA.Id, lessonA, CourseContentChangeKind.LessonUpdated, Now.AddHours(-2));
        var changeB = CourseContentChange.Create(courseB.Id, lessonB, CourseContentChangeKind.LessonUpdated, Now.AddHours(-2));
        var service = CreateService([courseA, courseB], [changeA, changeB], [], []);

        var result = await service.SendDueDigestsAsync(QuietPeriod, maxCourses: 1);

        Assert.Equal(1, result.CoursesProcessed);
        Assert.False(changeA.IsPending);
        Assert.True(changeB.IsPending);
    }
}
