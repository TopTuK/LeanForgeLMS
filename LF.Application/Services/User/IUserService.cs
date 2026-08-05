using LF.Application.ModelDto.User;

namespace LF.Application.Services.User;

public interface IUserService
{
    Task<UserDto> GetOrCreateUserAsync(GetOrCreateUserDto userRequestDto);
    Task<UserDto> EnsureUserWithRoleAsync(EnsureUserWithRoleDto userRequestDto);
}

