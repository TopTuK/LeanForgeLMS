using LF.AppDomain.Entities.Course;
using LF.ApplicationTests.TestSupport;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Enrollment;
using LF.Application.Services.Enrollment;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using DomainCourse = LF.AppDomain.Entities.Course.Course;
using DomainEnrollment = LF.AppDomain.Entities.Course.Enrollment;

namespace LF.ApplicationTests.Services.Enrollment;

public class EnrollmentServiceTests
{
    private static EnrollmentService CreateService(
        IReadOnlyCollection<DomainCourse> courses,
        IReadOnlyCollection<DomainEnrollment> enrollments,
        out Mock<IAppDbContext> dbContextMock)
    {
        var coursesMock = courses.ToList().BuildMockDbSet();
        var enrollmentsMock = enrollments.ToList().BuildMockDbSet();

        dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.Courses).Returns(coursesMock.Object);
        dbContextMock.SetupGet(c => c.Enrollments).Returns(enrollmentsMock.Object);
        dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new EnrollmentService(NullLogger<EnrollmentService>.Instance, dbContextMock.Object, TimeProvider.System);
    }

    // Ids are DB-generated (0 until persisted); tests that mix multiple courses/lessons need
    // distinct Ids assigned explicitly so Id-based lookups (dictionaries, Single()) don't collide.
    private static DomainCourse CreatePublishedCourse(int id = 1, int createdByUserId = 1, int lessonCount = 1)
    {
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId, DateTime.UtcNow);
        EntityIdSetter.SetId(course, id);

        var chapter = course.AddChapter("Chapter 1");
        for (var i = 0; i < lessonCount; i++)
        {
            var lesson = chapter.AddLesson($"Lesson {i + 1}");
            EntityIdSetter.SetId(lesson, i + 1);
        }

        course.Publish();
        return course;
    }

    [Fact]
    public async Task EnrollAsync_PublishedCourseNotEnrolled_CreatesEnrollment()
    {
        // Arrange
        var course = CreatePublishedCourse();
        var service = CreateService([course], [], out var dbContextMock);

        // Act
        var result = await service.EnrollAsync(course.Id, actingUserId: 7);

        // Assert
        Assert.Equal(course.Id, result.CourseId);
        Assert.Null(result.CompletedAt);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnrollAsync_UnpublishedCourse_Throws()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var service = CreateService([course], [], out var dbContextMock);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.EnrollAsync(course.Id, actingUserId: 7));
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnrollAsync_AlreadyEnrolled_Throws()
    {
        // Arrange
        var course = CreatePublishedCourse();
        var existingEnrollment = DomainEnrollment.Create(course.Id, userId: 7, DateTime.UtcNow);
        var service = CreateService([course], [existingEnrollment], out var dbContextMock);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.EnrollAsync(course.Id, actingUserId: 7));
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnrollAsync_CourseNotFound_Throws()
    {
        // Arrange
        var service = CreateService([], [], out _);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.EnrollAsync(courseId: 999, actingUserId: 7));
    }

    [Fact]
    public async Task CompleteLessonAsync_Owner_MarksOnlyThatLessonComplete()
    {
        // Arrange
        var course = CreatePublishedCourse(lessonCount: 2);
        var firstLessonId = course.Chapters[0].Lessons[0].Id;
        var secondLessonId = course.Chapters[0].Lessons[1].Id;
        var enrollment = DomainEnrollment.Create(course.Id, userId: 7, DateTime.UtcNow);
        var service = CreateService([course], [enrollment], out var dbContextMock);

        // Act
        var result = await service.CompleteLessonAsync(enrollment.Id, firstLessonId, actingUserId: 7, isAdmin: false);

        // Assert
        Assert.NotNull(result);
        Assert.True(result!.Chapters[0].Lessons.Single(l => l.Id == firstLessonId).IsCompleted);
        Assert.False(result.Chapters[0].Lessons.Single(l => l.Id == secondLessonId).IsCompleted);
        Assert.Null(result.CompletedAt);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteLessonAsync_LastLesson_SetsCompletedAt()
    {
        // Arrange
        var course = CreatePublishedCourse(lessonCount: 1);
        var lessonId = course.Chapters[0].Lessons[0].Id;
        var enrollment = DomainEnrollment.Create(course.Id, userId: 7, DateTime.UtcNow);
        var service = CreateService([course], [enrollment], out _);

        // Act
        var result = await service.CompleteLessonAsync(enrollment.Id, lessonId, actingUserId: 7, isAdmin: false);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result!.CompletedAt);
    }

    [Fact]
    public async Task CompleteLessonAsync_AlreadyCompleted_DoesNotSaveAgain()
    {
        // Arrange
        var course = CreatePublishedCourse(lessonCount: 1);
        var lessonId = course.Chapters[0].Lessons[0].Id;
        var enrollment = DomainEnrollment.Create(course.Id, userId: 7, DateTime.UtcNow);
        enrollment.CompleteLesson(lessonId);
        var service = CreateService([course], [enrollment], out var dbContextMock);

        // Act
        var result = await service.CompleteLessonAsync(enrollment.Id, lessonId, actingUserId: 7, isAdmin: false);

        // Assert
        Assert.NotNull(result);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteLessonAsync_LessonNotOnCourse_Throws()
    {
        // Arrange
        var course = CreatePublishedCourse(lessonCount: 1);
        var enrollment = DomainEnrollment.Create(course.Id, userId: 7, DateTime.UtcNow);
        var service = CreateService([course], [enrollment], out var dbContextMock);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CompleteLessonAsync(enrollment.Id, lessonId: 999, actingUserId: 7, isAdmin: false));
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteLessonAsync_NonOwnerNonAdmin_ThrowsEnrollmentAuthorizationException()
    {
        // Arrange
        var course = CreatePublishedCourse();
        var lessonId = course.Chapters[0].Lessons[0].Id;
        var enrollment = DomainEnrollment.Create(course.Id, userId: 7, DateTime.UtcNow);
        var service = CreateService([course], [enrollment], out var dbContextMock);

        // Act & Assert
        await Assert.ThrowsAsync<EnrollmentAuthorizationException>(
            () => service.CompleteLessonAsync(enrollment.Id, lessonId, actingUserId: 99, isAdmin: false));
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteLessonAsync_Admin_SucceedsOnAnyEnrollment()
    {
        // Arrange
        var course = CreatePublishedCourse();
        var lessonId = course.Chapters[0].Lessons[0].Id;
        var enrollment = DomainEnrollment.Create(course.Id, userId: 7, DateTime.UtcNow);
        var service = CreateService([course], [enrollment], out _);

        // Act
        var result = await service.CompleteLessonAsync(enrollment.Id, lessonId, actingUserId: 99, isAdmin: true);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetEnrollmentAsync_NonOwnerNonAdmin_ThrowsEnrollmentAuthorizationException()
    {
        // Arrange
        var course = CreatePublishedCourse();
        var enrollment = DomainEnrollment.Create(course.Id, userId: 7, DateTime.UtcNow);
        var service = CreateService([course], [enrollment], out _);

        // Act & Assert
        await Assert.ThrowsAsync<EnrollmentAuthorizationException>(
            () => service.GetEnrollmentAsync(enrollment.Id, actingUserId: 99, isAdmin: false));
    }

    [Fact]
    public async Task ListMyEnrollmentsAsync_FiltersByActiveStatus()
    {
        // Arrange
        var activeCourse = CreatePublishedCourse(id: 1, lessonCount: 2);
        var completedCourse = CreatePublishedCourse(id: 2, lessonCount: 1);
        var activeEnrollment = DomainEnrollment.Create(activeCourse.Id, userId: 7, DateTime.UtcNow);
        var completedEnrollment = DomainEnrollment.Create(completedCourse.Id, userId: 7, DateTime.UtcNow);
        completedEnrollment.CompleteLesson(completedCourse.Chapters[0].Lessons[0].Id);
        completedEnrollment.RecalculateCompletion(1, DateTime.UtcNow);
        var service = CreateService([activeCourse, completedCourse], [activeEnrollment, completedEnrollment], out _);

        // Act
        var result = await service.ListMyEnrollmentsAsync(actingUserId: 7, EnrollmentStatusFilter.Active);

        // Assert
        Assert.Single(result);
        Assert.Equal(activeCourse.Id, result[0].CourseId);
    }

    [Fact]
    public async Task ListMyEnrollmentsAsync_FiltersByCompletedStatus()
    {
        // Arrange
        var activeCourse = CreatePublishedCourse(id: 1, lessonCount: 2);
        var completedCourse = CreatePublishedCourse(id: 2, lessonCount: 1);
        var activeEnrollment = DomainEnrollment.Create(activeCourse.Id, userId: 7, DateTime.UtcNow);
        var completedEnrollment = DomainEnrollment.Create(completedCourse.Id, userId: 7, DateTime.UtcNow);
        completedEnrollment.CompleteLesson(completedCourse.Chapters[0].Lessons[0].Id);
        completedEnrollment.RecalculateCompletion(1, DateTime.UtcNow);
        var service = CreateService([activeCourse, completedCourse], [activeEnrollment, completedEnrollment], out _);

        // Act
        var result = await service.ListMyEnrollmentsAsync(actingUserId: 7, EnrollmentStatusFilter.Completed);

        // Assert
        Assert.Single(result);
        Assert.Equal(completedCourse.Id, result[0].CourseId);
        Assert.Equal(100, result[0].ProgressPercent);
    }

    [Fact]
    public async Task BrowseCatalogAsync_ExcludesEnrolledAndUnpublishedCourses()
    {
        // Arrange
        var availableCourse = CreatePublishedCourse(id: 1);
        var enrolledCourse = CreatePublishedCourse(id: 2);
        var category = Category.Create("Frontend");
        var unpublishedCourse = DomainCourse.Create("Draft", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        EntityIdSetter.SetId(unpublishedCourse, 3);
        var enrollment = DomainEnrollment.Create(enrolledCourse.Id, userId: 7, DateTime.UtcNow);
        var service = CreateService([availableCourse, enrolledCourse, unpublishedCourse], [enrollment], out _);

        // Act
        var result = await service.BrowseCatalogAsync(page: 1, pageSize: 20, actingUserId: 7);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(availableCourse.Id, result.Items[0].Id);
    }
}
