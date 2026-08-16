using LF.Application.ModelDto.Course;
using LF.Application.Services.Course;
using LF.Application.Services.CourseAuthoring;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LF.ApplicationTests.Services.CourseAuthoring;

public class CourseAuthoringServiceTests
{
    [Fact]
    public async Task CreateCourseAsync_DelegatesToGrpcCourseService()
    {
        // Arrange
        var dto = new CreateCourseDto { Title = "Title", ShortIntroduction = "Short", Description = "Description", CategoryId = 1 };
        var expected = new CourseDetailDto { Id = 1, Title = "Title", CreatedByUserId = 5 };
        var grpcMock = new Mock<IGrpcCourseService>();
        grpcMock.Setup(s => s.CreateCourseAsync(dto, 5)).ReturnsAsync(expected);
        var service = new CourseAuthoringService(NullLogger<CourseAuthoringService>.Instance, grpcMock.Object);

        // Act
        var result = await service.CreateCourseAsync(dto, createdByUserId: 5);

        // Assert
        Assert.Same(expected, result);
        grpcMock.Verify(s => s.CreateCourseAsync(dto, 5), Times.Once);
    }

    [Fact]
    public async Task GetCourseAsync_DelegatesToGrpcCourseService()
    {
        // Arrange
        var expected = new CourseDetailDto { Id = 1, Title = "Title" };
        var grpcMock = new Mock<IGrpcCourseService>();
        grpcMock.Setup(s => s.GetCourseAsync(1, 5, false)).ReturnsAsync(expected);
        var service = new CourseAuthoringService(NullLogger<CourseAuthoringService>.Instance, grpcMock.Object);

        // Act
        var result = await service.GetCourseAsync(1, actingUserId: 5, isAdmin: false);

        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task ListCoursesAsync_DelegatesToGrpcCourseService()
    {
        // Arrange
        var expected = new PagedCoursesDto { TotalCount = 1 };
        var grpcMock = new Mock<IGrpcCourseService>();
        grpcMock.Setup(s => s.ListCoursesAsync(1, 20, 5, true)).ReturnsAsync(expected);
        var service = new CourseAuthoringService(NullLogger<CourseAuthoringService>.Instance, grpcMock.Object);

        // Act
        var result = await service.ListCoursesAsync(1, 20, actingUserId: 5, isAdmin: true);

        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task ListCategoriesAsync_DelegatesToGrpcCourseService()
    {
        // Arrange
        IReadOnlyList<CategoryDto> expected = [new CategoryDto { Id = 1, Name = "Backend" }];
        var grpcMock = new Mock<IGrpcCourseService>();
        grpcMock.Setup(s => s.ListCategoriesAsync()).ReturnsAsync(expected);
        var service = new CourseAuthoringService(NullLogger<CourseAuthoringService>.Instance, grpcMock.Object);

        // Act
        var result = await service.ListCategoriesAsync();

        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task CreateCategoryAsync_DelegatesToGrpcCourseService()
    {
        // Arrange
        var expected = new CategoryDto { Id = 1, Name = "Backend" };
        var grpcMock = new Mock<IGrpcCourseService>();
        grpcMock.Setup(s => s.CreateCategoryAsync("Backend")).ReturnsAsync(expected);
        var service = new CourseAuthoringService(NullLogger<CourseAuthoringService>.Instance, grpcMock.Object);

        // Act
        var result = await service.CreateCategoryAsync("Backend");

        // Assert
        Assert.Same(expected, result);
        grpcMock.Verify(s => s.CreateCategoryAsync("Backend"), Times.Once);
    }

    [Fact]
    public async Task DeleteCategoryAsync_DelegatesToGrpcCourseService()
    {
        // Arrange
        var grpcMock = new Mock<IGrpcCourseService>();
        grpcMock.Setup(s => s.DeleteCategoryAsync(1)).ReturnsAsync(true);
        var service = new CourseAuthoringService(NullLogger<CourseAuthoringService>.Instance, grpcMock.Object);

        // Act
        var result = await service.DeleteCategoryAsync(1);

        // Assert
        Assert.True(result);
        grpcMock.Verify(s => s.DeleteCategoryAsync(1), Times.Once);
    }

    [Fact]
    public async Task AddChapterAsync_DelegatesToGrpcCourseService()
    {
        // Arrange
        var expected = new CourseDetailDto { Id = 1, Title = "Title" };
        var grpcMock = new Mock<IGrpcCourseService>();
        grpcMock.Setup(s => s.AddChapterAsync(1, "Chapter 1", 5, false)).ReturnsAsync(expected);
        var service = new CourseAuthoringService(NullLogger<CourseAuthoringService>.Instance, grpcMock.Object);

        // Act
        var result = await service.AddChapterAsync(1, "Chapter 1", actingUserId: 5, isAdmin: false);

        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task PublishCourseAsync_DelegatesToGrpcCourseService()
    {
        // Arrange
        var expected = new CourseDetailDto { Id = 1, Title = "Title", IsPublished = true };
        var grpcMock = new Mock<IGrpcCourseService>();
        grpcMock.Setup(s => s.PublishCourseAsync(1, 5, false)).ReturnsAsync(expected);
        var service = new CourseAuthoringService(NullLogger<CourseAuthoringService>.Instance, grpcMock.Object);

        // Act
        var result = await service.PublishCourseAsync(1, actingUserId: 5, isAdmin: false);

        // Assert
        Assert.Same(expected, result);
        grpcMock.Verify(s => s.PublishCourseAsync(1, 5, false), Times.Once);
    }
}
