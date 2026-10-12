using System.Text;
using LF.Application.Common.Documents;
using LF.Application.Common.Interfaces;
using LF.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LF.WebApiTests;

/// <summary>
/// Smoke tests for the QuestPDF renderer behind <c>AddInfrastructurePdf</c>. The renderer type is
/// internal to LF.Infrastructure, so it is resolved through DI exactly as LF.WebApi does.
/// </summary>
public class CoursePdfRendererTests
{
    // 1x1 transparent PNG.
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

    private static ICoursePdfRenderer CreateRenderer()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddInfrastructurePdf();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        return provider.GetRequiredService<ICoursePdfRenderer>();
    }

    private static CourseDocument Document(params DocumentBlock[] lessonBlocks) => new(
        "Основы программирования",
        [new ParagraphBlock([new TextRun("Курс для начинающих")])],
        "Саша Студент",
        new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.Zero),
        new CourseDocumentLabels("Содержание", "Глава", "Подготовлено для", "Дата выгрузки: 12 октября 2026 г."),
        [new DocumentChapter("Введение", [new DocumentLesson("Первый урок", lessonBlocks), new DocumentLesson("Второй урок", [])])]);

    [Fact]
    public void Render_CyrillicDocumentWithEveryBlockType_ProducesPdf()
    {
        // Arrange
        var renderer = CreateRenderer();
        var document = Document(
            new HeadingBlock(2, [new TextRun("Заголовок")]),
            new ParagraphBlock([
                new TextRun("Обычный, "), new TextRun("жирный", Bold: true), new TextRun(", "),
                new TextRun("ссылка", LinkUrl: "https://example.com"), new TextRun("\n"), new TextRun("код", Code: true)]),
            new ListBlock(true, [[new ParagraphBlock([new TextRun("один")])], [new ParagraphBlock([new TextRun("два")])]]),
            new QuoteBlock([new ParagraphBlock([new TextRun("цитата")])]),
            new CodeBlock("var x = 1;\nConsole.WriteLine(x);"),
            new RuleBlock(),
            new TableBlock([
                new TableRow(true, [[new TextRun("Имя")], [new TextRun("Значение")]]),
                new TableRow(false, [[new TextRun("a")]])]),
            new ImageBlock(TinyPng));

        // Act
        var pdf = renderer.Render(document);
        // Assert
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void Render_UndecodableImage_IsSkippedInsteadOfFailing()
    {
        // Arrange
        var renderer = CreateRenderer();

        // Act
        var pdf = renderer.Render(Document(new ImageBlock([1, 2, 3, 4]), new ParagraphBlock([new TextRun("после")])));

        // Assert
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void Render_EmptyCourse_ProducesPdf()
    {
        // Arrange
        var renderer = CreateRenderer();
        var document = Document() with { Chapters = [], Description = [] };

        // Act
        var pdf = renderer.Render(document);

        // Assert
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }
}
