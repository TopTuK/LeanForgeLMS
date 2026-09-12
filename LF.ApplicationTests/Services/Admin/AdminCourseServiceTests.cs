using LF.AppDomain.Models.Course.Enums;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Course;
using LF.Application.ModelDto.Enrollment;
using LF.Application.ModelDto.User;
using LF.Application.Services.Admin;
using LF.Application.Services.Course;
using LF.Application.Services.User;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LF.ApplicationTests.Services.Admin;

public class AdminCourseServiceTests
{
    private static AdminCourseService CreateService(
        out Mock<IGrpcCourseService> courseMock,
        out Mock<IGrpcIdentityService> identityMock,
        out Mock<IFileStorageService> storageMock)
    {
        courseMock = new Mock<IGrpcCourseService>();
        identityMock = new Mock<IGrpcIdentityService>();
        storageMock = new Mock<IFileStorageService>();

        return new AdminCourseService(
            NullLogger<AdminCourseService>.Instance, courseMock.Object, identityMock.Object, storageMock.Object);
    }

    private static CourseEnrollmentDto CreateEnrollment(int id, int userId) => new()
    {
        Id = id,
        UserId = userId,
        Status = EnrollmentStatus.Active,
        EnrolledAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task ListCoursesAsync_ListsEveryCourseAsAdmin()
    {
        // Arrange
        var expected = new PagedCoursesDto { Items = [], TotalCount = 0 };
        var service = CreateService(out var courseMock, out var identityMock, out _);
        courseMock.Setup(s => s.ListCoursesAsync(1, 20, 42, true)).ReturnsAsync(expected);
        identityMock.Setup(s => s.ListUsersByIdsAsync(It.IsAny<IReadOnlyList<int>>())).ReturnsAsync([]);

        // Act
        var result = await service.ListCoursesAsync(1, 20, actingAdminId: 42);

        // Assert
        Assert.Same(expected, result);
        courseMock.Verify(s => s.ListCoursesAsync(1, 20, 42, true), Times.Once);
    }

    // The admin list mixes every author's courses together, so each row must name its owner.
    [Fact]
    public async Task ListCoursesAsync_HydratesCourseAuthors()
    {
        // Arrange
        var paged = new PagedCoursesDto
        {
            TotalCount = 1,
            Items = [new CourseSummaryDto { Id = 5, Title = "Async in C#", CreatedByUserId = 3 }],
        };
        var service = CreateService(out var courseMock, out var identityMock, out _);
        courseMock.Setup(s => s.ListCoursesAsync(1, 20, 42, true)).ReturnsAsync(paged);
        identityMock
            .Setup(s => s.ListUsersByIdsAsync(It.Is<IReadOnlyList<int>>(ids => ids.Single() == 3)))
            .ReturnsAsync([new UserDto { Id = 3, Email = "ada@b.com", FirstName = "Ada", LastName = "Lovelace" }]);

        // Act
        var result = await service.ListCoursesAsync(1, 20, actingAdminId: 42);

        // Assert
        var course = Assert.Single(result.Items);
        Assert.Equal("Ada", course.AuthorFirstName);
        Assert.Equal("ada@b.com", course.AuthorEmail);
    }

    [Fact]
    public async Task ListCoursesAsync_UnknownAuthor_LeavesAuthorFieldsNull()
    {
        var paged = new PagedCoursesDto
        {
            TotalCount = 1,
            Items = [new CourseSummaryDto { Id = 5, Title = "Async in C#", CreatedByUserId = 3 }],
        };
        var service = CreateService(out var courseMock, out var identityMock, out _);
        courseMock.Setup(s => s.ListCoursesAsync(1, 20, 42, true)).ReturnsAsync(paged);
        identityMock.Setup(s => s.ListUsersByIdsAsync(It.IsAny<IReadOnlyList<int>>())).ReturnsAsync([]);

        var result = await service.ListCoursesAsync(1, 20, actingAdminId: 42);

        Assert.Null(Assert.Single(result.Items).AuthorEmail);
    }

    [Fact]
    public async Task ListCourseEnrollmentsAsync_HydratesStudentsFromIdentityService()
    {
        // Arrange
        var paged = new PagedCourseEnrollmentsDto
        {
            TotalCount = 2,
            Items = [CreateEnrollment(id: 1, userId: 7), CreateEnrollment(id: 2, userId: 8)],
        };
        var service = CreateService(out var courseMock, out var identityMock, out _);
        courseMock.Setup(s => s.ListCourseEnrollmentsAsync(5, 42, 1, 20)).ReturnsAsync(paged);
        identityMock
            .Setup(s => s.ListUsersByIdsAsync(It.Is<IReadOnlyList<int>>(ids => ids.Count == 2 && ids.Contains(7) && ids.Contains(8))))
            .ReturnsAsync([
                new UserDto { Id = 7, Email = "a@b.com", FirstName = "Ada", LastName = "L" },
                new UserDto { Id = 8, Email = "g@h.com", FirstName = "Grace", LastName = "H" },
            ]);

        // Act
        var result = await service.ListCourseEnrollmentsAsync(courseId: 5, actingAdminId: 42, page: 1, pageSize: 20);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("a@b.com", result!.Items[0].StudentEmail);
        Assert.Equal("Ada", result.Items[0].StudentFirstName);
        Assert.Equal("Grace", result.Items[1].StudentFirstName);
    }

    // Users live in another bounded context with no FK back, so a deleted user leaves their
    // enrollment behind. The row must still list rather than disappear or throw.
    [Fact]
    public async Task ListCourseEnrollmentsAsync_UnknownUser_LeavesStudentFieldsNull()
    {
        var paged = new PagedCourseEnrollmentsDto { TotalCount = 1, Items = [CreateEnrollment(id: 1, userId: 7)] };
        var service = CreateService(out var courseMock, out var identityMock, out _);
        courseMock.Setup(s => s.ListCourseEnrollmentsAsync(5, 42, 1, 20)).ReturnsAsync(paged);
        identityMock.Setup(s => s.ListUsersByIdsAsync(It.IsAny<IReadOnlyList<int>>())).ReturnsAsync([]);

        var result = await service.ListCourseEnrollmentsAsync(courseId: 5, actingAdminId: 42, page: 1, pageSize: 20);

        Assert.NotNull(result);
        Assert.Null(Assert.Single(result!.Items).StudentEmail);
    }

    [Fact]
    public async Task ListCourseEnrollmentsAsync_MissingCourse_ReturnsNullWithoutHydrating()
    {
        var service = CreateService(out var courseMock, out var identityMock, out _);
        courseMock.Setup(s => s.ListCourseEnrollmentsAsync(5, 42, 1, 20)).ReturnsAsync((PagedCourseEnrollmentsDto?)null);

        Assert.Null(await service.ListCourseEnrollmentsAsync(courseId: 5, actingAdminId: 42, page: 1, pageSize: 20));
        identityMock.Verify(s => s.ListUsersByIdsAsync(It.IsAny<IReadOnlyList<int>>()), Times.Never);
    }

    [Fact]
    public async Task EnrollStudentAsync_AlwaysActsAsAdmin()
    {
        var service = CreateService(out var courseMock, out _, out _);
        courseMock.Setup(s => s.EnrollUserAsync(5, 7, 42, true)).ReturnsAsync(new EnrollmentSummaryDto { Id = 1 });

        await service.EnrollStudentAsync(courseId: 5, targetUserId: 7, actingAdminId: 42);

        courseMock.Verify(s => s.EnrollUserAsync(5, 7, 42, true), Times.Once);
    }

    [Fact]
    public async Task DeleteCourseAsync_DeletesOrphanedStorageObjects()
    {
        // Arrange
        var result = new DeleteCourseResultDto { StorageObjectKeys = ["images/a.png", "videos/b.mp4"] };
        var service = CreateService(out var courseMock, out _, out var storageMock);
        courseMock.Setup(s => s.DeleteCourseAsync(5, 42, false)).ReturnsAsync(result);

        // Act
        await service.DeleteCourseAsync(courseId: 5, actingAdminId: 42, force: false);

        // Assert
        storageMock.Verify(s => s.DeleteAsync("images/a.png", It.IsAny<CancellationToken>()), Times.Once);
        storageMock.Verify(s => s.DeleteAsync("videos/b.mp4", It.IsAny<CancellationToken>()), Times.Once);
    }

    // The course rows are already gone once the RPC returns, so a storage failure must not be
    // reported as a failed delete — that would invite a retry against a course that no longer exists.
    [Fact]
    public async Task DeleteCourseAsync_StorageFailure_StillSucceedsAndContinues()
    {
        var result = new DeleteCourseResultDto { StorageObjectKeys = ["images/a.png", "videos/b.mp4"] };
        var service = CreateService(out var courseMock, out _, out var storageMock);
        courseMock.Setup(s => s.DeleteCourseAsync(5, 42, true)).ReturnsAsync(result);
        storageMock.Setup(s => s.DeleteAsync("images/a.png", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("minio down"));

        var deleted = await service.DeleteCourseAsync(courseId: 5, actingAdminId: 42, force: true);

        Assert.Same(result, deleted);
        storageMock.Verify(s => s.DeleteAsync("videos/b.mp4", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCourseAsync_MissingCourse_TouchesNoStorage()
    {
        var service = CreateService(out var courseMock, out _, out var storageMock);
        courseMock.Setup(s => s.DeleteCourseAsync(5, 42, false)).ReturnsAsync((DeleteCourseResultDto?)null);

        Assert.Null(await service.DeleteCourseAsync(courseId: 5, actingAdminId: 42, force: false));
        storageMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveEnrollmentAsync_DelegatesToCourseService()
    {
        var expected = new RemoveEnrollmentResultDto { UserId = 7, WasPaid = true, PricePaid = 1990m };
        var service = CreateService(out var courseMock, out _, out _);
        courseMock.Setup(s => s.RemoveEnrollmentAsync(5, 10, 42)).ReturnsAsync(expected);

        var result = await service.RemoveEnrollmentAsync(courseId: 5, enrollmentId: 10, actingAdminId: 42);

        Assert.Same(expected, result);
    }
}
