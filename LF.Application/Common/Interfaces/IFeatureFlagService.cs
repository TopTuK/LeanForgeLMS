namespace LF.Application.Common.Interfaces;

// Runtime feature flags, owned by an external provider (Unleash) rather than application state.
// Implementations must fail closed: an unreachable or unconfigured provider reports every flag
// as disabled rather than throwing.
public interface IFeatureFlagService
{
    // userId, when supplied, is handed to the provider as the evaluation context so that
    // per-user and gradual-rollout strategies work without changing this signature.
    Task<bool> IsEnabledAsync(string featureName, int? userId = null, CancellationToken cancellationToken = default);
}
