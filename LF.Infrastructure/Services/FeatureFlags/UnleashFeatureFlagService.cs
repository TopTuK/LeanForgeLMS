using System.Globalization;
using LF.Application.Common.Interfaces;
using Unleash;

namespace LF.Infrastructure.Services.FeatureFlags;

// IUnleash evaluates against a background-polled in-memory cache, so IsEnabled does no I/O and
// the Task here always completes synchronously.
internal sealed class UnleashFeatureFlagService(IUnleash unleash) : IFeatureFlagService
{
    private readonly IUnleash _unleash = unleash;

    public Task<bool> IsEnabledAsync(string featureName, int? userId = null, CancellationToken cancellationToken = default)
    {
        // defaultSetting: false — an unknown flag, or one the SDK has not fetched yet, stays off.
        var enabled = userId is { } id
            ? _unleash.IsEnabled(featureName, new UnleashContext { UserId = id.ToString(CultureInfo.InvariantCulture) }, false)
            : _unleash.IsEnabled(featureName, false);

        return Task.FromResult(enabled);
    }
}
