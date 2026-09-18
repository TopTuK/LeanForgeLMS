using LF.AppDomain.Models.User.Enums;

namespace LF.Application.ModelDto.Qna;

public sealed class CourseInstructorDto
{
    public int UserId { get; init; }
    public string Email { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public UserRole Role { get; init; }
    public DateTime AssignedAt { get; init; }

    // The course creator is teaching staff implicitly and has no LFCourseInstructors row, so the
    // list marks them rather than pretending they were assigned.
    public bool IsCreator { get; init; }
}
