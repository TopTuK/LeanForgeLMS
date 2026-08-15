using System.Security.Claims;
using LF.AppDomain.Models.User.Enums;
using LF.Application.ModelDto.Authentication;
using LF.Application.ModelDto.User;
using LF.WebApi.Models.Options;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using LFAppAuth = LF.Application.Services.Authentication;

namespace LF.WebApi.Endpoints;

/// <summary>
/// Development/testing-only login shortcuts for fixed Student, Instructor, CourseCreator and Admin
/// personas. Never registered outside Development — see the environment check in <see cref="Map"/>.
/// </summary>
public sealed class DevAuthEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        var env = app.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (!env.IsDevelopment())
            return;

        var group = app.MapGroup("/api/dev-auth").WithTags("DevAuth");

        group.MapGet("/{role}", async Task<Results<RedirectHttpResult, ValidationProblem>>
            (string role, HttpContext httpContext,
             IOptionsSnapshot<DevAuthOptions> devAuthOptions,
             IOptionsSnapshot<DefaultAuthOptions> authOptions,
             LFAppAuth.IAuthenticationService authenticationService,
             LFAppAuth.ITokenService tokenService) =>
        {
            if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var userRole)
                || userRole is not (UserRole.Student or UserRole.Instructor or UserRole.CourseCreator or UserRole.Admin))
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["role"] = ["role must be one of: Student, Instructor, CourseCreator, Admin."],
                });
            }

            var persona = devAuthOptions.Value.GetPersona(userRole);

            var userDto = await authenticationService.AuthenticateDevUserAsync(new EnsureUserWithRoleDto
            {
                Email = persona.Email,
                FirstName = persona.FirstName,
                LastName = persona.LastName,
                Role = userRole,
            });

            var options = authOptions.Value;
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userDto.Id.ToString()),
                new("email", userDto.Email),
                new("role", userDto.Role.ToString()),
            };

            var jwtToken = tokenService.CreateWebJwtToken(claims, new JwtTokenConfigDto
            {
                Issuer = options.JwtIssuer,
                Audience = options.JwtAudience,
                Key = options.JwtKey,
                ExpiresDays = options.JwtExpiresDays,
            });

            httpContext.Response.Cookies.Append(
                key: options.AuthCookieName,
                value: jwtToken.Token,
                options: new CookieOptions
                {
                    MaxAge = TimeSpan.FromDays(options.AuthMaxAgeDays),
                });

            return TypedResults.LocalRedirect("/courses");
        });
    }
}
