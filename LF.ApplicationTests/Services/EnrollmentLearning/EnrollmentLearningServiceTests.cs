using LF.Application.ModelDto.Enrollment;
using LF.Application.Services.Enrollment;
using LF.Application.Services.EnrollmentLearning;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LF.ApplicationTests.Services.EnrollmentLearning;

public class EnrollmentLearningServiceTests
{
    [Fact]
    public async Task BrowseCatalogAsync_DelegatesToGrpcEnrollmentService()
    {
        // Arrange
        var expected = new PagedCourseCatalogDto { TotalCount = 1 };
        var grpcMock = new Mock<IGrpcEnrollmentService>();
        grpcMock.Setup(s => s.BrowseCatalogAsync(1, 20, 5)).ReturnsAsync(expected);
        var service = new EnrollmentLearningService(NullLogger<EnrollmentLearningService>.Instance, grpcMock.Object);

        // Act
        var result = await service.BrowseCatalogAsync(1, 20, actingUserId: 5);

        // Assert
        Assert.Same(expected, result);
        grpcMock.Verify(s => s.BrowseCatalogAsync(1, 20, 5), Times.Once);
    }

    [Fact]
    public async Task EnrollAsync_DelegatesToGrpcEnrollmentService()
    {
        // Arrange
        var expected = new EnrollmentDetailDto { Id = 1, CourseId = 3 };
        var grpcMock = new Mock<IGrpcEnrollmentService>();
        grpcMock.Setup(s => s.EnrollAsync(3, 5)).ReturnsAsync(expected);
        var service = new EnrollmentLearningService(NullLogger<EnrollmentLearningService>.Instance, grpcMock.Object);

        // Act
        var result = await service.EnrollAsync(3, actingUserId: 5);

        // Assert
        Assert.Same(expected, result);
        grpcMock.Verify(s => s.EnrollAsync(3, 5), Times.Once);
    }

    [Fact]
    public async Task ListMyEnrollmentsAsync_DelegatesToGrpcEnrollmentService()
    {
        // Arrange
        IReadOnlyList<EnrollmentSummaryDto> expected = [new EnrollmentSummaryDto { Id = 1, CourseId = 3 }];
        var grpcMock = new Mock<IGrpcEnrollmentService>();
        grpcMock.Setup(s => s.ListMyEnrollmentsAsync(5, EnrollmentStatusFilter.Active)).ReturnsAsync(expected);
        var service = new EnrollmentLearningService(NullLogger<EnrollmentLearningService>.Instance, grpcMock.Object);

        // Act
        var result = await service.ListMyEnrollmentsAsync(5, EnrollmentStatusFilter.Active);

        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetEnrollmentAsync_DelegatesToGrpcEnrollmentService()
    {
        // Arrange
        var expected = new EnrollmentDetailDto { Id = 1, CourseId = 3 };
        var grpcMock = new Mock<IGrpcEnrollmentService>();
        grpcMock.Setup(s => s.GetEnrollmentAsync(1, 5, false)).ReturnsAsync(expected);
        var service = new EnrollmentLearningService(NullLogger<EnrollmentLearningService>.Instance, grpcMock.Object);

        // Act
        var result = await service.GetEnrollmentAsync(1, actingUserId: 5, isAdmin: false);

        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task CompleteLessonAsync_DelegatesToGrpcEnrollmentService()
    {
        // Arrange
        var expected = new EnrollmentDetailDto { Id = 1, CourseId = 3 };
        var grpcMock = new Mock<IGrpcEnrollmentService>();
        grpcMock.Setup(s => s.CompleteLessonAsync(1, 10, 5, false)).ReturnsAsync(expected);
        var service = new EnrollmentLearningService(NullLogger<EnrollmentLearningService>.Instance, grpcMock.Object);

        // Act
        var result = await service.CompleteLessonAsync(1, 10, actingUserId: 5, isAdmin: false);

        // Assert
        Assert.Same(expected, result);
        grpcMock.Verify(s => s.CompleteLessonAsync(1, 10, 5, false), Times.Once);
    }

    [Fact]
    public async Task GetCourseCoverAsync_DelegatesToGrpcEnrollmentService()
    {
        // Arrange
        var expected = new CourseCoverDto { CoverImageKey = "images/a.png", CoverImageContentType = "image/png" };
        var grpcMock = new Mock<IGrpcEnrollmentService>();
        grpcMock.Setup(s => s.GetCourseCoverAsync(3)).ReturnsAsync(expected);
        var service = new EnrollmentLearningService(NullLogger<EnrollmentLearningService>.Instance, grpcMock.Object);

        // Act
        var result = await service.GetCourseCoverAsync(3);

        // Assert
        Assert.Same(expected, result);
        grpcMock.Verify(s => s.GetCourseCoverAsync(3), Times.Once);
    }
}
