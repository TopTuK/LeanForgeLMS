using LF.Application.Common.Email;

namespace LF.ApplicationTests.Common.Email;

public class EmailTemplateRendererTests
{
    private static readonly EmailTemplateRenderer Renderer = new();

    private static Dictionary<string, string> Values(string courseTitle = "Lean <Basics> & more") => new()
    {
        ["Name"] = "Ivan",
        ["CourseTitle"] = courseTitle,
        ["MyCoursesUrl"] = "https://lms.example.com/courses/active",
    };

    [Fact]
    public void Render_Russian_UsesRussianTemplate()
    {
        var email = Renderer.Render(EmailTemplates.EnrollmentConfirmation, "ru", Values("Основы"));

        Assert.Equal("Вы записаны на курс «Основы»", email.Subject);
        Assert.Contains("Здравствуйте, Ivan!", email.HtmlBody);
        Assert.NotNull(email.TextBody);
        Assert.Contains("Здравствуйте, Ivan!", email.TextBody);
    }

    [Fact]
    public void Render_English_UsesEnglishTemplate()
    {
        var email = Renderer.Render(EmailTemplates.EnrollmentConfirmation, "en", Values("Basics"));

        Assert.Equal("You're enrolled in “Basics”", email.Subject);
        Assert.Contains("Hi Ivan,", email.HtmlBody);
        Assert.Contains("https://lms.example.com/courses/active", email.TextBody);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("de")]
    public void Render_MissingOrUnsupportedLanguage_FallsBackToRussian(string? language)
    {
        var email = Renderer.Render(EmailTemplates.EnrollmentConfirmation, language, Values("Основы"));

        Assert.StartsWith("Вы записаны", email.Subject);
    }

    [Fact]
    public void Render_EncodesValuesInHtmlButNotInSubjectOrText()
    {
        var email = Renderer.Render(EmailTemplates.EnrollmentConfirmation, "en", Values());

        Assert.Contains("Lean &lt;Basics&gt; &amp; more", email.HtmlBody);
        Assert.DoesNotContain("<Basics>", email.HtmlBody);
        Assert.Equal("You're enrolled in “Lean <Basics> & more”", email.Subject);
        Assert.Contains("Lean <Basics> & more", email.TextBody);
    }

    [Fact]
    public void Render_LeavesNoPlaceholdersBehind()
    {
        foreach (var language in new[] { "ru", "en" })
        {
            var email = Renderer.Render(EmailTemplates.EnrollmentConfirmation, language, Values());

            Assert.DoesNotContain("{{", email.HtmlBody);
            Assert.DoesNotContain("{{", email.TextBody);
            Assert.DoesNotContain("{{", email.Subject);
        }
    }

    [Fact]
    public void Render_MissingValue_Throws()
    {
        var values = Values();
        values.Remove("MyCoursesUrl");

        Assert.Throws<InvalidOperationException>(() => Renderer.Render(EmailTemplates.EnrollmentConfirmation, "en", values));
    }

    [Fact]
    public void Render_UnknownTemplate_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Renderer.Render("DoesNotExist", "en", Values()));
    }

    [Fact]
    public void Render_TriplePlaceholder_InsertsMarkupVerbatim()
    {
        var email = Renderer.Render(EmailTemplates.CourseUpdated, "en", new Dictionary<string, string>
        {
            ["Name"] = "Ann",
            ["CourseTitle"] = "Lean <Basics>",
            ["CourseUrl"] = "https://lms.example.com/courses/learn/5",
            ["ChangesHtml"] = "<ul><li>Kaizen</li></ul>",
            ["ChangesText"] = "- Kaizen",
        });

        Assert.Contains("<ul><li>Kaizen</li></ul>", email.HtmlBody);
        Assert.Contains("Lean &lt;Basics&gt;", email.HtmlBody);
        Assert.Contains("- Kaizen", email.TextBody);
        Assert.DoesNotContain("{{", email.HtmlBody);
    }
}
