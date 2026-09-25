using LF.AppDomain.Entities.Course;
using LF.AppDomain.Models.Course.Enums;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Course;
using LF.Application.Services.Course;
using LF.Application.Services.Notifications;
using LF.ApplicationTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using DomainCourse = LF.AppDomain.Entities.Course.Course;

namespace LF.ApplicationTests.Services.Course;

// Which lesson edits CourseService reports to ICourseChangeTracker (publish gating and merging are
// the tracker's job and are covered in CourseChangeTrackerTests).
public class CourseServiceChangeTrackingTests
{
    private readonly Mock<ICourseChangeTracker> _trackerMock = new();

    private CourseService CreateService(DomainCourse course)
    {
        var dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.Courses).Returns(new List<DomainCourse> { course }.BuildMockDbSet().Object);
        dbContextMock.SetupGet(c => c.StorageObjects).Returns(new List<LF.AppDomain.Entities.Storage.StorageObject>().BuildMockDbSet().Object);
        dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sanitizer = new Mock<IHtmlSanitizer>();
        sanitizer.Setup(s => s.Sanitize(It.IsAny<string?>())).Returns((string? html) => html ?? string.Empty);

        return new CourseService(NullLogger<CourseService>.Instance, dbContextMock.Object, TimeProvider.System, sanitizer.Object,
            Mock.Of<IEnrollmentNotifier>(), _trackerMock.Object);
    }

    private static (DomainCourse Course, Chapter Chapter, Lesson Lesson) CreatePublishedCourse()
    {
        var course = DomainCourse.Create("Course", "Short", "Description", Category.Create("Backend"), createdByUserId: 1, DateTime.UtcNow);
        EntityIdSetter.SetId(course, 1);
        var chapter = course.AddChapter("Chapter 1");
        EntityIdSetter.SetId(chapter, 10);
        var lesson = chapter.AddLesson("Lesson 1", "<p>Body</p>");
        EntityIdSetter.SetId(lesson, 100);
        lesson.ReplaceParts([new LessonPartInput(LessonPartType.Text, "<p>Intro</p>", null)]);
        course.Publish();
        return (course, chapter, lesson);
    }

    private void VerifyTracked(Lesson lesson, CourseContentChangeKind kind) =>
        _trackerMock.Verify(t => t.TrackLessonChangeAsync(It.IsAny<DomainCourse>(), lesson, kind, It.IsAny<CancellationToken>()), Times.Once);

    [Fact]
    public async Task AddLessonAsync_TracksLessonAdded()
    {
        var (course, chapter, _) = CreatePublishedCourse();
        var service = CreateService(course);

        await service.AddLessonAsync(course.Id, chapter.Id, new AddLessonDto { Title = "Lesson 2" }, actingUserId: 1, isAdmin: false);

        var added = chapter.Lessons.Single(l => l.Title == "Lesson 2");
        VerifyTracked(added, CourseContentChangeKind.LessonAdded);
    }

    [Fact]
    public async Task UpdateLessonAsync_TitleChanged_TracksLessonUpdated()
    {
        var (course, chapter, lesson) = CreatePublishedCourse();
        var service = CreateService(course);

        await service.UpdateLessonAsync(course.Id, chapter.Id, lesson.Id,
            new UpdateLessonDto { Title = "Lesson 1 (revised)", Content = "<p>Body</p>" }, actingUserId: 1, isAdmin: false);

        VerifyTracked(lesson, CourseContentChangeKind.LessonUpdated);
    }

    [Fact]
    public async Task UpdateLessonAsync_NothingChanged_DoesNotTrack()
    {
        var (course, chapter, lesson) = CreatePublishedCourse();
        var service = CreateService(course);

        await service.UpdateLessonAsync(course.Id, chapter.Id, lesson.Id,
            new UpdateLessonDto { Title = "Lesson 1", Content = "<p>Body</p>" }, actingUserId: 1, isAdmin: false);

        _trackerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateLessonAsync_OnlyPreviewFlagChanged_DoesNotTrack()
    {
        var (course, chapter, lesson) = CreatePublishedCourse();
        var service = CreateService(course);

        await service.UpdateLessonAsync(course.Id, chapter.Id, lesson.Id,
            new UpdateLessonDto { Title = "Lesson 1", Content = "<p>Body</p>", IncludeInPreview = true }, actingUserId: 1, isAdmin: false);

        Assert.True(lesson.IncludeInPreview);
        _trackerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReplaceLessonPartsAsync_IdenticalParts_DoesNotTrack()
    {
        var (course, chapter, lesson) = CreatePublishedCourse();
        var service = CreateService(course);

        await service.ReplaceLessonPartsAsync(course.Id, chapter.Id, lesson.Id,
            [new ReplaceLessonPartInputDto { PartType = LessonPartType.Text, Html = "<p>Intro</p>" }], actingUserId: 1, isAdmin: false);

        _trackerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReplaceLessonPartsAsync_ChangedParts_TracksLessonUpdated()
    {
        var (course, chapter, lesson) = CreatePublishedCourse();
        var service = CreateService(course);

        await service.ReplaceLessonPartsAsync(course.Id, chapter.Id, lesson.Id,
        [
            new ReplaceLessonPartInputDto { PartType = LessonPartType.Text, Html = "<p>Intro</p>" },
            new ReplaceLessonPartInputDto { PartType = LessonPartType.Text, Html = "<p>New section</p>" },
        ], actingUserId: 1, isAdmin: false);

        VerifyTracked(lesson, CourseContentChangeKind.LessonUpdated);
    }
}
