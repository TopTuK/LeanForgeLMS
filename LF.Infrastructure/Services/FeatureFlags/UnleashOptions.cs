namespace LF.Infrastructure.Services.FeatureFlags;

internal sealed class UnleashOptions
{
    public const string SectionName = "Unleash";

    // Client API root. The SDK appends "client/features", so this must include the /api/ segment.
    public string? ApiUrl { get; set; }

    // Client API token, e.g. "default:development.<secret>". Never committed — supplied via
    // user-secrets in dev and Unleash__ApiKey in deployed environments.
    public string? ApiKey { get; set; }

    public int FetchTogglesIntervalSeconds { get; set; } = 15;

    // Distinguishes instances of the same app in the Unleash UI. Defaults to the machine name.
    public string? InstanceTag { get; set; }

    // The placeholder appsettings.json ships, matching the convention used for Robokassa/OAuth secrets.
    private const string Placeholder = "CHANGE_ME";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiUrl)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.Equals(ApiKey, Placeholder, StringComparison.Ordinal);
}
