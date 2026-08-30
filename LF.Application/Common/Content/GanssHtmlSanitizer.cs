using GanssXss = Ganss.Xss;
using LF.Application.Common.Interfaces;

namespace LF.Application.Common.Content;

/// <summary>
/// <see cref="IHtmlSanitizer"/> backed by Ganss.Xss, locked down to the tag/attribute set the
/// TipTap editor in <c>lf.webapp</c> can produce. Registered as a singleton — the underlying
/// sanitizer is thread-safe and the allow-lists never change at runtime.
/// </summary>
internal sealed class GanssHtmlSanitizer : IHtmlSanitizer
{
    private static readonly GanssXss.HtmlSanitizer Sanitizer = CreateSanitizer();

    public string Sanitize(string? html)
        => string.IsNullOrWhiteSpace(html) ? string.Empty : Sanitizer.Sanitize(html);

    private static GanssXss.HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new GanssXss.HtmlSanitizer();

        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
                 {
                     "p", "br", "span",
                     "h1", "h2", "h3",
                     "strong", "b", "em", "i", "u", "s", "mark", "sub", "sup",
                     "ul", "ol", "li",
                     "blockquote", "pre", "code", "hr",
                     "a", "img",
                 })
        {
            sanitizer.AllowedTags.Add(tag);
        }

        sanitizer.AllowedAttributes.Clear();
        foreach (var attr in new[] { "href", "target", "rel", "title", "src", "alt", "style" })
        {
            sanitizer.AllowedAttributes.Add(attr);
        }

        sanitizer.AllowedCssProperties.Clear();
        foreach (var prop in new[] { "color", "background-color", "text-align", "font-weight", "font-style", "text-decoration" })
        {
            sanitizer.AllowedCssProperties.Add(prop);
        }

        sanitizer.AllowedSchemes.Clear();
        foreach (var scheme in new[] { "http", "https", "mailto" })
        {
            sanitizer.AllowedSchemes.Add(scheme);
        }

        sanitizer.AllowedAtRules.Clear();

        // Force safe link semantics regardless of what the author submitted.
        sanitizer.PostProcessNode += (_, e) =>
        {
            if (e.Node is not AngleSharp.Dom.IElement element) return;
            if (!string.Equals(element.TagName, "A", StringComparison.OrdinalIgnoreCase)) return;

            element.SetAttribute("target", "_blank");
            element.SetAttribute("rel", "noopener noreferrer");
        };

        return sanitizer;
    }
}
