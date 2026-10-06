namespace LF.Application.ModelDto.Lectures;

public sealed class LectureDto
{
    public int Id { get; init; }
    public int CourseId { get; init; }
    public string CourseTitle { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public DateTime StartsAt { get; init; }
    public int DurationMinutes { get; init; }

    // Null for cancelled lectures so nobody joins a session that isn't happening.
    public string? MeetingUrl { get; init; }
    public bool IsCancelled { get; init; }
    public IReadOnlyList<LectureGroupRefDto> Groups { get; init; } = [];

    // True when the viewer sees this lecture because they teach the course rather than attend it.
    public bool IsTeaching { get; init; }
}
