namespace LF.AppDomain.Entities.Groups;

// A live online session at a fixed time. The LMS doesn't host the video: MeetingUrl points at
// whatever conferencing tool the instructor uses.
public sealed class Lecture
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 2_000;
    public const int MaxMeetingUrlLength = 1_000;
    public const int MinDurationMinutes = 5;
    public const int MaxDurationMinutes = 600;

    private readonly List<LectureGroup> _groups = [];

    private Lecture()
    {
    }

    public int Id { get; private set; }
    public int CourseId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }

    // Always UTC; clients convert to the viewer's local time.
    public DateTime StartsAt { get; private set; }
    public int DurationMinutes { get; private set; }
    public string? MeetingUrl { get; private set; }
    public bool IsCancelled { get; private set; }
    public int CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<LectureGroup> Groups => _groups.AsReadOnly();

    public DateTime EndsAt => StartsAt.AddMinutes(DurationMinutes);

    public static Lecture Schedule(
        int courseId,
        string title,
        string? description,
        DateTime startsAt,
        int durationMinutes,
        string? meetingUrl,
        IReadOnlyCollection<int> groupIds,
        int createdByUserId,
        DateTime createdAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(courseId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(createdByUserId, 0);

        var lecture = new Lecture
        {
            CourseId = courseId,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
        };

        lecture.UpdateDetails(title, description, meetingUrl);
        lecture.Reschedule(startsAt, durationMinutes);
        lecture.AssignGroups(groupIds);
        return lecture;
    }

    public bool UpdateDetails(string title, string? description, string? meetingUrl)
    {
        var normalizedTitle = NormalizeTitle(title);
        var normalizedDescription = NormalizeDescription(description);
        var normalizedUrl = NormalizeMeetingUrl(meetingUrl);

        if (normalizedTitle == Title && normalizedDescription == Description && normalizedUrl == MeetingUrl)
            return false;

        Title = normalizedTitle;
        Description = normalizedDescription;
        MeetingUrl = normalizedUrl;
        return true;
    }

    public bool Reschedule(DateTime startsAt, int durationMinutes)
    {
        if (startsAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Lecture start time must be in UTC.", nameof(startsAt));

        ArgumentOutOfRangeException.ThrowIfLessThan(durationMinutes, MinDurationMinutes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(durationMinutes, MaxDurationMinutes);

        if (startsAt == StartsAt && durationMinutes == DurationMinutes)
            return false;

        StartsAt = startsAt;
        DurationMinutes = durationMinutes;
        return true;
    }

    // Replaces the whole set. Callers are responsible for checking the groups belong to this
    // lecture's course: the entity only sees ids.
    public bool AssignGroups(IReadOnlyCollection<int> groupIds)
    {
        ArgumentNullException.ThrowIfNull(groupIds);

        var distinct = groupIds.Distinct().ToList();
        if (distinct.Count == 0)
            throw new ArgumentException("A lecture must be assigned to at least one group.", nameof(groupIds));

        if (distinct.Any(id => id <= 0))
            throw new ArgumentException("Group ids must be positive.", nameof(groupIds));

        var current = _groups.Select(g => g.GroupId).ToHashSet();
        if (current.SetEquals(distinct))
            return false;

        _groups.RemoveAll(g => !distinct.Contains(g.GroupId));
        foreach (var groupId in distinct.Where(id => !current.Contains(id)))
            _groups.Add(LectureGroup.Create(groupId));

        return true;
    }

    public void Cancel()
    {
        if (IsCancelled)
            throw new InvalidOperationException("Lecture is already cancelled.");

        IsCancelled = true;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Lecture title cannot be empty.", nameof(title));

        var trimmed = title.Trim();
        if (trimmed.Length > MaxTitleLength)
            throw new ArgumentException($"Lecture title cannot exceed {MaxTitleLength} characters.", nameof(title));

        return trimmed;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var trimmed = description.Trim();
        if (trimmed.Length > MaxDescriptionLength)
            throw new ArgumentException($"Lecture description cannot exceed {MaxDescriptionLength} characters.", nameof(description));

        return trimmed;
    }

    // Rendered as a clickable link for students, so anything but http(s) (javascript:, data:, ...) is refused.
    private static string? NormalizeMeetingUrl(string? meetingUrl)
    {
        if (string.IsNullOrWhiteSpace(meetingUrl))
            return null;

        var trimmed = meetingUrl.Trim();
        if (trimmed.Length > MaxMeetingUrlLength)
            throw new ArgumentException($"Meeting URL cannot exceed {MaxMeetingUrlLength} characters.", nameof(meetingUrl));

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("Meeting URL must be an absolute http or https link.", nameof(meetingUrl));

        return trimmed;
    }
}
