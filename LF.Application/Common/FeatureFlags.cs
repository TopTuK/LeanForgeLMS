namespace LF.Application.Common;

// Flag names as configured in Unleash. Kept here so both the enforcement point and the
// SPA-facing probe reference the same literal.
public static class FeatureFlags
{
    public const string SelfEnrollment = "lf.self_enrollment";
}
