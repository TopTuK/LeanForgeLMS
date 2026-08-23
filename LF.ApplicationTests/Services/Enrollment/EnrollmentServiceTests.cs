using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.Course.Enums;
using LF.AppDomain.Models.Storage.Enums;
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
        var quizAttemptsMock = new List<QuizAttempt>().BuildMockDbSet();

        dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.Courses).Returns(coursesMock.Object);
        dbContextMock.SetupGet(c => c.Enrollments).Returns(enrollmentsMock.Object);
        dbContextMock.SetupGet(c => c.QuizAttempts).Returns(quizAttemptsMock.Object);
        dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new EnrollmentService(NullLogger<EnrollmentService>.Instance, dbContextMock.Object, TimeProvider.System);
    }

    // Quiz option Ids are DB-generated (0 until persisted); grading matches by Id, so tests need
    // distinct Ids assigned explicitly, same reasoning as CreatePublishedCourse's lesson Ids above.
    private static (DomainCourse Course, int LessonId, int PartId, int CorrectOptionId, int WrongOptionId) CreatePublishedCourseWithQuiz(int passThresholdPercent = 60)
    {
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        EntityIdSetter.SetId(course, 1);

        var chapter = course.AddChapter("Chapter 1");
        var lesson = chapter.AddLesson("Lesson 1");
        EntityIdSetter.SetId(lesson, 1);

        var question = new QuizQuestionInput("Q1", QuestionType.SingleChoice, 1,
            [new QuizOptionInput("Correct", true, 1), new QuizOptionInput("Wrong", false, 2)]);
        lesson.ReplaceParts([new LessonPartInput(LessonPartType.Quiz, null, null, [question], passThresholdPercent)]);

        var quizPart = lesson.Parts[0];
        EntityIdSetter.SetId(quizPart, 1);
        EntityIdSetter.SetId(quizPart.QuizQuestions[0], 1);
        EntityIdSetter.SetId(quizPart.QuizQuestions[0].Options[0], 1);
        EntityIdSetter.SetId(quizPart.QuizQuestions[0].Options[1], 2);

        course.Publish();
        return (course, lesson.Id, quizPart.Id, quizPart.QuizQuestions[0].Options[0].Id, quizPart.QuizQuestions[0].Options[1].Id);
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
    public async Task EnrollAsync_OwnCourse_ThrowsSelfEnrollmentException()
    {
        // Arrange
        var course = CreatePublishedCourse(createdByUserId: 1);
        var service = CreateService([course], [], out var dbContextMock);

        // Act & Assert
        await Assert.ThrowsAsync<SelfEnrollmentException>(() => service.EnrollAsync(course.Id, actingUserId: 1));
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnrollAsync_OtherUsersPublishedCourse_Succeeds()
    {
        // Arrange
        var course = CreatePublishedCourse(createdByUserId: 1);
        var service = CreateService([course], [], out var dbContextMock);

        // Act
        var result = await service.EnrollAsync(course.Id, actingUserId: 7);

        // Assert
        Assert.Equal(course.Id, result.CourseId);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task CompleteLessonAsync_LessonHasQuizPart_ThrowsQuizCompletionRequiredException()
    {
        // Arrange
        var (course, lessonId, _, _, _) = CreatePublishedCourseWithQuiz();
        var enrollment = DomainEnrollment.Create(course.Id, userId: 7, DateTime.UtcNow);
        var service = CreateService([course], [enrollment], out var dbContextMock);

        // Act & Assert
        await Assert.ThrowsAsync<QuizCompletionRequiredException>(
            () => service.CompleteLessonAsync(enrollment.Id, lessonId, actingUserId: 7, isAdmin: false));
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitQuizAttemptAsync_PassingScore_CompletesLessonAndReturnsResult()
    {
        // Arrange
        var (course, lessonId, partId, correctOptionId, _) = CreatePublishedCourseWithQuiz(passThresholdPercent: 60);
        var enrollment = DomainEnrollment.Create(course.Id, userId: 7, DateTime.UtcNow);
        var service = CreateService([course], [enrollment], out var dbContextMock);
        var answers = new List<QuizAnswerInputDto> { new() { QuestionId = 1, SelectedOptionIds = [correctOptionId] } };

        // Act
        var submission = await service.SubmitQuizAttemptAsync(enrollment.Id, lessonId, partId, answers, actingUserId: 7, isAdmin: false);

        // Assert
        Assert.NotNull(submission);
        Assert.Equal(100, submission!.Result.ScorePercent);
        Assert.True(submission.Result.Passed);
        Assert.True(submission.Enrollment.Chapters[0].Lessons[0].IsCompleted);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitQuizAttemptAsync_FailingScore_DoesNotCompleteLesson()
    {
        // Arrange
        var (course, lessonId, partId, _, wrongOptionId) = CreatePublishedCourseWithQuiz(passThresholdPercent: 60);
        var enrollment = DomainEnrollment.Create(course.Id, userId: 7, DateTime.UtcNow);
        var service = CreateService([course], [enrollment], out _);
        var answers = new List<QuizAnswerInputDto> { new() { QuestionId = 1, SelectedOptionIds = [wrongOptionId] } };

        // Act
        var submission = await service.SubmitQuizAttemptAsync(enrollment.Id, lessonId, partId, answers, actingUserId: 7, isAdmin: false);

        // Assert
        Assert.NotNull(submission);
        Assert.Equal(0, submission!.Result.ScorePercent);
        Assert.False(submission.Result.Passed);
        Assert.False(submission.Enrollment.Chapters[0].Lessons[0].IsCompleted);
    }

    [Fact]
    public async Task SubmitQuizAttemptAsync_NonOwnerNonAdmin_ThrowsEnrollmentAuthorizationException()
    {
        // Arrange
        var (course, lessonId, partId, correctOptionId, _) = CreatePublishedCourseWithQuiz();
        var enrollment = DomainEnrollment.Create(course.Id, userId: 7, DateTime.UtcNow);
        var service = CreateService([course], [enrollment], out _);
        var answers = new List<QuizAnswerInputDto> { new() { QuestionId = 1, SelectedOptionIds = [correctOptionId] } };

        // Act & Assert
        await Assert.ThrowsAsync<EnrollmentAuthorizationException>(
            () => service.SubmitQuizAttemptAsync(enrollment.Id, lessonId, partId, answers, actingUserId: 99, isAdmin: false));
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

    [Fact]
    public async Task BrowseCatalogAsync_ExcludesActingUsersOwnCourses()
    {
        // Arrange
        var ownCourse = CreatePublishedCourse(id: 1, createdByUserId: 7);
        var otherCourse = CreatePublishedCourse(id: 2, createdByUserId: 1);
        var service = CreateService([ownCourse, otherCourse], [], out _);

        // Act
        var result = await service.BrowseCatalogAsync(page: 1, pageSize: 20, actingUserId: 7);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(otherCourse.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task BrowseCatalogAsync_ImageCover_PropagatesCoverFields()
    {
        // Arrange
        var storageObject = StorageObject.Create(StorageObjectType.Image, "images/a.png", "image/png", 100, 1, DateTime.UtcNow);
        var course = CreatePublishedCourse();
        course.SetImageCover(storageObject);
        var service = CreateService([course], [], out _);

        // Act
        var result = await service.BrowseCatalogAsync(page: 1, pageSize: 20, actingUserId: 7);

        // Assert
        Assert.Equal(CourseCoverType.Image, result.Items[0].CoverType);
        Assert.Equal("images/a.png", result.Items[0].CoverImageKey);
        Assert.Equal("image/png", result.Items[0].CoverImageContentType);
    }

    [Fact]
    public async Task GetCourseCoverAsync_PublishedCourseWithImageCover_ReturnsCover()
    {
        // Arrange
        var storageObject = StorageObject.Create(StorageObjectType.Image, "images/a.png", "image/png", 100, 1, DateTime.UtcNow);
        var course = CreatePublishedCourse();
        course.SetImageCover(storageObject);
        var service = CreateService([course], [], out _);

        // Act
        var result = await service.GetCourseCoverAsync(course.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("images/a.png", result!.CoverImageKey);
        Assert.Equal("image/png", result.CoverImageContentType);
    }

    [Fact]
    public async Task GetCourseCoverAsync_UnpublishedCourse_ReturnsNull()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var storageObject = StorageObject.Create(StorageObjectType.Image, "images/a.png", "image/png", 100, 1, DateTime.UtcNow);
        course.SetImageCover(storageObject);
        var service = CreateService([course], [], out _);

        // Act
        var result = await service.GetCourseCoverAsync(course.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCourseCoverAsync_ColorCover_ReturnsNull()
    {
        // Arrange
        var course = CreatePublishedCourse();
        course.SetColorCover(CourseCoverColor.Ocean);
        var service = CreateService([course], [], out _);

        // Act
        var result = await service.GetCourseCoverAsync(course.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCourseCoverAsync_CourseNotFound_ReturnsNull()
    {
        // Arrange
        var service = CreateService([], [], out _);

        // Act
        var result = await service.GetCourseCoverAsync(courseId: 999);

        // Assert
        Assert.Null(result);
    }
}
