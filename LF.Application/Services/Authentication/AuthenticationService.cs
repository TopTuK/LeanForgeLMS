using LF.Application.ModelDto.Authentication;
using LF.Application.ModelDto.User;
using LF.Application.Services.User;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Authentication;

internal sealed class AuthenticationService(ILogger<AuthenticationService> logger, IGrpcIdentityService identityService)
    : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger = logger;
    private readonly IGrpcIdentityService _identityService = identityService;

    public async Task<UserDto> AuthenticatePmiUserAsync(UserAuthentificationDto userAuthentification)
    {
        _logger.LogInformation("AuthenticationService::AuthenticatePmiUserAsync:Authenticating PMI user with email: {Email}",
            userAuthentification.Email);

        var userDto = await _identityService.GetOrCreateUserAsync(userAuthentification);
        _logger.LogInformation("AuthenticationService::AuthenticatePmiUserAsync:Successfully authenticated PMI user with email: {Email}",
            userAuthentification.Email);

        return userDto;
    }

    public async Task<UserDto> AuthenticateDevUserAsync(EnsureUserWithRoleDto userRequestDto)
    {
        _logger.LogInformation(
            "AuthenticationService::AuthenticateDevUserAsync: Authenticating dev user Email={Email} Role={Role}",
            userRequestDto.Email,
            userRequestDto.Role);

        var userDto = await _identityService.EnsureUserWithRoleAsync(userRequestDto);

        _logger.LogInformation(
            "AuthenticationService::AuthenticateDevUserAsync: Successfully authenticated dev user Email={Email} Role={Role}",
            userDto.Email,
            userDto.Role);

        return userDto;
    }
}

