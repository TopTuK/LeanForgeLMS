using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.Course.Enums;
using LF.AppDomain.Models.Storage.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Course;
using LF.Application.Services.Course;
using LF.ApplicationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using DomainCourse = LF.AppDomain.Entities.Course.Course;
using DomainEnrollment = LF.AppDomain.Entities.Course.Enrollment;

namespace LF.ApplicationTests.Services.Course;

public class CourseServiceTests
{
    // A no-op sanitizer that records its calls; the real allow-list behaviour is covered by GanssHtmlSanitizerTests.
    private static Mock<IHtmlSanitizer> CreateSanitizerMock()
    {
        var mock = new Mock<IHtmlSanitizer>();
        mock.Setup(s => s.Sanitize(It.IsAny<string?>())).Returns((string? html) => html ?? string.Empty);
        return mock;
    }

    private static CourseService CreateService(
        IReadOnlyCollection<DomainCourse> courses,
        IReadOnlyCollection<Category> categories,
        out Mock<IAppDbContext> dbContextMock) =>
        CreateService(courses, categories, [], out dbContextMock);

    private static CourseService CreateService(
        IReadOnlyCollection<DomainCourse> courses,
        IReadOnlyCollection<Category> categories,
        IReadOnlyCollection<StorageObject> storageObjects,
        out Mock<IAppDbContext> dbContextMock)
    {
        var coursesMock = courses.ToList().BuildMockDbSet();
        var categoriesMock = categories.ToList().BuildMockDbSet();
        var storageObjectsMock = storageObjects.ToList().BuildMockDbSet();

        dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.Courses).Returns(coursesMock.Object);
        dbContextMock.SetupGet(c => c.Categories).Returns(categoriesMock.Object);
        dbContextMock.SetupGet(c => c.StorageObjects).Returns(storageObjectsMock.Object);
        dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new CourseService(NullLogger<CourseService>.Instance, dbContextMock.Object, TimeProvider.System, CreateSanitizerMock().Object);
    }

    [Fact]
    public async Task CreateCourseAsync_ValidCategory_CreatesCourse()
    {
        // Arrange
        var category = Category.Create("Backend");
        var service = CreateService([], [category], out var dbContextMock);
        var dto = new CreateCourseDto { Title = "Title", ShortIntroduction = "Short", Description = "Description", CategoryId = category.Id };

        // Act
        var result = await service.CreateCourseAsync(dto, createdByUserId: 1);

        // Assert
        Assert.Equal("Title", result.Title);
        Assert.Equal(1, result.CreatedByUserId);
        Assert.False(result.IsPublished);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCourseAsync_PaidCourse_StoresPriceAndMode()
    {
        var category = Category.Create("Backend");
        var service = CreateService([], [category], out _);
        var dto = new CreateCourseDto
        {
            Title = "Title",
            ShortIntroduction = "Short",
            Description = "Description",
            CategoryId = category.Id,
            PricingType = CoursePricingType.Paid,
            Price = 1990m,
            EnrollmentMode = CourseEnrollmentMode.Managed,
        };

        var result = await service.CreateCourseAsync(dto, createdByUserId: 1);

        Assert.Equal(CoursePricingType.Paid, result.PricingType);
        Assert.Equal(1990m, result.Price);
        Assert.Equal(CourseEnrollmentMode.Managed, result.EnrollmentMode);
    }

    [Fact]
    public async Task CreateCourseAsync_PaidWithoutPrice_ThrowsArgumentException()
    {
        var category = Category.Create("Backend");
        var service = CreateService([], [category], out _);
        var dto = new CreateCourseDto
        {
            Title = "Title",
            ShortIntroduction = "Short",
            Description = "Description",
            CategoryId = category.Id,
            PricingType = CoursePricingType.Paid,
            Price = null,
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCourseAsync(dto, createdByUserId: 1));
    }

    private static CourseService CreateServiceWithEnrollments(
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

        return new CourseService(NullLogger<CourseService>.Instance, dbContextMock.Object, TimeProvider.System, CreateSanitizerMock().Object);
    }

    private static DomainCourse CreatePublishedManagedCourse(int id = 1, int ownerId = 1)
    {
        var course = DomainCourse.Create("Managed", "Short", "Description", Category.Create("Backend"), ownerId, DateTime.UtcNow,
            CoursePricingType.Free, null, CourseEnrollmentMode.Managed);
        EntityIdSetter.SetId(course, id);
        var chapter = course.AddChapter("Chapter 1");
        EntityIdSetter.SetId(chapter.AddLesson("Lesson 1"), 1);
        course.Publish();
        return course;
    }

    [Fact]
    public async Task EnrollUserAsync_ByOwner_CreatesActiveEnrollment()
    {
        var course = CreatePublishedManagedCourse(id: 1, ownerId: 1);
        var service = CreateServiceWithEnrollments([course], [], out var dbContextMock);

        var result = await service.EnrollUserAsync(course.Id, targetUserId: 7, actingUserId: 1, isAdmin: false);

        Assert.NotNull(result);
        Assert.Equal(EnrollmentStatus.Active, result!.Status);
        Assert.Equal(0m, result.PricePaid);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnrollUserAsync_ByNonOwnerNonAdmin_ThrowsAuthorization()
    {
        var course = CreatePublishedManagedCourse(id: 1, ownerId: 1);
        var service = CreateServiceWithEnrollments([course], [], out _);

        await Assert.ThrowsAsync<CourseAuthorizationException>(
            () => service.EnrollUserAsync(course.Id, targetUserId: 7, actingUserId: 99, isAdmin: false));
    }

    [Fact]
    public async Task EnrollUserAsync_MissingCourse_ReturnsNull()
    {
        var service = CreateServiceWithEnrollments([], [], out _);

        Assert.Null(await service.EnrollUserAsync(courseId: 42, targetUserId: 7, actingUserId: 1, isAdmin: true));
    }

    [Fact]
    public async Task CreateCourseAsync_UnknownCategory_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService([], [], out _);
        var dto = new CreateCourseDto { Title = "Title", ShortIntroduction = "Short", Description = "Description", CategoryId = 999 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCourseAsync(dto, createdByUserId: 1));
    }

    [Fact]
    public async Task CreateCourseAsync_ColorCover_SetsColorCover()
    {
        // Arrange
        var category = Category.Create("Backend");
        var service = CreateService([], [category], out _);
        var dto = new CreateCourseDto
        {
            Title = "Title",
            ShortIntroduction = "Short",
            Description = "Description",
            CategoryId = category.Id,
            CoverType = CourseCoverType.Color,
            CoverColor = CourseCoverColor.Ocean,
        };

        // Act
        var result = await service.CreateCourseAsync(dto, createdByUserId: 1);

        // Assert
        Assert.Equal(CourseCoverType.Color, result.CoverType);
        Assert.Equal(CourseCoverColor.Ocean, result.CoverColor);
        Assert.Null(result.CoverImageKey);
    }

    [Fact]
    public async Task CreateCourseAsync_ValidImageCover_SetsImageCover()
    {
        // Arrange
        var category = Category.Create("Backend");
        var storageObject = StorageObject.Create(StorageObjectType.Image, "images/a.png", "image/png", 100, 1, DateTime.UtcNow);
        var service = CreateService([], [category], [storageObject], out _);
        var dto = new CreateCourseDto
        {
            Title = "Title",
            ShortIntroduction = "Short",
            Description = "Description",
            CategoryId = category.Id,
            CoverType = CourseCoverType.Image,
            CoverImageStorageObjectId = storageObject.Id,
        };

        // Act
        var result = await service.CreateCourseAsync(dto, createdByUserId: 1);

        // Assert
        Assert.Equal(CourseCoverType.Image, result.CoverType);
        Assert.Equal("images/a.png", result.CoverImageKey);
        Assert.Equal("image/png", result.CoverImageContentType);
    }

    [Fact]
    public async Task CreateCourseAsync_UnknownImageCover_ThrowsArgumentException()
    {
        // Arrange
        var category = Category.Create("Backend");
        var service = CreateService([], [category], out _);
        var dto = new CreateCourseDto
        {
            Title = "Title",
            ShortIntroduction = "Short",
            Description = "Description",
            CategoryId = category.Id,
            CoverType = CourseCoverType.Image,
            CoverImageStorageObjectId = 999,
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCourseAsync(dto, createdByUserId: 1));
    }

    [Fact]
    public async Task CreateCategoryAsync_NewName_CreatesCategory()
    {
        // Arrange
        var service = CreateService([], [], out var dbContextMock);

        // Act
        var result = await service.CreateCategoryAsync("Backend");

        // Assert
        Assert.Equal("Backend", result.Name);
        Assert.False(result.IsDefault);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCategoryAsync_DuplicateName_ThrowsArgumentException()
    {
        // Arrange
        var category = Category.Create("Backend");
        var service = CreateService([], [category], out var dbContextMock);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCategoryAsync("backend"));
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategoryAsync_NotFound_ReturnsFalse()
    {
        // Arrange
        var service = CreateService([], [], out var dbContextMock);

        // Act
        var result = await service.DeleteCategoryAsync(999);

        // Assert
        Assert.False(result);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategoryAsync_DefaultCategory_ThrowsCategoryProtectedException()
    {
        // Arrange
        var category = Category.Create("Common", isDefault: true);
        var service = CreateService([], [category], out var dbContextMock);

        // Act & Assert
        await Assert.ThrowsAsync<CategoryProtectedException>(() => service.DeleteCategoryAsync(category.Id));
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategoryAsync_CategoryInUse_ThrowsInvalidOperationException()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var service = CreateService([course], [category], out var dbContextMock);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteCategoryAsync(category.Id));
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategoryAsync_UnusedNonDefaultCategory_RemovesCategory()
    {
        // Arrange
        var category = Category.Create("Backend");
        var service = CreateService([], [category], out var dbContextMock);

        // Act
        var result = await service.DeleteCategoryAsync(category.Id);

        // Assert
        Assert.True(result);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddChapterAsync_Owner_Succeeds()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var service = CreateService([course], [category], out var dbContextMock);

        // Act
        var result = await service.AddChapterAsync(course.Id, "Chapter 1", actingUserId: 1, isAdmin: false);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result!.Chapters);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddChapterAsync_NonOwnerNonAdmin_ThrowsCourseAuthorizationException()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var service = CreateService([course], [category], out var dbContextMock);

        // Act & Assert
        await Assert.ThrowsAsync<CourseAuthorizationException>(
            () => service.AddChapterAsync(course.Id, "Chapter 1", actingUserId: 2, isAdmin: false));
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddChapterAsync_Admin_SucceedsOnAnyCourse()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var service = CreateService([course], [category], out _);

        // Act
        var result = await service.AddChapterAsync(course.Id, "Chapter 1", actingUserId: 999, isAdmin: true);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result!.Chapters);
    }

    [Fact]
    public async Task AddChapterAsync_CourseNotFound_ReturnsNull()
    {
        // Arrange
        var service = CreateService([], [], out var dbContextMock);

        // Act
        var result = await service.AddChapterAsync(999, "Chapter 1", actingUserId: 1, isAdmin: false);

        // Assert
        Assert.Null(result);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishCourseAsync_NoChapters_ThrowsInvalidOperationException()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var service = CreateService([course], [category], out _);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PublishCourseAsync(course.Id, actingUserId: 1, isAdmin: false));
    }

    [Fact]
    public async Task ListCoursesAsync_Admin_ReturnsAll()
    {
        // Arrange
        var category = Category.Create("Backend");
        var courses = new List<DomainCourse>
        {
            DomainCourse.Create("Course 1", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow),
            DomainCourse.Create("Course 2", "Short", "Description", category, createdByUserId: 2, DateTime.UtcNow),
        };
        var service = CreateService(courses, [category], out _);

        // Act
        var result = await service.ListCoursesAsync(page: 1, pageSize: 10, actingUserId: 1, isAdmin: true);

        // Assert
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task ListCoursesAsync_CourseCreator_ReturnsOwnedOnly()
    {
        // Arrange
        var category = Category.Create("Backend");
        var courses = new List<DomainCourse>
        {
            DomainCourse.Create("Course 1", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow),
            DomainCourse.Create("Course 2", "Short", "Description", category, createdByUserId: 2, DateTime.UtcNow),
        };
        var service = CreateService(courses, [category], out _);

        // Act
        var result = await service.ListCoursesAsync(page: 1, pageSize: 10, actingUserId: 1, isAdmin: false);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Course 1", result.Items[0].Title);
    }

    [Fact]
    public async Task MoveLessonAsync_UnknownLesson_ReturnsNull()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        chapter.AddLesson("Lesson 1");
        var service = CreateService([course], [category], out var dbContextMock);

        // Act
        var result = await service.MoveLessonAsync(course.Id, chapter.Id, lessonId: 999, MoveDirection.Up, actingUserId: 1, isAdmin: false);

        // Assert
        Assert.Null(result);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveLessonAsync_RemovesAndReturnsUpdatedDetail()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        var lesson = chapter.AddLesson("Lesson 1");
        var service = CreateService([course], [category], out var dbContextMock);

        // Act
        var result = await service.RemoveLessonAsync(course.Id, chapter.Id, lesson.Id, actingUserId: 1, isAdmin: false);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result!.Chapters.Single().Lessons);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceLessonPartsAsync_Owner_ReplacesAllParts()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        var lesson = chapter.AddLesson("Lesson 1");
        var storageObject = StorageObject.Create(StorageObjectType.Image, "images/a.png", "image/png", 100, 1, DateTime.UtcNow);
        var service = CreateService([course], [category], [storageObject], out var dbContextMock);
        var parts = new List<ReplaceLessonPartInputDto>
        {
            new() { PartType = LessonPartType.Text, Html = "<p>Intro</p>" },
            new() { PartType = LessonPartType.Image, StorageObjectId = storageObject.Id },
        };

        // Act
        var result = await service.ReplaceLessonPartsAsync(course.Id, chapter.Id, lesson.Id, parts, actingUserId: 1, isAdmin: false);

        // Assert
        Assert.NotNull(result);
        var resultParts = result!.Chapters.Single().Lessons.Single().Parts;
        Assert.Equal(2, resultParts.Count);
        Assert.Equal(LessonPartType.Text, resultParts[0].PartType);
        Assert.Equal(LessonPartType.Image, resultParts[1].PartType);
        Assert.Equal("images/a.png", resultParts[1].StorageObjectKey);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceLessonPartsAsync_NotOwnerNotAdmin_ThrowsCourseAuthorizationException()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        var lesson = chapter.AddLesson("Lesson 1");
        var service = CreateService([course], [category], out var dbContextMock);
        var parts = new List<ReplaceLessonPartInputDto> { new() { PartType = LessonPartType.Text, Html = "<p>Intro</p>" } };

        // Act & Assert
        await Assert.ThrowsAsync<CourseAuthorizationException>(
            () => service.ReplaceLessonPartsAsync(course.Id, chapter.Id, lesson.Id, parts, actingUserId: 2, isAdmin: false));
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReplaceLessonPartsAsync_Admin_Succeeds()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        var lesson = chapter.AddLesson("Lesson 1");
        var service = CreateService([course], [category], out _);
        var parts = new List<ReplaceLessonPartInputDto> { new() { PartType = LessonPartType.Text, Html = "<p>Intro</p>" } };

        // Act
        var result = await service.ReplaceLessonPartsAsync(course.Id, chapter.Id, lesson.Id, parts, actingUserId: 999, isAdmin: true);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result!.Chapters.Single().Lessons.Single().Parts);
    }

    [Fact]
    public async Task ReplaceLessonPartsAsync_CourseNotFound_ReturnsNull()
    {
        // Arrange
        var service = CreateService([], [], out var dbContextMock);
        var parts = new List<ReplaceLessonPartInputDto> { new() { PartType = LessonPartType.Text, Html = "<p>Intro</p>" } };

        // Act
        var result = await service.ReplaceLessonPartsAsync(999, 1, 1, parts, actingUserId: 1, isAdmin: false);

        // Assert
        Assert.Null(result);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReplaceLessonPartsAsync_LessonNotFound_ReturnsNull()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        var service = CreateService([course], [category], out var dbContextMock);
        var parts = new List<ReplaceLessonPartInputDto> { new() { PartType = LessonPartType.Text, Html = "<p>Intro</p>" } };

        // Act
        var result = await service.ReplaceLessonPartsAsync(course.Id, chapter.Id, 999, parts, actingUserId: 1, isAdmin: false);

        // Assert
        Assert.Null(result);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReplaceLessonPartsAsync_UnresolvableStorageObjectId_ThrowsArgumentException()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        var lesson = chapter.AddLesson("Lesson 1");
        var service = CreateService([course], [category], out var dbContextMock);
        var parts = new List<ReplaceLessonPartInputDto> { new() { PartType = LessonPartType.Image, StorageObjectId = 999 } };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ReplaceLessonPartsAsync(course.Id, chapter.Id, lesson.Id, parts, actingUserId: 1, isAdmin: false));
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReplaceLessonPartsAsync_FilesPart_ResolvesFileStorageObjects()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        var lesson = chapter.AddLesson("Lesson 1");
        var storageObjectA = StorageObject.Create(StorageObjectType.File, "files/a.pdf", "application/pdf", 100, 1, DateTime.UtcNow);
        var storageObjectB = StorageObject.Create(StorageObjectType.File, "files/b.zip", "application/zip", 200, 1, DateTime.UtcNow);
        EntityIdSetter.SetId(storageObjectA, 1);
        EntityIdSetter.SetId(storageObjectB, 2);
        var service = CreateService([course], [category], [storageObjectA, storageObjectB], out var dbContextMock);
        var parts = new List<ReplaceLessonPartInputDto>
        {
            new()
            {
                PartType = LessonPartType.Files,
                Files =
                [
                    new LessonPartFileInputDto { FileName = "a.pdf", StorageObjectId = storageObjectA.Id },
                    new LessonPartFileInputDto { FileName = "b.zip", StorageObjectId = storageObjectB.Id },
                ],
            },
        };

        // Act
        var result = await service.ReplaceLessonPartsAsync(course.Id, chapter.Id, lesson.Id, parts, actingUserId: 1, isAdmin: false);

        // Assert
        Assert.NotNull(result);
        var resultPart = result!.Chapters.Single().Lessons.Single().Parts.Single();
        Assert.Equal(LessonPartType.Files, resultPart.PartType);
        Assert.Equal(2, resultPart.Files.Count);
        Assert.Equal("a.pdf", resultPart.Files[0].FileName);
        Assert.Equal("files/a.pdf", resultPart.Files[0].StorageObjectKey);
        Assert.Equal("b.zip", resultPart.Files[1].FileName);
        Assert.Equal("files/b.zip", resultPart.Files[1].StorageObjectKey);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceLessonPartsAsync_FilesPartUnresolvableStorageObjectId_ThrowsArgumentException()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        var lesson = chapter.AddLesson("Lesson 1");
        var service = CreateService([course], [category], out var dbContextMock);
        var parts = new List<ReplaceLessonPartInputDto>
        {
            new()
            {
                PartType = LessonPartType.Files,
                Files = [new LessonPartFileInputDto { FileName = "a.pdf", StorageObjectId = 999 }],
            },
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ReplaceLessonPartsAsync(course.Id, chapter.Id, lesson.Id, parts, actingUserId: 1, isAdmin: false));
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReplaceLessonPartsAsync_CalledTwice_DiscardsPreviousParts()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        var lesson = chapter.AddLesson("Lesson 1");
        var service = CreateService([course], [category], out _);
        await service.ReplaceLessonPartsAsync(course.Id, chapter.Id, lesson.Id,
            [new() { PartType = LessonPartType.Text, Html = "<p>First</p>" }], actingUserId: 1, isAdmin: false);

        // Act
        var result = await service.ReplaceLessonPartsAsync(course.Id, chapter.Id, lesson.Id,
            [new() { PartType = LessonPartType.Text, Html = "<p>Second</p>" }], actingUserId: 1, isAdmin: false);

        // Assert
        var resultParts = result!.Chapters.Single().Lessons.Single().Parts;
        Assert.Single(resultParts);
        Assert.Equal("<p>Second</p>", resultParts[0].Html);
    }

    [Fact]
    public async Task CreateCourseAsync_SanitizesDescriptionBeforePersisting()
    {
        var category = Category.Create("Backend");
        var (service, sanitizer) = CreateSanitizingService([], [category], out _);
        sanitizer.Setup(s => s.Sanitize("<p>hi</p><script>x</script>")).Returns("<p>hi</p>");
        var dto = new CreateCourseDto { Title = "T", ShortIntroduction = "S", Description = "<p>hi</p><script>x</script>", CategoryId = category.Id };

        var result = await service.CreateCourseAsync(dto, createdByUserId: 1);

        sanitizer.Verify(s => s.Sanitize("<p>hi</p><script>x</script>"), Times.Once);
        Assert.Equal("<p>hi</p>", result.Description);
    }

    [Fact]
    public async Task AddLessonAsync_SanitizesContentBeforePersisting()
    {
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("T", "S", "D", category, createdByUserId: 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        var (service, sanitizer) = CreateSanitizingService([course], [category], out _);
        sanitizer.Setup(s => s.Sanitize("<p>dirty</p>")).Returns("<p>clean</p>");

        var result = await service.AddLessonAsync(course.Id, chapter.Id,
            new AddLessonDto { Title = "L", Content = "<p>dirty</p>" }, actingUserId: 1, isAdmin: false);

        sanitizer.Verify(s => s.Sanitize("<p>dirty</p>"), Times.Once);
        Assert.Equal("<p>clean</p>", result!.Chapters.Single().Lessons.Single().Content);
    }

    [Fact]
    public async Task ReplaceLessonPartsAsync_SanitizesTextPartHtmlOnly()
    {
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("T", "S", "D", category, createdByUserId: 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        var lesson = chapter.AddLesson("Lesson 1");
        var storageObject = StorageObject.Create(StorageObjectType.Image, "images/a.png", "image/png", 100, 1, DateTime.UtcNow);
        var (service, sanitizer) = CreateSanitizingService([course], [category], [storageObject], out _);
        sanitizer.Setup(s => s.Sanitize("<p>x</p><script>y</script>")).Returns("<p>x</p>");
        var parts = new List<ReplaceLessonPartInputDto>
        {
            new() { PartType = LessonPartType.Text, Html = "<p>x</p><script>y</script>" },
            new() { PartType = LessonPartType.Image, StorageObjectId = storageObject.Id },
        };

        var result = await service.ReplaceLessonPartsAsync(course.Id, chapter.Id, lesson.Id, parts, actingUserId: 1, isAdmin: false);

        sanitizer.Verify(s => s.Sanitize("<p>x</p><script>y</script>"), Times.Once);
        sanitizer.Verify(s => s.Sanitize(null), Times.Never); // image part carries no Html
        Assert.Equal("<p>x</p>", result!.Chapters.Single().Lessons.Single().Parts[0].Html);
    }

    private static (CourseService Service, Mock<IHtmlSanitizer> Sanitizer) CreateSanitizingService(
        IReadOnlyCollection<DomainCourse> courses,
        IReadOnlyCollection<Category> categories,
        out Mock<IAppDbContext> dbContextMock) =>
        CreateSanitizingService(courses, categories, [], out dbContextMock);

    private static (CourseService Service, Mock<IHtmlSanitizer> Sanitizer) CreateSanitizingService(
        IReadOnlyCollection<DomainCourse> courses,
        IReadOnlyCollection<Category> categories,
        IReadOnlyCollection<StorageObject> storageObjects,
        out Mock<IAppDbContext> dbContextMock)
    {
        dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.Courses).Returns(courses.ToList().BuildMockDbSet().Object);
        dbContextMock.SetupGet(c => c.Categories).Returns(categories.ToList().BuildMockDbSet().Object);
        dbContextMock.SetupGet(c => c.StorageObjects).Returns(storageObjects.ToList().BuildMockDbSet().Object);
        dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sanitizer = new Mock<IHtmlSanitizer>();
        sanitizer.Setup(s => s.Sanitize(It.IsAny<string?>())).Returns((string? html) => html ?? string.Empty);

        var service = new CourseService(NullLogger<CourseService>.Instance, dbContextMock.Object, TimeProvider.System, sanitizer.Object);
        return (service, sanitizer);
    }
}
