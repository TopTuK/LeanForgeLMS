using LF.Application;
using LF.Application.Common.Interfaces;
using LF.Application.Services.Admin;
using LF.Application.Services.Authentication;
using LF.Application.Services.Course;
using LF.Application.Services.CourseAuthoring;
using LF.Application.Services.Enrollment;
using LF.Application.Services.EnrollmentLearning;
using LF.Application.Services.Profile;
using LF.Application.Services.Storage;
using LF.Application.Services.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CourseServiceImpl = LF.Application.Services.Course.CourseService;

namespace LF.ApplicationTests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddAuthenticationApplication_RegistersAuthTokenProfileAdminAndCourseAuthoringServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped(_ => Mock.Of<IGrpcIdentityService>());
        services.AddScoped(_ => Mock.Of<IGrpcCourseService>());
        services.AddScoped(_ => Mock.Of<IGrpcEnrollmentService>());
        services.AddScoped(_ => Mock.Of<IStorageRepository>());
        services.AddKeyedScoped("storage", (_, _) => Mock.Of<IFileStorageService>());
        services.AddAuthenticationApplication();

        // Act
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();

        // Assert
        Assert.IsType<AuthenticationService>(scope.ServiceProvider.GetRequiredService<IAuthenticationService>());
        Assert.IsType<TokenService>(scope.ServiceProvider.GetRequiredService<ITokenService>());
        Assert.IsType<ProfileService>(scope.ServiceProvider.GetRequiredService<IProfileService>());
        Assert.IsType<AdminUserService>(scope.ServiceProvider.GetRequiredService<IAdminUserService>());
        Assert.IsType<CourseAuthoringService>(scope.ServiceProvider.GetRequiredService<ICourseAuthoringService>());
        Assert.IsType<EnrollmentLearningService>(scope.ServiceProvider.GetRequiredService<IEnrollmentLearningService>());
        Assert.IsType<StorageService>(scope.ServiceProvider.GetRequiredService<IStorageService>());
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
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();

        // Assert
        Assert.IsType<UserService>(scope.ServiceProvider.GetRequiredService<IUserService>());
    }

    [Fact]
    public void AddCourseApplication_RegistersCourseService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(Mock.Of<IAppDbContext>());
        services.AddCourseApplication();

        // Act
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();

        // Assert
        Assert.IsType<CourseServiceImpl>(scope.ServiceProvider.GetRequiredService<ICourseService>());
        Assert.IsType<EnrollmentService>(scope.ServiceProvider.GetRequiredService<IEnrollmentService>());
    }
}
