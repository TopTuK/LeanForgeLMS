using LF.Application.ModelDto.User;
using LF.Application.Services.User;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Profile;

internal sealed class ProfileService(ILogger<ProfileService> logger, IGrpcIdentityService identityService)
    : IProfileService
{
    private readonly ILogger<ProfileService> _logger = logger;
    private readonly IGrpcIdentityService _identityService = identityService;

    public async Task<UserDto?> GetProfileAsync(int userId)
    {
        _logger.LogInformation("ProfileService::GetProfileAsync: called with UserId={usrId}", userId);

        return await _identityService.GetUserProfileAsync(userId);
    }

    public async Task<UserDto?> UpdateProfileAsync(int userId, UpdateUserProfileDto dto)
    {
        _logger.LogInformation("ProfileService::UpdateProfileAsync: called with UserId={usrId}", userId);

        return await _identityService.UpdateUserProfileAsync(userId, dto);
    }

    public async Task<UserDto?> UpdateAvatarAsync(int userId, string? avatarKey)
    {
        _logger.LogInformation("ProfileService::UpdateAvatarAsync: called with UserId={usrId}", userId);

        return await _identityService.UpdateUserAvatarAsync(userId, avatarKey);
    }
}
