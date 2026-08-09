namespace LF.AppDomain.Entities.Course;

public sealed class Lesson
{
    private Lesson()
    {
    }

    public int Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string Content { get; private set; } = string.Empty;
    public bool IncludeInPreview { get; private set; }
    public int SortOrder { get; private set; }
    public int ChapterId { get; private set; }

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
}
