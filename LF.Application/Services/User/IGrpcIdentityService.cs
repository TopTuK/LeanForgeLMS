using LF.Application.ModelDto.Authentication;
using LF.Application.ModelDto.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace LF.Application.Services.User
{
    public interface IGrpcIdentityService
    {
        Task<UserDto> GetOrCreateUserAsync(UserAuthentificationDto userAuthentification);
        Task<UserDto?> GetUserProfileAsync(int userId);
        Task<UserDto?> UpdateUserProfileAsync(int userId, UpdateUserProfileDto dto);
        Task<UserDto?> UpdateUserAvatarAsync(int userId, string? avatarKey);
        Task<UserDto> EnsureUserWithRoleAsync(EnsureUserWithRoleDto userRequestDto);
        Task<PagedUsersDto> ListUsersAsync(int page, int pageSize, string? search);
        Task<UserDto?> UpdateUserRoleAsync(int userId, UpdateUserRoleDto dto);
        Task<bool> DeleteUserAsync(int userId);
    }
}
