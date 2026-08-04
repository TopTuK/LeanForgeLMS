using LF.Application.Common.Interfaces;
using LF.Application.Services.User;
using LF.IdentityService;
using LF.Infrastructure.Persistence;
using LF.Infrastructure.Persistence.Seed;
using LF.Infrastructure.Services.Identity;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
}
