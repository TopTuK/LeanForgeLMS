using LF.Application.ModelDto.Teaching;

namespace LF.Application.Services.Teaching;

// "Courses I teach": the ones the user created plus the ones they were assigned to as an instructor.
// The gRPC ListCourses only returns owned courses, which hid assigned instructors' courses entirely.
public interface ITeachingCourseService
{
    Task<IReadOnlyList<TeachingCourseDto>> ListMyTeachingCoursesAsync(int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);
}
