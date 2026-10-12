using LF.Application.Common.Content;
using LF.Application.Common.Documents;

namespace LF.ApplicationTests.Common.Content;

public class LessonHtmlBlockParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyHtml_ReturnsNoBlocks(string? html)
    {
        // Act
        var blocks = LessonHtmlBlockParser.Parse(html);

        // Assert
        Assert.Empty(blocks);
    }

    [Fact]
    public void Parse_ParagraphWithNestedFormatting_ProducesStyledRuns()
    {
        // Act
        var blocks = LessonHtmlBlockParser.Parse("<p>Привет, <strong>жирный <em>и курсив</em></strong> текст</p>");

        // Assert
        var paragraph = Assert.IsType<ParagraphBlock>(Assert.Single(blocks));
        Assert.Collection(paragraph.Runs,
            r => Assert.Equal(new TextRun("Привет, "), r),
            r => Assert.Equal(new TextRun("жирный ", Bold: true), r),
            r => Assert.Equal(new TextRun("и курсив", Bold: true, Italic: true), r),
            r => Assert.Equal(new TextRun(" текст"), r));
    }

    [Fact]
    public void Parse_CollapsesWhitespaceAndTrimsEdges()
    {
        // Act
        var blocks = LessonHtmlBlockParser.Parse("<p>\n   one\n\n   two   </p>");

        // Assert
        var paragraph = Assert.IsType<ParagraphBlock>(Assert.Single(blocks));
        Assert.Equal("one two", string.Concat(paragraph.Runs.Select(r => r.Text)));
    }

    [Fact]
    public void Parse_Headings_KeepTheirLevel()
    {
        // Act
        var blocks = LessonHtmlBlockParser.Parse("<h1>A</h1><h2>B</h2><h3>C</h3>");

        // Assert
        Assert.Equal([1, 2, 3], blocks.Cast<HeadingBlock>().Select(h => h.Level));
    }

    [Fact]
    public void Parse_Lists_ProduceOneItemPerLi()
    {
        // Act
        var blocks = LessonHtmlBlockParser.Parse("<ol><li><p>first</p></li><li><p>second</p></li></ol><ul><li>dot</li></ul>");

        // Assert
        Assert.Collection(blocks,
            b =>
            {
                var list = Assert.IsType<ListBlock>(b);
                Assert.True(list.Ordered);
                Assert.Equal(2, list.Items.Count);
                Assert.Equal("second", Assert.IsType<ParagraphBlock>(Assert.Single(list.Items[1])).Runs[0].Text);
            },
            b => Assert.False(Assert.IsType<ListBlock>(b).Ordered));
    }

    [Fact]
    public void Parse_QuoteCodeAndRule_MapToTheirBlocks()
    {
        // Act
        var blocks = LessonHtmlBlockParser.Parse("<blockquote><p>q</p></blockquote><pre><code>var x = 1;\n</code></pre><hr>");

        // Assert
        Assert.Collection(blocks,
            b => Assert.IsType<ParagraphBlock>(Assert.Single(Assert.IsType<QuoteBlock>(b).Blocks)),
            b => Assert.Equal("var x = 1;", Assert.IsType<CodeBlock>(b).Text),
            b => Assert.IsType<RuleBlock>(b));
    }

    [Fact]
    public void Parse_Table_MarksHeaderRowsAndKeepsCells()
    {
        // Act
        var blocks = LessonHtmlBlockParser.Parse(
            "<table><tbody><tr><th><p>Name</p></th><th><p>Value</p></th></tr><tr><td><p>a</p></td><td><p>1</p></td></tr></tbody></table>");

        // Assert
        var table = Assert.IsType<TableBlock>(Assert.Single(blocks));
        Assert.True(table.Rows[0].IsHeader);
        Assert.False(table.Rows[1].IsHeader);
        Assert.Equal("1", Assert.Single(table.Rows[1].Cells[1]).Text);
    }

    [Fact]
    public void Parse_Link_KeepsSafeUrlOnly()
    {
        // Act
        var blocks = LessonHtmlBlockParser.Parse("<p><a href=\"https://example.com/x\">ok</a> <a href=\"javascript:alert(1)\">bad</a></p>");

        // Assert
        var runs = Assert.IsType<ParagraphBlock>(Assert.Single(blocks)).Runs;
        Assert.Equal("https://example.com/x", runs.Single(r => r.Text == "ok").LinkUrl);
        Assert.Null(runs.Single(r => r.Text == "bad").LinkUrl);
    }

    [Fact]
    public void Parse_Images_KeepsDataUriAndNeverRemoteSources()
    {
        // Arrange
        var bytes = new byte[] { 1, 2, 3 };
        var html = $"<p>text<img src=\"https://evil.test/a.png\"><img src=\"data:image/png;base64,{Convert.ToBase64String(bytes)}\"></p>";

        // Act
        var blocks = LessonHtmlBlockParser.Parse(html);

        // Assert
        Assert.Collection(blocks,
            b => Assert.IsType<ParagraphBlock>(b),
            b => Assert.Equal(bytes, Assert.IsType<ImageBlock>(b).Data));
    }

    [Fact]
    public void Parse_LineBreak_SplitsRuns()
    {
        // Act
        var blocks = LessonHtmlBlockParser.Parse("<p>one<br>two</p>");

        // Assert
        var runs = Assert.IsType<ParagraphBlock>(Assert.Single(blocks)).Runs;
        Assert.Equal(["one", "\n", "two"], runs.Select(r => r.Text));
    }
}
