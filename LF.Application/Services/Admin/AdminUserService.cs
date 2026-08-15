using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.ModelDto.User;
using LF.Application.Services.User;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Admin;

internal sealed class AdminUserService(ILogger<AdminUserService> logger, IGrpcIdentityService identityService) : IAdminUserService
{
    private readonly ILogger<AdminUserService> _logger = logger;
    private readonly IGrpcIdentityService _identityService = identityService;

    public async Task<PagedUsersDto> ListUsersAsync(int page, int pageSize, string? search)
    {
        _logger.LogInformation("AdminUserService::ListUsersAsync: called with Page={Page} PageSize={PageSize} Search={Search}", page, pageSize, search);

        return await _identityService.ListUsersAsync(page, pageSize, search);
    }

    public async Task<UserDto?> UpdateUserInfoAsync(int targetUserId, UpdateUserProfileDto dto)
    {
        _logger.LogInformation("AdminUserService::UpdateUserInfoAsync: called with TargetUserId={usrId}", targetUserId);

        return await _identityService.UpdateUserProfileAsync(targetUserId, dto);
    }

    public async Task<UserDto?> UpdateUserRoleAsync(int targetUserId, int actingAdminId, UserRole role)
    {
        _logger.LogInformation(
            "AdminUserService::UpdateUserRoleAsync: called with TargetUserId={usrId} ActingAdminId={adminId} Role={Role}",
            targetUserId, actingAdminId, role);

        if (targetUserId == actingAdminId)
            throw new SelfAdministrationException("Admins cannot change their own role.");

        return await _identityService.UpdateUserRoleAsync(targetUserId, new UpdateUserRoleDto { Role = role });
    }

    public async Task<bool> DeleteUserAsync(int targetUserId, int actingAdminId)
    {
        _logger.LogInformation("AdminUserService::DeleteUserAsync: called with TargetUserId={usrId} ActingAdminId={adminId}", targetUserId, actingAdminId);

        if (targetUserId == actingAdminId)
            throw new SelfAdministrationException("Admins cannot delete their own account.");

        return await _identityService.DeleteUserAsync(targetUserId);
    }
}
