namespace LF.AppDomain.Entities.Course;

public sealed class Lesson
{
    private readonly List<LessonPart> _parts = [];

    private Lesson()
    {
    }

    public int Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string Content { get; private set; } = string.Empty;
    public bool IncludeInPreview { get; private set; }
    public int SortOrder { get; private set; }
    public int ChapterId { get; private set; }
    public IReadOnlyList<LessonPart> Parts => _parts.AsReadOnly();

    internal static Lesson Create(string title, string? content, bool includeInPreview, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Lesson title cannot be empty.", nameof(title));

        return new Lesson
        {
            Title = title.Trim(),
            Content = content ?? string.Empty,
            IncludeInPreview = includeInPreview,
            SortOrder = sortOrder
        };
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Lesson title cannot be empty.", nameof(title));

        Title = title.Trim();
    }

    public void UpdateContent(string? content) => Content = content ?? string.Empty;

    public void SetIncludeInPreview(bool includeInPreview) => IncludeInPreview = includeInPreview;

    public void ReplaceParts(IReadOnlyList<LessonPartInput> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        _parts.Clear();
        for (var i = 0; i < parts.Count; i++)
            _parts.Add(LessonPart.Create(parts[i].PartType, i + 1, parts[i].Html, parts[i].StorageObject, parts[i].QuizQuestions, parts[i].QuizPassThresholdPercent, parts[i].Files));
    }

    internal void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}
