using LF.AppDomain.Entities.Course;
using LF.AppDomain.Models.Course.Enums;
using LF.Application.Common.Interfaces;
using LF.Application.Services.Notifications;
using LF.ApplicationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using DomainCourse = LF.AppDomain.Entities.Course.Course;

namespace LF.ApplicationTests.Services.Notifications;

public class CourseChangeTrackerTests
{
    private static CourseChangeTracker CreateTracker(
        List<CourseContentChange> changes,
        out Mock<IAppDbContext> dbContextMock,
        out Mock<DbSet<CourseContentChange>> changesMock)
    {
        changesMock = changes.BuildMockDbSet();
        dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.CourseContentChanges).Returns(changesMock.Object);

        return new CourseChangeTracker(NullLogger<CourseChangeTracker>.Instance, dbContextMock.Object, TimeProvider.System);
    }

    private static (DomainCourse Course, Lesson Lesson) CreateCourse(bool published, int lessonId = 100)
    {
        var course = DomainCourse.Create("Course", "Short", "Description", Category.Create("Backend"), createdByUserId: 1, DateTime.UtcNow);
        EntityIdSetter.SetId(course, 1);
        var lesson = course.AddChapter("Chapter 1").AddLesson("Lesson 1");
        EntityIdSetter.SetId(lesson, lessonId);
        if (published)
            course.Publish();
        return (course, lesson);
    }

    [Fact]
    public async Task Track_UnpublishedCourse_DoesNothing()
    {
        var (course, lesson) = CreateCourse(published: false);
        var tracker = CreateTracker([], out var dbContextMock, out var changesMock);

        await tracker.TrackLessonChangeAsync(course, lesson, CourseContentChangeKind.LessonUpdated);

        changesMock.Verify(s => s.Add(It.IsAny<CourseContentChange>()), Times.Never);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Track_NoPendingChange_StagesNewRowWithoutSaving()
    {
        var (course, lesson) = CreateCourse(published: true);
        var tracker = CreateTracker([], out var dbContextMock, out var changesMock);

        await tracker.TrackLessonChangeAsync(course, lesson, CourseContentChangeKind.LessonUpdated);

        changesMock.Verify(s => s.Add(It.Is<CourseContentChange>(c =>
            c.CourseId == 1 && c.Lesson == lesson && c.Kind == CourseContentChangeKind.LessonUpdated)), Times.Once);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Track_PendingChangeExists_MergesIntoIt()
    {
        var (course, lesson) = CreateCourse(published: true);
        var pending = CourseContentChange.Create(1, lesson, CourseContentChangeKind.LessonAdded, DateTime.UtcNow.AddMinutes(-10));
        var tracker = CreateTracker([pending], out _, out var changesMock);

        await tracker.TrackLessonChangeAsync(course, lesson, CourseContentChangeKind.LessonUpdated);

        changesMock.Verify(s => s.Add(It.IsAny<CourseContentChange>()), Times.Never);
        Assert.Equal(CourseContentChangeKind.LessonAdded, pending.Kind);
        Assert.True(pending.LastChangedAt > pending.FirstChangedAt);
    }

    [Fact]
    public async Task Track_OnlyAnnouncedChangeExists_StagesNewRow()
    {
        var (course, lesson) = CreateCourse(published: true);
        var announced = CourseContentChange.Create(1, lesson, CourseContentChangeKind.LessonAdded, DateTime.UtcNow.AddDays(-1));
        announced.MarkNotified(DateTime.UtcNow.AddHours(-20));
        var tracker = CreateTracker([announced], out _, out var changesMock);

        await tracker.TrackLessonChangeAsync(course, lesson, CourseContentChangeKind.LessonUpdated);

        changesMock.Verify(s => s.Add(It.IsAny<CourseContentChange>()), Times.Once);
    }
}
