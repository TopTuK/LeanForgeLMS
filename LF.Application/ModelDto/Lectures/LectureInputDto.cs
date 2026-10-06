namespace LF.Application.ModelDto.Lectures;

public sealed class LectureInputDto
{
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public DateTime StartsAt { get; init; }
    public int DurationMinutes { get; init; }
    public string? MeetingUrl { get; init; }
    public IReadOnlyList<int> GroupIds { get; init; } = [];
}
