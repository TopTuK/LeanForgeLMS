namespace LF.Application.ModelDto.News;

public sealed class NotificationFeedDto
{
    public IReadOnlyList<NewsPostDto> Items { get; init; } = [];
    public int TotalCount { get; init; }

    // Null when the user has never opened Notifications — every item is new to them.
    public DateTime? LastSeenAt { get; init; }
}
