using LF.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LF.Infrastructure.Services.FeatureFlags;

// Registered when Unleash is not configured, so the DI graph still validates on Build() and every
// flag reads as off. Mirrors the old "missing PlatformSettings row means disabled" fail-safe.
internal sealed class DisabledFeatureFlagService(ILogger<DisabledFeatureFlagService> logger) : IFeatureFlagService
{
    private readonly ILogger<DisabledFeatureFlagService> _logger = logger;

    public Task<bool> IsEnabledAsync(string featureName, int? userId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("DisabledFeatureFlagService::IsEnabledAsync: Unleash is not configured, reporting {FeatureName} as disabled", featureName);

        return Task.FromResult(false);
    }
}
