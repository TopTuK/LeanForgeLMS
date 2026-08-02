using LF.Application.Services.Authentication;
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

        return services;
    }

    public static IServiceCollection AddUserApplication(this IServiceCollection services)
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);

        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
