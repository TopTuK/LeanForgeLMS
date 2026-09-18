using LF.Application.ModelDto.Qna;

namespace LF.Application.Services.CourseTeaching;

// Manages which users may answer a course's questions. The creator is implicit staff and is
// returned in the list flagged as such, but never stored as an assignment.
public interface ICourseTeachingTeamService
{
    Task<IReadOnlyList<CourseInstructorDto>?> ListAsync(int courseId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CourseInstructorDto>?> AssignAsync(int courseId, string email, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CourseInstructorDto>?> RemoveAsync(int courseId, int userId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);
}
