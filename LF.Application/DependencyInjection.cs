using LF.Application.Services.Admin;
using LF.Application.Services.Authentication;
using LF.Application.Services.Course;
using LF.Application.Services.CourseAuthoring;
using LF.Application.Services.Enrollment;
using LF.Application.Services.EnrollmentLearning;
using LF.Application.Services.Profile;
using LF.Application.Services.Promo;
using LF.Application.Services.PromoCodeAdmin;
using LF.Application.Services.Storage;
using LF.Application.Services.User;
using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace LF.Application;

public static class DependencyInjection
{
    // Split by host rather than one AddApplication(): ASP.NET Core validates the whole DI
    // graph on Build(), so registering AuthenticationService (needs IGrpcIdentityService)
    // in LF.IdentityService, which never calls AddInfrastructureGrpcClient(), would crash on startup.
    public static IServiceCollection AddAuthenticationApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<ICourseAuthoringService, CourseAuthoringService>();
        services.AddScoped<IEnrollmentLearningService, EnrollmentLearningService>();
        services.AddScoped<IPromoCodeAdminService, PromoCodeAdminService>();
        services.AddScoped<IStorageService, StorageService>();

        return services;
    }

    public static IServiceCollection AddUserApplication(this IServiceCollection services)
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);

        services.AddScoped<IUserService, UserService>();

        return services;
    }

    public static IServiceCollection AddCourseApplication(this IServiceCollection services)
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IPromoCodeService, PromoCodeService>();

        return services;
    }
}
