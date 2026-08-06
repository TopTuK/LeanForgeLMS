using LF.Application;
using LF.Application.Common.Interfaces;
using LF.Application.Services.Authentication;
using LF.Application.Services.Profile;
using LF.Application.Services.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LF.ApplicationTests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddAuthenticationApplication_RegistersAuthTokenAndProfileServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(Mock.Of<IGrpcIdentityService>());
        services.AddAuthenticationApplication();

        // Act
        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        // Assert
        Assert.IsType<AuthenticationService>(scope.ServiceProvider.GetRequiredService<IAuthenticationService>());
        Assert.IsType<TokenService>(scope.ServiceProvider.GetRequiredService<ITokenService>());
        Assert.IsType<ProfileService>(scope.ServiceProvider.GetRequiredService<IProfileService>());
    }

    [Fact]
    public void AddUserApplication_RegistersUserService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(Mock.Of<IAppDbContext>());
        services.AddUserApplication();

        // Act
        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        // Assert
        Assert.IsType<UserService>(scope.ServiceProvider.GetRequiredService<IUserService>());
    }
}
