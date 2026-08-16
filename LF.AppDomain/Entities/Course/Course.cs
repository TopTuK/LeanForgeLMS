using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.Course.Enums;

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
    public CourseCoverType CoverType { get; private set; } = CourseCoverType.None;
    public CourseCoverColor? CoverColor { get; private set; }
    public int? CoverImageStorageObjectId { get; private set; }
    public StorageObject? CoverImageStorageObject { get; private set; }
    public bool IsPublished { get; private set; }
    public int CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public int CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyList<Chapter> Chapters => _chapters.AsReadOnly();

    public static Course Create(
        string title,
        string shortIntroduction,
        string description,
        Category category,
        int createdByUserId,
        DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(createdByUserId, 0);

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
            CreatedByUserId = createdByUserId,
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

    public void MoveChapterUp(int chapterId) => ReorderChapter(chapterId, -1);

    public void MoveChapterDown(int chapterId) => ReorderChapter(chapterId, 1);

    private void ReorderChapter(int chapterId, int delta)
    {
        var index = _chapters.FindIndex(c => c.Id == chapterId);
        if (index < 0)
            throw new InvalidOperationException($"Chapter {chapterId} not found on this course.");

        var targetIndex = index + delta;
        if (targetIndex < 0 || targetIndex >= _chapters.Count)
            return;

        var current = _chapters[index];
        var target = _chapters[targetIndex];
        (var currentOrder, var targetOrder) = (current.SortOrder, target.SortOrder);
        current.SetSortOrder(targetOrder);
        target.SetSortOrder(currentOrder);
        (_chapters[index], _chapters[targetIndex]) = (_chapters[targetIndex], _chapters[index]);
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

    public void SetColorCover(CourseCoverColor color)
    {
        CoverType = CourseCoverType.Color;
        CoverColor = color;
        CoverImageStorageObjectId = null;
        CoverImageStorageObject = null;
    }

    public void SetImageCover(StorageObject storageObject)
    {
        ArgumentNullException.ThrowIfNull(storageObject);

        CoverType = CourseCoverType.Image;
        CoverImageStorageObjectId = storageObject.Id;
        CoverImageStorageObject = storageObject;
        CoverColor = null;
    }

    public void ClearCover()
    {
        CoverType = CourseCoverType.None;
        CoverColor = null;
        CoverImageStorageObjectId = null;
        CoverImageStorageObject = null;
    }
}
