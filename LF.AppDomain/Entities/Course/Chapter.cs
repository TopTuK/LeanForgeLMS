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

    public void RemoveLesson(int lessonId)
    {
        var lesson = _lessons.FirstOrDefault(l => l.Id == lessonId)
            ?? throw new InvalidOperationException($"Lesson {lessonId} not found on this chapter.");

        _lessons.Remove(lesson);
        for (var i = 0; i < _lessons.Count; i++)
            _lessons[i].SetSortOrder(i + 1);
    }

    public void MoveLessonUp(int lessonId) => ReorderLesson(lessonId, -1);

    public void MoveLessonDown(int lessonId) => ReorderLesson(lessonId, 1);

    private void ReorderLesson(int lessonId, int delta)
    {
        var index = _lessons.FindIndex(l => l.Id == lessonId);
        if (index < 0)
            throw new InvalidOperationException($"Lesson {lessonId} not found on this chapter.");

        var targetIndex = index + delta;
        if (targetIndex < 0 || targetIndex >= _lessons.Count)
            return;

        var current = _lessons[index];
        var target = _lessons[targetIndex];
        (var currentOrder, var targetOrder) = (current.SortOrder, target.SortOrder);
        current.SetSortOrder(targetOrder);
        target.SetSortOrder(currentOrder);
        (_lessons[index], _lessons[targetIndex]) = (_lessons[targetIndex], _lessons[index]);
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Chapter title cannot be empty.", nameof(title));

        Title = title.Trim();
    }

    internal void SetSortOrder(int sortOrder) => SortOrder = sortOrder;

    internal bool HasLessons => _lessons.Count > 0;
}
