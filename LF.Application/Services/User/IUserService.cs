using LF.Application.ModelDto.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace LF.Application.Services.User
{
    public interface IUserService
    {
        Task<UserDto> GetOrCreateUserAsync(GetOrCreateUserDto userRequestDto);
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<UserDto?> UpdateUserNameAsync(int id, UpdateUserProfileDto dto);
        Task<UserDto?> UpdateUserAvatarAsync(int id, UpdateUserAvatarDto dto);
        Task<UserDto> EnsureUserWithRoleAsync(EnsureUserWithRoleDto userRequestDto);
        Task<PagedUsersDto> ListUsersAsync(int page, int pageSize, string? search);
        Task<UserDto?> UpdateUserRoleAsync(int id, UpdateUserRoleDto dto);
        Task<bool> DeleteUserAsync(int id);
    }
}
