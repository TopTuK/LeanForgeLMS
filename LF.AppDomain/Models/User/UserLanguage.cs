namespace LF.AppDomain.Models.User;

// UI/email languages the platform ships translations for. Codes match lf.webapp's vue-i18n locales.
public static class UserLanguage
{
    public const string Russian = "ru";
    public const string English = "en";

    // Used when a user never picked a language. Matches the SPA's initial locale.
    public const string Default = Russian;

    public const int MaxLength = 8;

    public static readonly IReadOnlyList<string> Supported = [Russian, English];

    /// <returns>The canonical code, or null when the value is not a supported language.</returns>
    public static string? Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var normalized = code.Trim().ToLowerInvariant();
        return Supported.Contains(normalized) ? normalized : null;
    }

    public static string OrDefault(string? code) => Normalize(code) ?? Default;
}
