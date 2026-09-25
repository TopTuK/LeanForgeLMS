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

    /// <returns>true if the title actually changed.</returns>
    public bool Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Lesson title cannot be empty.", nameof(title));

        var trimmed = title.Trim();
        if (Title == trimmed)
            return false;

        Title = trimmed;
        return true;
    }

    /// <returns>true if the content actually changed.</returns>
    public bool UpdateContent(string? content)
    {
        var normalized = content ?? string.Empty;
        if (Content == normalized)
            return false;

        Content = normalized;
        return true;
    }

    public void SetIncludeInPreview(bool includeInPreview) => IncludeInPreview = includeInPreview;

    // The editor always sends the whole part list, so the parts are rebuilt on every save.
    /// <returns>true if what a student sees actually changed; false for a save with identical content.</returns>
    public bool ReplaceParts(IReadOnlyList<LessonPartInput> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        var before = PartsFingerprint();

        _parts.Clear();
        for (var i = 0; i < parts.Count; i++)
            _parts.Add(LessonPart.Create(parts[i].PartType, i + 1, parts[i].Html, parts[i].StorageObject, parts[i].QuizQuestions, parts[i].QuizPassThresholdPercent, parts[i].Files));

        return before != PartsFingerprint();
    }

    // Everything student-visible about the parts, ignoring row ids (which change on every rebuild).
    // Storage ids are read through the navigation too: a freshly built part has no FK value until EF fixup.
    private string PartsFingerprint()
    {
        const char Sep = '';
        var builder = new System.Text.StringBuilder();

        foreach (var part in _parts.OrderBy(p => p.SortOrder))
        {
            builder.Append(part.PartType).Append(Sep)
                .Append(part.Html).Append(Sep)
                .Append(part.StorageObject?.Id ?? part.StorageObjectId).Append(Sep)
                .Append(part.QuizPassThresholdPercent).Append(Sep);

            foreach (var question in part.QuizQuestions.OrderBy(q => q.SortOrder))
            {
                builder.Append('Q').Append(question.QuestionType).Append(Sep).Append(question.Text).Append(Sep);
                foreach (var option in question.Options.OrderBy(o => o.SortOrder))
                    builder.Append('O').Append(option.IsCorrect).Append(Sep).Append(option.Text).Append(Sep);
            }

            foreach (var file in part.Files.OrderBy(f => f.SortOrder))
                builder.Append('F').Append(file.FileName).Append(Sep).Append(file.StorageObject?.Id ?? file.StorageObjectId).Append(Sep);

            builder.Append('');
        }

        return builder.ToString();
    }

    internal void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}
