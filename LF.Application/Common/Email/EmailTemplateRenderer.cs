using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using LF.AppDomain.Models.User;

namespace LF.Application.Common.Email;

// Templates are embedded resources (Templates/Email/{Name}.{lang}.html, optional .txt). The subject is
// the HTML <title>. Values are HTML-encoded in the HTML body and inserted verbatim into subject and text.
internal sealed partial class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private const string ResourcePrefix = "LF.EmailTemplates.";

    private static readonly Assembly TemplateAssembly = typeof(EmailTemplateRenderer).Assembly;
    private static readonly ConcurrentDictionary<string, string?> Cache = new();

    public RenderedEmail Render(string templateName, string? language, IReadOnlyDictionary<string, string> values)
    {
        var htmlLanguage = UserLanguage.OrDefault(language);
        var html = Load(templateName, htmlLanguage, "html");
        if (html is null)
        {
            htmlLanguage = UserLanguage.Default;
            html = Load(templateName, htmlLanguage, "html")
                ?? throw new InvalidOperationException($"Email template '{templateName}' has no HTML body for '{language}' or '{UserLanguage.Default}'.");
        }

        // The text part always comes from the same language as the HTML, never a mix.
        var text = Load(templateName, htmlLanguage, "txt");

        var titleMatch = TitleRegex().Match(html);
        if (!titleMatch.Success)
            throw new InvalidOperationException($"Email template '{templateName}.{htmlLanguage}.html' has no <title> to use as the subject.");

        var subject = Substitute(WebUtility.HtmlDecode(titleMatch.Groups[1].Value).Trim(), values, encode: false, templateName);

        return new RenderedEmail(
            subject,
            Substitute(html, values, encode: true, templateName),
            text is null ? null : Substitute(text, values, encode: false, templateName));
    }

    private static string? Load(string templateName, string language, string extension) =>
        Cache.GetOrAdd($"{ResourcePrefix}{templateName}.{language}.{extension}", static resourceName =>
        {
            using var stream = TemplateAssembly.GetManifestResourceStream(resourceName);
            if (stream is null)
                return null;

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        });

    // {{Key}} is HTML-encoded in the HTML body. {{{Key}}} is inserted verbatim: only for markup the
    // caller built itself from already-encoded values (e.g. a list of lesson titles).
    private static string Substitute(string template, IReadOnlyDictionary<string, string> values, bool encode, string templateName) =>
        PlaceholderRegex().Replace(template, match =>
        {
            var isRaw = match.Groups["raw"].Success;
            var key = isRaw ? match.Groups["raw"].Value : match.Groups["key"].Value;
            if (!values.TryGetValue(key, out var value))
                throw new InvalidOperationException($"Email template '{templateName}' uses placeholder '{key}' but no value was supplied.");

            return encode && !isRaw ? WebUtility.HtmlEncode(value) : value;
        });

    [GeneratedRegex(@"\{\{\{(?<raw>\w+)\}\}\}|\{\{(?<key>\w+)\}\}")]
    private static partial Regex PlaceholderRegex();

    [GeneratedRegex(@"<title>(.*?)</title>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex TitleRegex();
}
