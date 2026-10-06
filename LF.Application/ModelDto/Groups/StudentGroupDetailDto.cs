namespace LF.Application.ModelDto.Groups;

public sealed class StudentGroupDetailDto
{
    public int Id { get; init; }
    public int CourseId { get; init; }
    public string CourseTitle { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }

    // True when the viewer teaches the course (or is an admin), i.e. may edit the group.
    public bool CanManage { get; init; }
    public IReadOnlyList<StudentGroupMemberDto> Members { get; init; } = [];
}
