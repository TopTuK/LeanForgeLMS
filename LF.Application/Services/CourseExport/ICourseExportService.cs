using LF.Application.ModelDto.CourseExport;

namespace LF.Application.Services.CourseExport;

public interface ICourseExportService
{
    /// <summary>
    /// Builds a PDF of the enrolled course. Access rules are those of the learn view.
    /// </summary>
    /// <returns>null when the enrollment does not exist.</returns>
    /// <exception cref="Common.Exceptions.EnrollmentAuthorizationException">The caller may not read this enrollment.</exception>
    /// <exception cref="Common.Exceptions.CourseUnavailableException">The course has been unpublished.</exception>
    Task<CourseExportFileDto?> ExportEnrollmentPdfAsync(int enrollmentId, int actingUserId, bool isAdmin, string? language, CancellationToken ct);
}
