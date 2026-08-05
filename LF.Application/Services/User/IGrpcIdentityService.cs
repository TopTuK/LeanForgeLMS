using LF.Application.ModelDto.Authentication;
using LF.Application.ModelDto.User;

namespace LF.Application.Services.User;

public interface IGrpcIdentityService
{
    Task<UserDto> GetOrCreateUserAsync(UserAuthentificationDto userAuthentification);
    Task<UserDto> EnsureUserWithRoleAsync(EnsureUserWithRoleDto userRequestDto);
}

