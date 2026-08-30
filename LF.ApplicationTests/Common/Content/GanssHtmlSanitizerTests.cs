using LF.Application.Common.Content;

namespace LF.ApplicationTests.Common.Content;

public class GanssHtmlSanitizerTests
{
    private readonly GanssHtmlSanitizer _sanitizer = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sanitize_BlankInput_ReturnsEmpty(string? input)
        => Assert.Equal(string.Empty, _sanitizer.Sanitize(input));

    [Fact]
    public void Sanitize_KeepsAllowedFormatting()
    {
        const string html = "<h2>Title</h2><p><strong>bold</strong> <em>x</em></p><ul><li>one</li></ul>";
        Assert.Equal(html, _sanitizer.Sanitize(html));
    }

    [Fact]
    public void Sanitize_StripsScriptTag()
    {
        var result = _sanitizer.Sanitize("<p>hi</p><script>alert(1)</script>");
        Assert.Equal("<p>hi</p>", result);
        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_StripsInlineEventHandlers()
        => Assert.DoesNotContain("onerror", _sanitizer.Sanitize("<img src=\"x\" onerror=\"alert(1)\">"), StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Sanitize_DropsJavascriptHref()
        => Assert.DoesNotContain("javascript:", _sanitizer.Sanitize("<a href=\"javascript:alert(1)\">x</a>"), StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Sanitize_RemovesDisallowedElements()
        => Assert.Equal("<p>ok</p>", _sanitizer.Sanitize("<iframe src=\"https://evil.test\"></iframe><style>*{}</style><p>ok</p>"));

    [Fact]
    public void Sanitize_ForcesSafeLinkAttributes()
    {
        var result = _sanitizer.Sanitize("<a href=\"https://example.com\">x</a>");
        Assert.Contains("rel=\"noopener noreferrer\"", result);
        Assert.Contains("target=\"_blank\"", result);
    }

    [Fact]
    public void Sanitize_KeepsHttpImages()
        => Assert.Contains("src=\"https://cdn.test/a.png\"", _sanitizer.Sanitize("<img src=\"https://cdn.test/a.png\" alt=\"a\">"));
}
