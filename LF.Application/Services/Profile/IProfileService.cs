using LF.Application.ModelDto.User;

namespace LF.Application.Services.Profile;

public interface IProfileService
{
    Task<UserDto?> GetProfileAsync(int userId);
    Task<UserDto?> UpdateProfileAsync(int userId, UpdateUserNameDto dto);
    Task<UserDto?> UpdateAvatarAsync(int userId, string? avatarKey);
}
