using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using LF.Application.Common.Documents;

namespace LF.Application.Common.Content;

/// <summary>
/// Turns sanitized lesson HTML (the <see cref="GanssHtmlSanitizer"/> allow-list) into printable
/// <see cref="DocumentBlock"/>s. Unknown elements are unwrapped so their text survives. Remote
/// <c>&lt;img&gt;</c> sources are never fetched; only inline <c>data:image</c> URIs are kept.
/// </summary>
internal static partial class LessonHtmlBlockParser
{
    private static readonly HtmlParser Parser = new();

    public static IReadOnlyList<DocumentBlock> Parse(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var document = Parser.ParseDocument(html);
        return document.Body is null ? [] : ParseBlocks(document.Body);
    }

    private static List<DocumentBlock> ParseBlocks(INode parent)
    {
        var blocks = new List<DocumentBlock>();
        var pendingRuns = new List<TextRun>();
        var pendingImages = new List<ImageBlock>();

        void FlushParagraph()
        {
            var runs = Normalize(pendingRuns);
            if (runs.Count > 0) blocks.Add(new ParagraphBlock(runs));
            blocks.AddRange(pendingImages);
            pendingRuns.Clear();
            pendingImages.Clear();
        }

        foreach (var node in parent.ChildNodes)
        {
            if (node is not IElement element)
            {
                CollectRuns(node, default, pendingRuns, pendingImages);
                continue;
            }

            switch (element.LocalName)
            {
                case "p":
                    FlushParagraph();
                    CollectRuns(element, default, pendingRuns, pendingImages);
                    FlushParagraph();
                    break;

                case "h1" or "h2" or "h3":
                    FlushParagraph();
                    var images = new List<ImageBlock>();
                    var headingRuns = new List<TextRun>();
                    CollectRuns(element, default, headingRuns, images);
                    var normalized = Normalize(headingRuns);
                    if (normalized.Count > 0) blocks.Add(new HeadingBlock(element.LocalName[1] - '0', normalized));
                    blocks.AddRange(images);
                    break;

                case "ul" or "ol":
                    FlushParagraph();
                    var items = element.Children
                        .Where(child => child.LocalName == "li")
                        .Select(li => (IReadOnlyList<DocumentBlock>)ParseBlocks(li))
                        .Where(item => item.Count > 0)
                        .ToList();
                    if (items.Count > 0) blocks.Add(new ListBlock(element.LocalName == "ol", items));
                    break;

                case "blockquote":
                    FlushParagraph();
                    var quoted = ParseBlocks(element);
                    if (quoted.Count > 0) blocks.Add(new QuoteBlock(quoted));
                    break;

                case "pre":
                    FlushParagraph();
                    var code = element.TextContent.TrimEnd('\n', '\r');
                    if (code.Length > 0) blocks.Add(new CodeBlock(code));
                    break;

                case "hr":
                    FlushParagraph();
                    blocks.Add(new RuleBlock());
                    break;

                case "table":
                    FlushParagraph();
                    var table = ParseTable(element);
                    if (table is not null) blocks.Add(table);
                    break;

                default:
                    CollectRuns(element, default, pendingRuns, pendingImages);
                    break;
            }
        }

        FlushParagraph();
        return blocks;
    }

    private static TableBlock? ParseTable(IElement table)
    {
        var rows = new List<TableRow>();

        foreach (var row in table.QuerySelectorAll("tr"))
        {
            var cells = row.Children.Where(c => c.LocalName is "td" or "th").ToList();
            if (cells.Count == 0) continue;

            var cellRuns = cells
                .Select(cell =>
                {
                    var runs = new List<TextRun>();
                    CollectRuns(cell, default, runs, []);
                    return (IReadOnlyList<TextRun>)Normalize(runs);
                })
                .ToList();

            rows.Add(new TableRow(cells.All(c => c.LocalName == "th"), cellRuns));
        }

        return rows.Count == 0 ? null : new TableBlock(rows);
    }

    private static void CollectRuns(INode node, RunStyle style, List<TextRun> runs, List<ImageBlock> images)
    {
        if (node is IText text)
        {
            var value = Whitespace().Replace(text.Data, " ");
            if (value.Length > 0) runs.Add(style.ToRun(value));
            return;
        }

        if (node is not IElement element) return;

        switch (element.LocalName)
        {
            case "br":
                runs.Add(style.ToRun("\n"));
                return;

            case "img":
                if (TryDecodeDataImage(element.GetAttribute("src"), out var data)) images.Add(new ImageBlock(data));
                return;

            case "p" or "h1" or "h2" or "h3" or "li" or "pre" or "blockquote" when runs.Count > 0:
                // A block nested in an inline context (paragraphs inside a table cell) starts a new line.
                runs.Add(style.ToRun("\n"));
                break;
        }

        var childStyle = element.LocalName switch
        {
            "strong" or "b" => style with { Bold = true },
            "em" or "i" => style with { Italic = true },
            "u" => style with { Underline = true },
            "s" => style with { Strike = true },
            "code" => style with { Code = true },
            "a" => style with { LinkUrl = SafeLink(element.GetAttribute("href")) },
            _ => style,
        };

        foreach (var child in element.ChildNodes)
            CollectRuns(child, childStyle, runs, images);
    }

    // Collapses the whitespace HTML would collapse: trims around line breaks and at the edges,
    // and drops runs left empty.
    private static List<TextRun> Normalize(List<TextRun> runs)
    {
        var result = new List<TextRun>(runs.Count);
        var atLineStart = true;

        foreach (var run in runs)
        {
            if (run.Text == "\n")
            {
                if (result.Count > 0)
                {
                    result[^1] = result[^1] with { Text = result[^1].Text.TrimEnd(' ') };
                    result.Add(run);
                }
                atLineStart = true;
                continue;
            }

            var text = atLineStart ? run.Text.TrimStart(' ') : run.Text;
            if (!atLineStart && result.Count > 0 && result[^1].Text.EndsWith(' ') && text.StartsWith(' '))
                text = text.TrimStart(' ');
            if (text.Length == 0) continue;

            result.Add(run with { Text = text });
            atLineStart = false;
        }

        while (result.Count > 0 && result[^1].Text == "\n")
            result.RemoveAt(result.Count - 1);

        if (result.Count > 0)
        {
            var trimmed = result[^1].Text.TrimEnd(' ');
            if (trimmed.Length == 0) result.RemoveAt(result.Count - 1);
            else result[^1] = result[^1] with { Text = trimmed };
        }

        result.RemoveAll(r => r.Text.Length == 0);
        return result;
    }

    private static string? SafeLink(string? href)
        => Uri.TryCreate(href, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" or "mailto"
            ? uri.AbsoluteUri
            : null;

    private static bool TryDecodeDataImage(string? src, out byte[] data)
    {
        data = [];
        if (src is null || !src.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)) return false;

        var marker = src.IndexOf(";base64,", StringComparison.OrdinalIgnoreCase);
        if (marker < 0) return false;

        try
        {
            data = Convert.FromBase64String(src[(marker + ";base64,".Length)..]);
            return data.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    private readonly record struct RunStyle(bool Bold, bool Italic, bool Underline, bool Strike, bool Code, string? LinkUrl)
    {
        public TextRun ToRun(string text) => new(text, Bold, Italic, Underline, Strike, Code, LinkUrl);
    }
}
