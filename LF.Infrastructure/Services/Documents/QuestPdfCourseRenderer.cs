using LF.Application.Common.Documents;
using LF.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using QuestPDF.Drawing.Exceptions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PdfImage = QuestPDF.Infrastructure.Image;

namespace LF.Infrastructure.Services.Documents;

internal sealed class QuestPdfCourseRenderer : ICoursePdfRenderer
{
    private const float MaxImageHeight = 500;
    private const float PointsPerPixel = 72f / 96f;

    private readonly ILogger<QuestPdfCourseRenderer> _logger;

    public QuestPdfCourseRenderer(ILogger<QuestPdfCourseRenderer> logger)
    {
        _logger = logger;
        QuestPdfSetup.EnsureConfigured();
    }

    public byte[] Render(CourseDocument document)
    {
        var images = DecodeImages(document);
        try
        {
            return Document
                .Create(container =>
                {
                    ComposeCover(container, document, images);
                    ComposeBody(container, document, images);
                })
                .WithMetadata(new DocumentMetadata
                {
                    Title = document.Title,
                    Author = document.StudentName,
                    CreationDate = document.ExportedAt,
                    ModifiedDate = document.ExportedAt,
                })
                .GeneratePdf();
        }
        finally
        {
            foreach (var image in images.Values)
                image.Dispose();
        }
    }

    // Decoded up front so a corrupt upload is skipped instead of aborting layout halfway through.
    private Dictionary<ImageBlock, PdfImage> DecodeImages(CourseDocument document)
    {
        var images = new Dictionary<ImageBlock, PdfImage>(ReferenceEqualityComparer.Instance);

        var blocks = document.Description.Concat(document.Chapters.SelectMany(c => c.Lessons).SelectMany(l => l.Blocks));
        foreach (var block in Flatten(blocks).OfType<ImageBlock>())
        {
            if (images.ContainsKey(block)) continue;

            try
            {
                images[block] = PdfImage.FromBinaryData(block.Data);
            }
            catch (DocumentComposeException ex)
            {
                _logger.LogWarning(ex, "QuestPdfCourseRenderer::DecodeImages: skipping an undecodable image of {SizeBytes} bytes", block.Data.Length);
            }
        }

        return images;
    }

    private static IEnumerable<DocumentBlock> Flatten(IEnumerable<DocumentBlock> blocks)
    {
        foreach (var block in blocks)
        {
            yield return block;

            var children = block switch
            {
                QuoteBlock quote => quote.Blocks,
                ListBlock list => list.Items.SelectMany(i => i),
                _ => [],
            };

            foreach (var child in Flatten(children))
                yield return child;
        }
    }

