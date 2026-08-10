namespace LF.AppDomain.Entities.Course;

public sealed class Chapter
{
    private readonly List<Lesson> _lessons = [];

    private Chapter()
    {
    }

    public int Id { get; private set; }
    public string Title { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public int CourseId { get; private set; }
    public IReadOnlyList<Lesson> Lessons => _lessons.AsReadOnly();

    internal static Chapter Create(string title, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Chapter title cannot be empty.", nameof(title));

        return new Chapter
        {
            Title = title.Trim(),
            SortOrder = sortOrder
        };
    }

    public Lesson AddLesson(string title, string? content = null, bool includeInPreview = false)
    {
        var lesson = Lesson.Create(title, content, includeInPreview, _lessons.Count + 1);
        _lessons.Add(lesson);
        return lesson;
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Chapter title cannot be empty.", nameof(title));

        Title = title.Trim();
    }

    internal bool HasLessons => _lessons.Count > 0;
}
