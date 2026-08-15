using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.ModelDto.User;
using LF.Application.Services.Admin;
using LF.Application.Services.User;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LF.ApplicationTests.Services.Admin;

public class AdminUserServiceTests
{
    [Fact]
    public async Task ListUsersAsync_DelegatesToIdentityService()
    {
        // Arrange
        var expected = new PagedUsersDto { Items = [new UserDto { Id = 1, Email = "a@b.com", FirstName = "A" }], TotalCount = 1 };
        var identityMock = new Mock<IGrpcIdentityService>();
        identityMock.Setup(s => s.ListUsersAsync(1, 20, "ada")).ReturnsAsync(expected);
        var service = new AdminUserService(NullLogger<AdminUserService>.Instance, identityMock.Object);

        // Act
        var result = await service.ListUsersAsync(1, 20, "ada");

        // Assert
        Assert.Same(expected, result);
        identityMock.Verify(s => s.ListUsersAsync(1, 20, "ada"), Times.Once);
    }

    [Fact]
    public async Task UpdateUserInfoAsync_DelegatesToIdentityService()
    {
        // Arrange
        var dto = new UpdateUserProfileDto { FirstName = "New", LastName = "Name", Description = "New bio." };
        var expected = new UserDto { Id = 5, Email = "a@b.com", FirstName = "New", LastName = "Name", Description = "New bio." };
        var identityMock = new Mock<IGrpcIdentityService>();
        identityMock.Setup(s => s.UpdateUserProfileAsync(5, dto)).ReturnsAsync(expected);
        var service = new AdminUserService(NullLogger<AdminUserService>.Instance, identityMock.Object);

        // Act
        var result = await service.UpdateUserInfoAsync(5, dto);

        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_DelegatesToIdentityService()
    {
        // Arrange
        var expected = new UserDto { Id = 5, Email = "a@b.com", FirstName = "A", Role = UserRole.Instructor };
        var identityMock = new Mock<IGrpcIdentityService>();
        identityMock.Setup(s => s.UpdateUserRoleAsync(5, It.Is<UpdateUserRoleDto>(d => d.Role == UserRole.Instructor)))
            .ReturnsAsync(expected);
        var service = new AdminUserService(NullLogger<AdminUserService>.Instance, identityMock.Object);

        // Act
        var result = await service.UpdateUserRoleAsync(5, actingAdminId: 1, UserRole.Instructor);

        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_TargetIsActingAdmin_ThrowsSelfAdministrationException()
    {
        // Arrange
        var identityMock = new Mock<IGrpcIdentityService>();
        var service = new AdminUserService(NullLogger<AdminUserService>.Instance, identityMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<SelfAdministrationException>(
            () => service.UpdateUserRoleAsync(targetUserId: 1, actingAdminId: 1, UserRole.Admin));
        identityMock.Verify(s => s.UpdateUserRoleAsync(It.IsAny<int>(), It.IsAny<UpdateUserRoleDto>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_DelegatesToIdentityService()
    {
        // Arrange
        var identityMock = new Mock<IGrpcIdentityService>();
        identityMock.Setup(s => s.DeleteUserAsync(5)).ReturnsAsync(true);
        var service = new AdminUserService(NullLogger<AdminUserService>.Instance, identityMock.Object);

        // Act
        var result = await service.DeleteUserAsync(targetUserId: 5, actingAdminId: 1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteUserAsync_TargetIsActingAdmin_ThrowsSelfAdministrationException()
    {
        // Arrange
        var identityMock = new Mock<IGrpcIdentityService>();
        var service = new AdminUserService(NullLogger<AdminUserService>.Instance, identityMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<SelfAdministrationException>(
            () => service.DeleteUserAsync(targetUserId: 1, actingAdminId: 1));
        identityMock.Verify(s => s.DeleteUserAsync(It.IsAny<int>()), Times.Never);
    }
}
