using LF.Application.ModelDto.Enrollment;

namespace LF.Application.Services.Enrollment;

public interface IEnrollmentService
{
    Task<PagedCourseCatalogDto> BrowseCatalogAsync(int page, int pageSize, int actingUserId);
    Task<EnrollmentDetailDto> EnrollAsync(int courseId, int actingUserId);
    Task<IReadOnlyList<EnrollmentSummaryDto>> ListMyEnrollmentsAsync(int actingUserId, EnrollmentStatusFilter status);
    Task<EnrollmentDetailDto?> GetEnrollmentAsync(int id, int actingUserId, bool isAdmin);
    Task<EnrollmentDetailDto?> CompleteLessonAsync(int id, int lessonId, int actingUserId, bool isAdmin);
    Task<QuizSubmissionDto?> SubmitQuizAttemptAsync(int id, int lessonId, int partId, IReadOnlyList<QuizAnswerInputDto> answers, int actingUserId, bool isAdmin);
    Task<CourseCoverDto?> GetCourseCoverAsync(int courseId);
    Task<CoursePreviewDto?> GetCoursePreviewAsync(int courseId, int actingUserId);
}
