using Microsoft.Extensions.Logging;
using Unleash;
using Unleash.ClientFactory;
using Unleash.Internal;

namespace LF.Infrastructure.Services.FeatureFlags;

internal static class UnleashClientBuilder
{
    public static IUnleash Build(UnleashOptions options, string appName, ILogger logger)
    {
        var settings = new UnleashSettings
        {
            AppName = appName,
            UnleashApi = new Uri(options.ApiUrl!),
            InstanceTag = string.IsNullOrWhiteSpace(options.InstanceTag) ? Environment.MachineName : options.InstanceTag,
            FetchTogglesInterval = TimeSpan.FromSeconds(options.FetchTogglesIntervalSeconds),
            CustomHttpHeaders = new Dictionary<string, string> { ["Authorization"] = options.ApiKey! },
        };

        void ConfigureEvents(EventCallbackConfig config)
        {
            // Background fetch failures are reported here rather than thrown; the client keeps
            // serving the last known toggle state (or the fail-closed default on a cold start).
            config.ErrorEvent = evt => logger.LogError(
                evt.Error, "Unleash: feature toggle client error {ErrorType} for {Resource}", evt.ErrorType, evt.Resource);

            config.TogglesUpdatedEvent = evt => logger.LogInformation(
                "Unleash: feature toggles updated at {UpdatedOn}", evt.UpdatedOn);
        }

        var factory = new UnleashClientFactory();

        try
        {
            // synchronousInitialization blocks on the first fetch (the SDK caps it at a 5s HTTP
            // timeout) so an instance never serves the fail-closed default to real traffic just
            // because it has only just booted. UnleashInitializer pays this cost during startup.
            return factory.CreateClient(settings, synchronousInitialization: true, ConfigureEvents);
        }
        catch (Exception ex) when (ex is UnleashException or HttpRequestException or OperationCanceledException or TimeoutException)
        {
            // The synchronous path *throws* when that first fetch fails — an unreachable server
            // gives TaskCanceledException, a rejected API key gives UnleashException. Feature flags
            // must never stop the host from booting, so fall back to a background-polling client:
            // every flag reads as off until a later poll succeeds, then it self-heals.
            logger.LogError(ex, "Unleash: initial toggle fetch failed; starting in background-polling mode with all flags off");

            return factory.CreateClient(settings, synchronousInitialization: false, ConfigureEvents);
        }
    }
}
