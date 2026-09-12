using LF.Application.ModelDto.Course;
using LF.Application.ModelDto.Enrollment;

namespace LF.Application.Services.Admin;

public interface IAdminCourseService
{
    Task<PagedCoursesDto> ListCoursesAsync(int page, int pageSize, int actingAdminId);
    Task<PagedCourseEnrollmentsDto?> ListCourseEnrollmentsAsync(int courseId, int actingAdminId, int page, int pageSize);
    Task<EnrollmentSummaryDto?> EnrollStudentAsync(int courseId, int targetUserId, int actingAdminId);
    Task<RemoveEnrollmentResultDto?> RemoveEnrollmentAsync(int courseId, int enrollmentId, int actingAdminId);
    Task<DeleteCourseResultDto?> DeleteCourseAsync(int courseId, int actingAdminId, bool force);
}
