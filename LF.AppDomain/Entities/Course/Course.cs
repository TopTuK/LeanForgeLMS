namespace LF.AppDomain.Entities.Course;

public sealed class Course
{
    private readonly List<Chapter> _chapters = [];

    private Course()
    {
    }

    public int Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string ShortIntroduction { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string? ImageKey { get; private set; }
    public bool IsPublished { get; private set; }
    public int CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyList<Chapter> Chapters => _chapters.AsReadOnly();

    public static Course Create(
        string title,
        string shortIntroduction,
        string description,
        Category category,
        DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(category);

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Course title cannot be empty.", nameof(title));

        if (string.IsNullOrWhiteSpace(shortIntroduction))
            throw new ArgumentException("Short introduction cannot be empty.", nameof(shortIntroduction));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty.", nameof(description));

        return new Course
        {
            Title = title.Trim(),
            ShortIntroduction = shortIntroduction.Trim(),
            Description = description.Trim(),
            Category = category,
            CategoryId = category.Id,
            CreatedAt = createdAt,
            IsPublished = false
        };
    }

    public Chapter AddChapter(string title)
    {
        var chapter = Chapter.Create(title, _chapters.Count + 1);
        _chapters.Add(chapter);
        return chapter;
    }

    public void Publish()
    {
        if (_chapters.Count == 0)
            throw new InvalidOperationException("Cannot publish a course without chapters.");

        if (_chapters.Any(c => !c.HasLessons))
            throw new InvalidOperationException("Cannot publish a course with empty chapters.");

        IsPublished = true;
    }

    public void Unpublish() => IsPublished = false;

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Course title cannot be empty.", nameof(title));

        Title = title.Trim();
    }

    public void UpdateShortIntroduction(string shortIntroduction)
    {
        if (string.IsNullOrWhiteSpace(shortIntroduction))
            throw new ArgumentException("Short introduction cannot be empty.", nameof(shortIntroduction));

        ShortIntroduction = shortIntroduction.Trim();
    }

    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty.", nameof(description));

        Description = description.Trim();
    }

    public void SetCategory(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        Category = category;
        CategoryId = category.Id;
    }

    public void SetImage(string imageKey)
    {
        if (string.IsNullOrWhiteSpace(imageKey))
            throw new ArgumentException("Image key cannot be empty.", nameof(imageKey));

        ImageKey = imageKey;
    }

    public void ClearImage() => ImageKey = null;
}