    private static void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(2, Unit.Centimetre);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(x => x.FontFamily(QuestPdfSetup.BodyFont).FontSize(11).LineHeight(1.4f).FontColor(Colors.Grey.Darken4));
    }

    private static void ComposeCover(IDocumentContainer container, CourseDocument document, Dictionary<ImageBlock, PdfImage> images)
    {
        container.Page(page =>
        {
            ConfigurePage(page);

            page.Content().AlignMiddle().Column(column =>
            {
                column.Spacing(16);

                column.Item().Text(document.Title).FontSize(28).Bold().FontColor(Colors.Grey.Darken4);
                column.Item().LineHorizontal(2).LineColor(Colors.Blue.Darken2);

                if (document.Description.Count > 0)
                    column.Item().DefaultTextStyle(x => x.FontColor(Colors.Grey.Darken2)).Element(c => ComposeBlocks(c, document.Description, images));

                column.Item().PaddingTop(24).Column(meta =>
                {
                    meta.Spacing(4);

                    if (document.StudentName.Length > 0)
                    {
                        meta.Item().Text(text =>
                        {
                            text.Span($"{document.Labels.PreparedFor}: ").FontColor(Colors.Grey.Darken1);
                            text.Span(document.StudentName).SemiBold();
                        });
                    }

                    meta.Item().Text(document.Labels.ExportedOn).FontColor(Colors.Grey.Darken1);
                });
            });
        });
    }

    private static void ComposeBody(IDocumentContainer container, CourseDocument document, Dictionary<ImageBlock, PdfImage> images)
    {
        container.Page(page =>
        {
            ConfigurePage(page);

            page.Header().PaddingBottom(10).Text(document.Title).FontSize(9).FontColor(Colors.Grey.Medium);

            page.Footer().AlignCenter().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Medium));
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });

            page.Content().Column(column =>
            {
                column.Item().Element(c => ComposeTableOfContents(c, document));

                for (var chapterIndex = 0; chapterIndex < document.Chapters.Count; chapterIndex++)
                {
                    var chapter = document.Chapters[chapterIndex];
                    var chapterNumber = chapterIndex + 1;

                    column.Item().PageBreak();
                    column.Item().Section(ChapterSection(chapterIndex)).PaddingBottom(16)
                        .Text(ChapterTitle(document.Labels, chapterNumber, chapter.Title)).FontSize(22).Bold().FontColor(Colors.Blue.Darken3);

                    for (var lessonIndex = 0; lessonIndex < chapter.Lessons.Count; lessonIndex++)
                    {
                        var lesson = chapter.Lessons[lessonIndex];

                        column.Item().Section(LessonSection(chapterIndex, lessonIndex)).PaddingTop(lessonIndex == 0 ? 0 : 20).PaddingBottom(8)
                            .Text(lesson.Title).FontSize(16).SemiBold();

                        column.Item().Element(c => ComposeBlocks(c, lesson.Blocks, images));
                    }
                }
            });
        });
    }

    private static void ComposeTableOfContents(IContainer container, CourseDocument document)
    {
        container.Column(column =>
        {
            column.Spacing(4);
            column.Item().PaddingBottom(12).Text(document.Labels.Contents).FontSize(22).Bold();

            for (var chapterIndex = 0; chapterIndex < document.Chapters.Count; chapterIndex++)
            {
                var chapter = document.Chapters[chapterIndex];

                column.Item().PaddingTop(chapterIndex == 0 ? 0 : 8).SectionLink(ChapterSection(chapterIndex)).Row(row =>
                {
                    row.RelativeItem().Text(ChapterTitle(document.Labels, chapterIndex + 1, chapter.Title)).SemiBold();
                    row.AutoItem().PaddingLeft(8).Text(text => text.BeginPageNumberOfSection(ChapterSection(chapterIndex)));
                });

                for (var lessonIndex = 0; lessonIndex < chapter.Lessons.Count; lessonIndex++)
                {
                    var section = LessonSection(chapterIndex, lessonIndex);

                    column.Item().PaddingLeft(16).SectionLink(section).Row(row =>
                    {
                        row.RelativeItem().Text(chapter.Lessons[lessonIndex].Title);
                        row.AutoItem().PaddingLeft(8).DefaultTextStyle(x => x.FontColor(Colors.Grey.Darken1)).Text(text => text.BeginPageNumberOfSection(section));
                    });
                }
            }
        });
    }

    private static void ComposeBlocks(IContainer container, IReadOnlyList<DocumentBlock> blocks, Dictionary<ImageBlock, PdfImage> images)
    {
        container.Column(column =>
        {
            column.Spacing(8);

            foreach (var block in blocks)
            {
                if (block is ImageBlock image && !images.ContainsKey(image))
                    continue;

                column.Item().Element(c => ComposeBlock(c, block, images));
            }
        });
    }

    private static void ComposeBlock(IContainer container, DocumentBlock block, Dictionary<ImageBlock, PdfImage> images)
    {
        switch (block)
        {
            case ParagraphBlock paragraph:
                container.Text(text => AddRuns(text, paragraph.Runs));
                break;

            case HeadingBlock heading:
                container.PaddingTop(6).Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(heading.Level switch { 1 => 18, 2 => 15, _ => 13 }).Bold());
                    AddRuns(text, heading.Runs);
                });
                break;

            case ListBlock list:
                container.Column(column =>
                {
                    column.Spacing(4);

                    for (var i = 0; i < list.Items.Count; i++)
                    {
                        var marker = list.Ordered ? $"{i + 1}." : "•";
                        var item = list.Items[i];

                        column.Item().Row(row =>
                        {
                            row.ConstantItem(20).Text(marker);
                            row.RelativeItem().Element(c => ComposeBlocks(c, item, images));
                        });
                    }
                });
                break;

            case QuoteBlock quote:
                container.BorderLeft(3).BorderColor(Colors.Grey.Lighten1).PaddingLeft(12)
                    .DefaultTextStyle(x => x.Italic().FontColor(Colors.Grey.Darken2))
                    .Element(c => ComposeBlocks(c, quote.Blocks, images));
                break;

            case CodeBlock code:
                container.Background(Colors.Grey.Lighten4).Padding(8)
                    .Text(code.Text).FontFamily(QuestPdfSetup.MonoFont).FontSize(9).LineHeight(1.3f);
                break;

            case RuleBlock:
                container.PaddingVertical(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                break;

            case TableBlock table:
                ComposeTable(container, table);
                break;

            case ImageBlock image:
                // FitArea alone would upscale a small picture to fill the page; cap it at its natural size (96 dpi).
                var picture = images[image];
                container.AlignCenter()
                    .MaxWidth(picture.Size.Width * PointsPerPixel)
                    .MaxHeight(Math.Min(MaxImageHeight, picture.Size.Height * PointsPerPixel))
                    .Image(picture).FitArea();
                break;
        }
    }

    private static void ComposeTable(IContainer container, TableBlock table)
    {
        var columnCount = table.Rows.Max(r => r.Cells.Count);

        container.Table(grid =>
        {
            grid.ColumnsDefinition(columns =>
            {
                for (var i = 0; i < columnCount; i++)
                    columns.RelativeColumn();
            });

            foreach (var row in table.Rows)
            {
                for (var i = 0; i < columnCount; i++)
                {
                    var cell = grid.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten1)
                        .Background(row.IsHeader ? Colors.Grey.Lighten4 : Colors.White).Padding(4);

                    var runs = i < row.Cells.Count ? row.Cells[i] : [];
                    cell.Text(text =>
                    {
                        text.DefaultTextStyle(x => row.IsHeader ? x.FontSize(10).SemiBold() : x.FontSize(10));
                        AddRuns(text, runs);
                    });
                }
            }
        });
    }

    private static void AddRuns(TextDescriptor text, IReadOnlyList<TextRun> runs)
    {
        foreach (var run in runs)
        {
            var span = run.LinkUrl is null ? text.Span(run.Text) : text.Hyperlink(run.Text, run.LinkUrl);

            if (run.Bold) span.Bold();
            if (run.Italic) span.Italic();
            if (run.Strike) span.Strikethrough();
            if (run.Underline || run.LinkUrl is not null) span.Underline();
            if (run.LinkUrl is not null) span.FontColor(Colors.Blue.Darken2);
            if (run.Code) span.FontFamily(QuestPdfSetup.MonoFont).FontSize(10).BackgroundColor(Colors.Grey.Lighten3);
        }
    }

    private static string ChapterTitle(CourseDocumentLabels labels, int number, string title) => $"{labels.Chapter} {number}. {title}";

    private static string ChapterSection(int chapterIndex) => $"chapter-{chapterIndex}";

    private static string LessonSection(int chapterIndex, int lessonIndex) => $"lesson-{chapterIndex}-{lessonIndex}";
}
