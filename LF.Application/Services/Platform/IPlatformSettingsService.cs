using LF.Application.ModelDto.Platform;

namespace LF.Application.Services.Platform;

public interface IPlatformSettingsService
{
    Task<PlatformSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task<bool> IsStudentEnrollmentEnabledAsync(CancellationToken cancellationToken = default);
    Task<PlatformSettingsDto> SetStudentEnrollmentEnabledAsync(bool enabled, int adminUserId, CancellationToken cancellationToken = default);
}
