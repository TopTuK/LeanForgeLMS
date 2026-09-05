using LF.AppDomain.Entities.Platform;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Platform;

internal sealed class PlatformSettingsService(
    ILogger<PlatformSettingsService> logger,
    IAppDbContext dbContext,
    TimeProvider timeProvider) : IPlatformSettingsService
{
    private readonly ILogger<PlatformSettingsService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<PlatformSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PlatformSettingsService::GetAsync: called");

        var settings = await _dbContext.PlatformSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == PlatformSettings.SingletonId, cancellationToken);

        return settings is null
            ? new PlatformSettingsDto { StudentEnrollmentEnabled = false, UpdatedAt = default }
            : ToDto(settings);
    }

    public async Task<bool> IsStudentEnrollmentEnabledAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.PlatformSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == PlatformSettings.SingletonId, cancellationToken);

        // Missing row is treated as "disabled" — fail safe for the Robokassa review.
        return settings?.StudentEnrollmentEnabled ?? false;
    }

    public async Task<PlatformSettingsDto> SetStudentEnrollmentEnabledAsync(bool enabled, int adminUserId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PlatformSettingsService::SetStudentEnrollmentEnabledAsync: called with Enabled={Enabled} AdminUserId={AdminUserId}",
            enabled, adminUserId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var settings = await _dbContext.PlatformSettings
            .FirstOrDefaultAsync(s => s.Id == PlatformSettings.SingletonId, cancellationToken);

        if (settings is null)
        {
            settings = PlatformSettings.CreateDefault(now);
            _dbContext.PlatformSettings.Add(settings);
        }

        settings.SetStudentEnrollmentEnabled(enabled, adminUserId, now);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(settings);
    }

    private static PlatformSettingsDto ToDto(PlatformSettings settings) => new()
    {
        StudentEnrollmentEnabled = settings.StudentEnrollmentEnabled,
        UpdatedAt = settings.UpdatedAt,
        UpdatedByUserId = settings.UpdatedByUserId,
    };
}
