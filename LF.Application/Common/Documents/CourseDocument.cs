namespace LF.Application.Common.Documents;

// Framework-free description of a printable course. Built by the Application layer, drawn by an
// ICoursePdfRenderer in Infrastructure, so no PDF library type ever leaks into the use case.
public sealed record CourseDocument(
    string Title,
    IReadOnlyList<DocumentBlock> Description,
    string StudentName,
    DateTimeOffset ExportedAt,
    CourseDocumentLabels Labels,
    IReadOnlyList<DocumentChapter> Chapters);

// Already localized and formatted by the caller; the renderer prints them verbatim.
public sealed record CourseDocumentLabels(
    string Contents,
    string Chapter,
    string PreparedFor,
    string ExportedOn);

public sealed record DocumentChapter(string Title, IReadOnlyList<DocumentLesson> Lessons);

public sealed record DocumentLesson(string Title, IReadOnlyList<DocumentBlock> Blocks);

public abstract record DocumentBlock;

public sealed record ParagraphBlock(IReadOnlyList<TextRun> Runs) : DocumentBlock;

public sealed record HeadingBlock(int Level, IReadOnlyList<TextRun> Runs) : DocumentBlock;

public sealed record ListBlock(bool Ordered, IReadOnlyList<IReadOnlyList<DocumentBlock>> Items) : DocumentBlock;

public sealed record QuoteBlock(IReadOnlyList<DocumentBlock> Blocks) : DocumentBlock;

public sealed record CodeBlock(string Text) : DocumentBlock;

public sealed record RuleBlock : DocumentBlock;

public sealed record TableBlock(IReadOnlyList<TableRow> Rows) : DocumentBlock;

public sealed record TableRow(bool IsHeader, IReadOnlyList<IReadOnlyList<TextRun>> Cells);

public sealed record ImageBlock(byte[] Data) : DocumentBlock;

public sealed record TextRun(
    string Text,
    bool Bold = false,
    bool Italic = false,
    bool Underline = false,
    bool Strike = false,
    bool Code = false,
    string? LinkUrl = null);
