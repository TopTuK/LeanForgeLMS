using LF.Application.ModelDto.Authentication;
using LF.Application.ModelDto.User;
using LF.Application.Services.Authentication;
using LF.Application.Services.User;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LF.ApplicationTests.Services.Authentication;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task AuthenticatePmiUserAsync_DelegatesToIdentityService()
    {
        // Arrange
        var request = new UserAuthentificationDto { Email = "pmi@x.com", FirstName = "P", LastName = "M" };
        var expected = new UserDto { Id = 5, Email = request.Email, FirstName = "P", LastName = "M" };
        var identityMock = new Mock<IGrpcIdentityService>();
        identityMock.Setup(s => s.GetOrCreateUserAsync(request)).ReturnsAsync(expected);
        var service = new AuthenticationService(NullLogger<AuthenticationService>.Instance, identityMock.Object);

        // Act
        var result = await service.AuthenticatePmiUserAsync(request);

        // Assert
        Assert.Same(expected, result);
        identityMock.Verify(s => s.GetOrCreateUserAsync(request), Times.Once);
    }
}
