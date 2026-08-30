using LF.Application.Common.Interfaces;
using LF.Application.Services.Course;
using LF.Application.Services.Enrollment;
using LF.Application.Services.Promo;
using LF.Application.Services.Storage;
using LF.Application.Services.User;
using LF.IdentityService;
using LF.Infrastructure.Persistence;
using LF.Infrastructure.Persistence.Repositories;
using LF.Infrastructure.Persistence.Seed;
using LF.Infrastructure.Services.Identity;
using LF.Infrastructure.Services.Storage;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;

namespace LF.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("leanforge")
            ?? throw new InvalidOperationException("Connection string 'leanforge' not found.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.Configure<List<DefaultAdminEntry>>(configuration.GetSection("DefaultAdmins"));
        services.AddScoped<IStorageRepository, StorageRepository>();

        return services;
    }

    public static IServiceCollection AddInfrastructureGrpcClient(this IServiceCollection services, string identityServiceAddress)
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);

        services.AddGrpcClient<UserServiceRpc.UserServiceRpcClient>(options =>
        {
            options.Address = new Uri(identityServiceAddress);
        });
        services.AddScoped<IGrpcIdentityService, GrpcIdentityService>();

        return services;
    }

    public static IServiceCollection AddInfrastructureCourseGrpcClient(this IServiceCollection services, string courseServiceAddress)
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);

        services.AddGrpcClient<global::LF.CourseService.CourseServiceRpc.CourseServiceRpcClient>(options =>
        {
            options.Address = new Uri(courseServiceAddress);
        });
        services.AddScoped<IGrpcCourseService, Services.Course.GrpcCourseService>();
        services.AddScoped<IGrpcEnrollmentService, Services.Enrollment.GrpcEnrollmentService>();
        services.AddScoped<IGrpcPromoCodeService, Services.Promo.GrpcPromoCodeService>();

        return services;
    }

    public static IServiceCollection AddInfrastructureFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MinioStorageOptions>(configuration.GetSection("Minio"));

        services.AddScoped<IFileStorageService>(sp =>
        {
            var bucketName = sp.GetRequiredService<IOptions<MinioStorageOptions>>().Value.AvatarsBucketName;
            return new MinioFileStorageService(sp.GetRequiredService<ILogger<MinioFileStorageService>>(), sp.GetRequiredService<IMinioClient>(), bucketName);
        });

        services.AddKeyedScoped<IFileStorageService>("storage", (sp, _) =>
        {
            var bucketName = sp.GetRequiredService<IOptions<MinioStorageOptions>>().Value.StorageBucketName;
            return new MinioFileStorageService(sp.GetRequiredService<ILogger<MinioFileStorageService>>(), sp.GetRequiredService<IMinioClient>(), bucketName);
        });

        services.AddHostedService<MinioBucketInitializer>();

        return services;
    }
}
