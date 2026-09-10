using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unleash;

namespace LF.Infrastructure.Services.FeatureFlags;

// IUnleash is a lazily-constructed singleton whose construction performs the first (blocking)
// toggle fetch. Resolving it here moves that cost into startup, so the first real request already
// sees live flag state. Mirrors MinioBucketInitializer's role for buckets.
internal sealed class UnleashInitializer(ILogger<UnleashInitializer> logger, IUnleash unleash) : IHostedService
{
    private readonly ILogger<UnleashInitializer> _logger = logger;
    private readonly IUnleash _unleash = unleash;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("UnleashInitializer::StartAsync: feature flag client ready with {ToggleCount} known toggles",
            _unleash.ListKnownToggles().Count);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
