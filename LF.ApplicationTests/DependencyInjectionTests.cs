using LF.Application;
using LF.Application.Common.Interfaces;
using LF.Application.Services.Admin;
using LF.Application.Services.Authentication;
using LF.Application.Services.Course;
using LF.Application.Services.CourseAuthoring;
using LF.Application.Services.Enrollment;
using LF.Application.Services.EnrollmentLearning;
using LF.Application.Services.Payment;
using LF.Application.Services.PaymentReporting;
using LF.Application.Services.Platform;
using LF.Application.Services.Profile;
using LF.Application.Services.Promo;
using LF.Application.Services.PromoCodeAdmin;
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
        services.AddScoped(_ => Mock.Of<IGrpcPromoCodeService>());
        services.AddScoped(_ => Mock.Of<IStorageRepository>());
        services.AddScoped(_ => Mock.Of<IAppDbContext>());
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
        Assert.IsType<PromoCodeAdminService>(scope.ServiceProvider.GetRequiredService<IPromoCodeAdminService>());
        Assert.IsType<StorageService>(scope.ServiceProvider.GetRequiredService<IStorageService>());
        Assert.IsType<PlatformSettingsService>(scope.ServiceProvider.GetRequiredService<IPlatformSettingsService>());
        Assert.IsType<PaymentReportService>(scope.ServiceProvider.GetRequiredService<IPaymentReportService>());
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
        Assert.IsType<PromoCodeService>(scope.ServiceProvider.GetRequiredService<IPromoCodeService>());
        Assert.IsType<PlatformSettingsService>(scope.ServiceProvider.GetRequiredService<IPlatformSettingsService>());
    }

    [Fact]
    public void AddPaymentApplication_RegistersPaymentOrderService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(Mock.Of<IAppDbContext>());
        services.AddScoped(_ => Mock.Of<IPaymentGateway>());
        services.AddPaymentApplication();

        // Act
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();

        // Assert
        Assert.IsType<PaymentOrderService>(scope.ServiceProvider.GetRequiredService<IPaymentOrderService>());
    }
}
