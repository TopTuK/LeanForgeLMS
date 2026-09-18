using LF.Application.Common.Content;
using LF.Application.Common.Interfaces;
using LF.Application.Services.Admin;
using LF.Application.Services.Authentication;
using LF.Application.Services.Course;
using LF.Application.Services.CourseAuthoring;
using LF.Application.Services.CourseTeaching;
using LF.Application.Services.Enrollment;
using LF.Application.Services.EnrollmentLearning;
using LF.Application.Services.News;
using LF.Application.Services.Payment;
using LF.Application.Services.PaymentReporting;
using LF.Application.Services.Profile;
using LF.Application.Services.Promo;
using LF.Application.Services.PromoCodeAdmin;
using LF.Application.Services.Qna;
using LF.Application.Services.Storage;
using LF.Application.Services.User;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddScoped<IAdminCourseService, AdminCourseService>();
        services.AddScoped<ICourseAuthoringService, CourseAuthoringService>();
        services.AddScoped<IEnrollmentLearningService, EnrollmentLearningService>();
        services.AddScoped<IPromoCodeAdminService, PromoCodeAdminService>();
        services.AddScoped<IStorageService, StorageService>();

        // PaymentReportService reads and writes the shared DB directly and stamps timestamps.
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IPaymentReportService, PaymentReportService>();

        // News is owned by LF.WebApi (DB + MinIO in one process); admin-authored bodies are
        // sanitized here with the same allow-list LF.CourseService applies to lesson HTML.
        services.TryAddSingleton<IHtmlSanitizer, GanssHtmlSanitizer>();
        services.AddScoped<INewsService, NewsService>();
        services.AddScoped<IAdminNewsService, AdminNewsService>();

        // Lesson Q&A is owned by LF.WebApi for the same reason News is: it needs the shared DB plus
        // the course, lesson, enrollment and user tables in a single query, and none of the gRPC
        // hosts have a use for it.
        services.AddScoped<ILessonQuestionService, LessonQuestionService>();
        services.AddScoped<IAdminLessonQuestionService, AdminLessonQuestionService>();
        services.AddScoped<ICourseTeachingTeamService, CourseTeachingTeamService>();

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
        services.AddSingleton<IHtmlSanitizer, GanssHtmlSanitizer>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IPromoCodeService, PromoCodeService>();

        return services;
    }

    // Used by LF.PaymentService. IPaymentGateway is supplied by AddInfrastructureRobokassa().
    public static IServiceCollection AddPaymentApplication(this IServiceCollection services)
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IPaymentOrderService, PaymentOrderService>();

        return services;
    }
}
