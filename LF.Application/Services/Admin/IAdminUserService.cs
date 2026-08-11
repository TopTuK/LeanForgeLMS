using LF.AppDomain.Models.User.Enums;
using LF.Application.ModelDto.User;

namespace LF.Application.Services.Admin;

public interface IAdminUserService
{
    Task<PagedUsersDto> ListUsersAsync(int page, int pageSize, string? search);
    Task<UserDto?> UpdateUserInfoAsync(int targetUserId, UpdateUserNameDto dto);
    Task<UserDto?> UpdateUserRoleAsync(int targetUserId, int actingAdminId, UserRole role);
    Task<bool> DeleteUserAsync(int targetUserId, int actingAdminId);
}
