using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.Course.Enums;
using LF.AppDomain.Models.Storage.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Course;
using LF.Application.Services.Course;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using DomainCourse = LF.AppDomain.Entities.Course.Course;

namespace LF.ApplicationTests.Services.Course;

public class CourseServiceTests
{
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

        return new CourseService(NullLogger<CourseService>.Instance, dbContextMock.Object, TimeProvider.System);
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
            Title = "Title", ShortIntroduction = "Short", Description = "Description", CategoryId = category.Id,
            CoverType = CourseCoverType.Color, CoverColor = CourseCoverColor.Ocean,
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
            Title = "Title", ShortIntroduction = "Short", Description = "Description", CategoryId = category.Id,
            CoverType = CourseCoverType.Image, CoverImageStorageObjectId = storageObject.Id,
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
            Title = "Title", ShortIntroduction = "Short", Description = "Description", CategoryId = category.Id,
            CoverType = CourseCoverType.Image, CoverImageStorageObjectId = 999,
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
}
