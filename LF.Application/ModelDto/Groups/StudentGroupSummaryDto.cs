namespace LF.Application.ModelDto.Groups;

public sealed class StudentGroupSummaryDto
{
    public int Id { get; init; }
    public int CourseId { get; init; }
    public string CourseTitle { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public int MemberCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? NextLectureStartsAt { get; init; }

    // The viewer sees this group because they teach its course rather than belong to it.
    public bool IsTeaching { get; init; }
}
