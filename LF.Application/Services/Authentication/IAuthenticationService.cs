using LF.Application.ModelDto.Authentication;
using LF.Application.ModelDto.User;

namespace LF.Application.Services.Authentication;

public interface IAuthenticationService
{
    Task<UserDto> AuthenticatePmiUserAsync(UserAuthentificationDto userAuthentification);
    Task<UserDto> AuthenticateDevUserAsync(EnsureUserWithRoleDto userRequestDto);
}

