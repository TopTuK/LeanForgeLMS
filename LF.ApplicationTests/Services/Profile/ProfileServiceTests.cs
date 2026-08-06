using LF.Application.ModelDto.User;
using LF.Application.Services.Profile;
using LF.Application.Services.User;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LF.ApplicationTests.Services.Profile;

public class ProfileServiceTests
{
    [Fact]
    public async Task GetProfileAsync_DelegatesToIdentityService()
    {
        // Arrange
        var expected = new UserDto { Id = 1, Email = "a@b.com", FirstName = "A", LastName = "B" };
        var identityMock = new Mock<IGrpcIdentityService>();
        identityMock.Setup(s => s.GetUserProfileAsync(1)).ReturnsAsync(expected);
        var service = new ProfileService(NullLogger<ProfileService>.Instance, identityMock.Object);

        // Act
        var result = await service.GetProfileAsync(1);

        // Assert
        Assert.Same(expected, result);
        identityMock.Verify(s => s.GetUserProfileAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetProfileAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        var identityMock = new Mock<IGrpcIdentityService>();
        identityMock.Setup(s => s.GetUserProfileAsync(It.IsAny<int>())).ReturnsAsync((UserDto?)null);
        var service = new ProfileService(NullLogger<ProfileService>.Instance, identityMock.Object);

        // Act
        var result = await service.GetProfileAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProfileAsync_DelegatesToIdentityService()
    {
        // Arrange
        var dto = new UpdateUserNameDto { FirstName = "New", LastName = "Name" };
        var expected = new UserDto { Id = 1, Email = "a@b.com", FirstName = "New", LastName = "Name" };
        var identityMock = new Mock<IGrpcIdentityService>();
        identityMock.Setup(s => s.UpdateUserProfileAsync(1, dto)).ReturnsAsync(expected);
        var service = new ProfileService(NullLogger<ProfileService>.Instance, identityMock.Object);

        // Act
        var result = await service.UpdateProfileAsync(1, dto);

        // Assert
        Assert.Same(expected, result);
        identityMock.Verify(s => s.UpdateUserProfileAsync(1, dto), Times.Once);
    }
}
