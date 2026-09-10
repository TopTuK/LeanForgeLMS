using LF.Application.Common;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Enrollment;
using LF.Application.Services.Enrollment;
using LF.Application.Services.EnrollmentLearning;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LF.ApplicationTests.Services.EnrollmentLearning;

public class EnrollmentLearningServiceTests
{
    // Self-enrollment is flag-gated, so every test needs a feature flag stub. Defaults to enabled
    // so the delegation tests exercise the happy path.
    private static EnrollmentLearningService CreateService(
        Mock<IGrpcEnrollmentService> grpcMock,
        Mock<IFeatureFlagService>? featureFlagsMock = null,
        bool selfEnrollmentEnabled = true)
    {
        featureFlagsMock ??= new Mock<IFeatureFlagService>();
        featureFlagsMock
            .Setup(f => f.IsEnabledAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(selfEnrollmentEnabled);

        return new EnrollmentLearningService(
            NullLogger<EnrollmentLearningService>.Instance, grpcMock.Object, featureFlagsMock.Object);
    }

    [Fact]
    public async Task BrowseCatalogAsync_DelegatesToGrpcEnrollmentService()
    {
        // Arrange
        var expected = new PagedCourseCatalogDto { TotalCount = 1 };
        var grpcMock = new Mock<IGrpcEnrollmentService>();
        grpcMock.Setup(s => s.BrowseCatalogAsync(1, 20, 5)).ReturnsAsync(expected);
        var service = CreateService(grpcMock);

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
        var service = CreateService(grpcMock);

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
        var service = CreateService(grpcMock);

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
        var service = CreateService(grpcMock);

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
        var service = CreateService(grpcMock);

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
        var service = CreateService(grpcMock);

        // Act
        var result = await service.GetCourseCoverAsync(3);

        // Assert
        Assert.Same(expected, result);
        grpcMock.Verify(s => s.GetCourseCoverAsync(3), Times.Once);
    }

    [Fact]
    public async Task GetCoursePreviewAsync_DelegatesToGrpcEnrollmentService()
    {
        // Arrange
        var expected = new CoursePreviewDto { Id = 3, Title = "Title" };
        var grpcMock = new Mock<IGrpcEnrollmentService>();
        grpcMock.Setup(s => s.GetCoursePreviewAsync(3, 5)).ReturnsAsync(expected);
        var service = CreateService(grpcMock);

        // Act
        var result = await service.GetCoursePreviewAsync(3, actingUserId: 5);

        // Assert
        Assert.Same(expected, result);
        grpcMock.Verify(s => s.GetCoursePreviewAsync(3, 5), Times.Once);
    }

    [Fact]
    public async Task EnrollAsync_SelfEnrollmentFlagDisabled_ThrowsEnrollmentDisabledException()
    {
        // Arrange
        var grpcMock = new Mock<IGrpcEnrollmentService>();
        var service = CreateService(grpcMock, selfEnrollmentEnabled: false);

        // Act
        var ex = await Assert.ThrowsAsync<EnrollmentDisabledException>(() => service.EnrollAsync(3, actingUserId: 5));

        // Assert
        // Endpoints catch InvalidOperationException to produce the 409, so the base type matters.
        Assert.IsAssignableFrom<InvalidOperationException>(ex);
    }

    [Fact]
    public async Task EnrollAsync_SelfEnrollmentFlagDisabled_DoesNotCallGrpc()
    {
        // Arrange
        var grpcMock = new Mock<IGrpcEnrollmentService>();
        var service = CreateService(grpcMock, selfEnrollmentEnabled: false);

        // Act
        await Assert.ThrowsAsync<EnrollmentDisabledException>(() => service.EnrollAsync(3, actingUserId: 5));

        // Assert
        grpcMock.Verify(s => s.EnrollAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task EnrollAsync_EvaluatesSelfEnrollmentFlagForTheActingUser()
    {
        // Arrange
        var grpcMock = new Mock<IGrpcEnrollmentService>();
        grpcMock.Setup(s => s.EnrollAsync(3, 5, null)).ReturnsAsync(new EnrollmentDetailDto { Id = 1, CourseId = 3 });
        var featureFlagsMock = new Mock<IFeatureFlagService>();
        var service = CreateService(grpcMock, featureFlagsMock);

        // Act
        await service.EnrollAsync(3, actingUserId: 5);

        // Assert
        featureFlagsMock.Verify(
            f => f.IsEnabledAsync(FeatureFlags.SelfEnrollment, 5, It.IsAny<CancellationToken>()), Times.Once);
    }
}
