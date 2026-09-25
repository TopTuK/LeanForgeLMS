namespace LF.Application.Common.Email;

public interface IEmailTemplateRenderer
{
    // Falls back to UserLanguage.Default when no template exists for the requested language.
    RenderedEmail Render(string templateName, string? language, IReadOnlyDictionary<string, string> values);
}

public sealed record RenderedEmail(string Subject, string HtmlBody, string? TextBody);

public static class EmailTemplates
{
    // Templates/Email/EnrollmentConfirmation.{lang}.html|txt — placeholders: Name, CourseTitle, MyCoursesUrl.
    public const string EnrollmentConfirmation = "EnrollmentConfirmation";

    // Templates/Email/CourseUpdated.{lang}.html|txt — placeholders: Name, CourseTitle, CourseUrl,
    // {{{ChangesHtml}}} (pre-encoded list markup), ChangesText.
    public const string CourseUpdated = "CourseUpdated";
}
