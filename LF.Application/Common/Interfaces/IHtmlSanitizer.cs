namespace LF.Application.Common.Interfaces;

/// <summary>
/// Removes script-bearing markup from author-supplied rich text before it is persisted.
/// The stored value is the security boundary — a compromised/escalated author account can
/// POST arbitrary HTML straight to the course-authoring endpoints, bypassing the editor.
/// </summary>
public interface IHtmlSanitizer
{
    /// <summary>Returns <paramref name="html"/> with disallowed tags, attributes and URI schemes stripped. Null/blank input returns <see cref="string.Empty"/>.</summary>
    string Sanitize(string? html);
}
