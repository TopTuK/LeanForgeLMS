using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.News.Enums;
using LF.AppDomain.Models.Storage.Enums;

namespace LF.AppDomain.Entities.News;

public sealed class NewsPost
{
    public const int MaxTitleLength = 200;
    public const int MaxImages = 10;

    private readonly List<NewsImage> _images = [];

    private NewsPost()
    {
    }

    public int Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string Html { get; private set; } = null!;
    public NewsVisibility Visibility { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public int CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public IReadOnlyList<NewsImage> Images => _images.AsReadOnly();

    public static NewsPost Create(string title, string html, NewsVisibility visibility, int createdByUserId, DateTime createdAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(createdByUserId, 0);

        var post = new NewsPost
        {
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
        };

        post.Edit(title, html, visibility, createdAt);
        return post;
    }

    public void Edit(string title, string html, NewsVisibility visibility, DateTime updatedAt)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("News title cannot be empty.", nameof(title));

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > MaxTitleLength)
            throw new ArgumentException($"News title cannot exceed {MaxTitleLength} characters.", nameof(title));

        if (string.IsNullOrWhiteSpace(html))
            throw new ArgumentException("News content cannot be empty.", nameof(html));

        if (!Enum.IsDefined(visibility))
            throw new ArgumentException("Unknown news visibility.", nameof(visibility));

        Title = trimmedTitle;
        Html = html.Trim();
        Visibility = visibility;
        UpdatedAt = updatedAt;
    }

    // Bulk-replaces the gallery with the given ordered set. Images that stay keep their row (and
    // therefore their URL); the storage object ids that dropped out are returned so the caller can
    // delete the orphaned blobs.
    public IReadOnlyList<int> ReplaceImages(IReadOnlyList<StorageObject> images)
    {
        ArgumentNullException.ThrowIfNull(images);

        if (images.Count > MaxImages)
            throw new ArgumentException($"A news post can have at most {MaxImages} images.", nameof(images));

        if (images.Any(i => i.ObjectType != StorageObjectType.Image))
            throw new ArgumentException("Only image storage objects can be attached to a news post.", nameof(images));

        if (images.Select(i => i.Id).Distinct().Count() != images.Count)
            throw new ArgumentException("The same image cannot be attached twice.", nameof(images));

        var requestedIds = images.Select(i => i.Id).ToHashSet();
        var removedStorageObjectIds = _images
            .Where(i => !requestedIds.Contains(i.StorageObjectId))
            .Select(i => i.StorageObjectId)
            .ToList();

        var existingByStorageObjectId = _images.ToDictionary(i => i.StorageObjectId);
        _images.Clear();

        for (var i = 0; i < images.Count; i++)
        {
            var sortOrder = i + 1;
            if (existingByStorageObjectId.TryGetValue(images[i].Id, out var existing))
            {
                existing.MoveTo(sortOrder);
                _images.Add(existing);
            }
            else
            {
                _images.Add(NewsImage.Create(images[i], sortOrder));
            }
        }

        return removedStorageObjectIds;
    }

    // PublishedAt is stamped on the first publish only: it drives feed order and unread counts, so
    // an unpublish/republish round-trip must not resurface an old post as "new".
    public void Publish(DateTime publishedAt)
    {
        IsPublished = true;
        PublishedAt ??= publishedAt;
    }

    public void Unpublish() => IsPublished = false;

    public bool IsVisibleTo(bool isAuthenticated) =>
        IsPublished && (Visibility == NewsVisibility.Public || isAuthenticated);
}
