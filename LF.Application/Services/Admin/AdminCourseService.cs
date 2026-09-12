using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Course;
using LF.Application.ModelDto.Enrollment;
using LF.Application.Services.Course;
using LF.Application.Services.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Admin;

// Runs inside LF.WebApi. Composes the course and identity services: LF.CourseService owns
// enrollments but only knows a scalar UserId, so student names/emails are hydrated here.
internal sealed class AdminCourseService(
    ILogger<AdminCourseService> logger,
    IGrpcCourseService courseService,
    IGrpcIdentityService identityService,
    [FromKeyedServices("storage")] IFileStorageService fileStorageService) : IAdminCourseService
{
    private readonly ILogger<AdminCourseService> _logger = logger;
    private readonly IGrpcCourseService _courseService = courseService;
    private readonly IGrpcIdentityService _identityService = identityService;
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public async Task<PagedCoursesDto> ListCoursesAsync(int page, int pageSize, int actingAdminId)
    {
        _logger.LogInformation("AdminCourseService::ListCoursesAsync: called with Page={Page} PageSize={PageSize} ActingAdminId={adminId}",
            page, pageSize, actingAdminId);

        // isAdmin: true widens the existing listing from "my courses" to every course.
        var paged = await _courseService.ListCoursesAsync(page, pageSize, actingAdminId, isAdmin: true);

        // This list mixes every author's courses together, so resolve who owns each one.
        var authorIds = paged.Items.Select(c => c.CreatedByUserId).Distinct().ToList();
        var authorsById = (await _identityService.ListUsersByIdsAsync(authorIds)).ToDictionary(u => u.Id);

        foreach (var course in paged.Items)
        {
            if (!authorsById.TryGetValue(course.CreatedByUserId, out var author))
                continue;

            course.AuthorEmail = author.Email;
            course.AuthorFirstName = author.FirstName;
            course.AuthorLastName = author.LastName;
        }

        return paged;
    }

    public async Task<PagedCourseEnrollmentsDto?> ListCourseEnrollmentsAsync(int courseId, int actingAdminId, int page, int pageSize)
    {
        _logger.LogInformation("AdminCourseService::ListCourseEnrollmentsAsync: called with CourseId={CourseId} ActingAdminId={adminId} Page={Page} PageSize={PageSize}",
            courseId, actingAdminId, page, pageSize);

        var paged = await _courseService.ListCourseEnrollmentsAsync(courseId, actingAdminId, page, pageSize);
        if (paged is null)
            return null;

        var userIds = paged.Items.Select(e => e.UserId).Distinct().ToList();
        var users = await _identityService.ListUsersByIdsAsync(userIds);
        var usersById = users.ToDictionary(u => u.Id);

        // A student deleted from the identity store leaves their enrollment behind (no FK across
        // the boundary), so a missing user is expected — the row still lists, without a name.
        foreach (var item in paged.Items)
        {
            if (!usersById.TryGetValue(item.UserId, out var user))
                continue;

            item.StudentEmail = user.Email;
            item.StudentFirstName = user.FirstName;
            item.StudentLastName = user.LastName;
        }

        return paged;
    }

    public async Task<EnrollmentSummaryDto?> EnrollStudentAsync(int courseId, int targetUserId, int actingAdminId)
    {
        _logger.LogInformation("AdminCourseService::EnrollStudentAsync: called with CourseId={CourseId} TargetUserId={usrId} ActingAdminId={adminId}",
            courseId, targetUserId, actingAdminId);

        return await _courseService.EnrollUserAsync(courseId, targetUserId, actingAdminId, isAdmin: true);
    }

    public async Task<RemoveEnrollmentResultDto?> RemoveEnrollmentAsync(int courseId, int enrollmentId, int actingAdminId)
    {
        _logger.LogInformation("AdminCourseService::RemoveEnrollmentAsync: called with CourseId={CourseId} EnrollmentId={EnrollmentId} ActingAdminId={adminId}",
            courseId, enrollmentId, actingAdminId);

        return await _courseService.RemoveEnrollmentAsync(courseId, enrollmentId, actingAdminId);
    }

    public async Task<DeleteCourseResultDto?> DeleteCourseAsync(int courseId, int actingAdminId, bool force)
    {
        _logger.LogInformation("AdminCourseService::DeleteCourseAsync: called with CourseId={CourseId} ActingAdminId={adminId} Force={Force}",
            courseId, actingAdminId, force);

        var result = await _courseService.DeleteCourseAsync(courseId, actingAdminId, force);
        if (result is null)
            return null;

        await DeleteStorageObjectsAsync(result.StorageObjectKeys);
        return result;
    }

    // The course rows are already gone by the time we get here, so a storage failure must never
    // surface as a failed delete — it would invite a retry against a course that no longer exists.
    // A leaked blob is recoverable; a misleading error is not. The catch stays broad because the
    // concrete MinIO exception types live in LF.Infrastructure and cannot be named from here;
    // cancellation is deliberately excluded so a shutdown still unwinds.
    private async Task DeleteStorageObjectsAsync(IReadOnlyList<string> objectKeys)
    {
        foreach (var objectKey in objectKeys)
        {
            try
            {
                await _fileStorageService.DeleteAsync(objectKey);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "AdminCourseService::DeleteCourseAsync: failed to delete storage object {ObjectKey}", objectKey);
            }
        }
    }
}
