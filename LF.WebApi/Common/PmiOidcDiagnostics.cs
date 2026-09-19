using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;

namespace LF.WebApi.Common;

// Diagnostics for the PMI Club OIDC login. The code exchange is done by hand in Program.cs, so these
// helpers let each step log enough to tell an expired, replayed or mismatched authorization code apart.
internal static class PmiOidcDiagnostics
{
    private const string ChallengeStartedKey = "lf.pmi.challenge_started_utc";

    public static ILogger Logger(HttpContext httpContext) =>
        httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("LF.WebApi.PmiOidc");

    public static TimeProvider Clock(HttpContext httpContext) =>
        httpContext.RequestServices.GetService<TimeProvider>() ?? TimeProvider.System;

    // The code is a one-time secret: log a short hash so a replayed callback shows up as the same
    // fingerprint twice without the code itself ever reaching the logs.
    public static string Fingerprint(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "<none>";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash, 0, 4).ToLowerInvariant();
    }

    // Properties are serialized into the OIDC state parameter after RedirectToIdentityProvider runs,
    // so a value stored here comes back on the callback.
    public static void MarkChallengeStarted(AuthenticationProperties properties, TimeProvider clock) =>
        properties.Items[ChallengeStartedKey] = clock.GetUtcNow().ToString("O", CultureInfo.InvariantCulture);

    public static double? MillisecondsSinceChallenge(AuthenticationProperties? properties, TimeProvider clock)
    {
        if (properties?.Items.TryGetValue(ChallengeStartedKey, out var raw) is not true
            || !DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var startedAt))
        {
            return null;
        }

        return Math.Round((clock.GetUtcNow() - startedAt).TotalMilliseconds);
    }
}
