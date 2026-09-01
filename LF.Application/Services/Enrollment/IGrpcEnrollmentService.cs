using LF.Application.ModelDto.Enrollment;
using LF.Application.ModelDto.Promo;

namespace LF.Application.Services.Enrollment;

public interface IGrpcEnrollmentService
{
    Task<PagedCourseCatalogDto> BrowseCatalogAsync(int page, int pageSize, int actingUserId);
    Task<EnrollmentDetailDto> EnrollAsync(int courseId, int actingUserId, string? promoCode = null);
    Task<EnrollmentActivationDto> ActivatePaidEnrollmentAsync(int enrollmentId, decimal paidAmount);
    Task<PromoCodeValidationDto> ValidatePromoCodeAsync(string code, int courseId, int actingUserId);
    Task<IReadOnlyList<EnrollmentSummaryDto>> ListMyEnrollmentsAsync(int actingUserId, EnrollmentStatusFilter status);
    Task<EnrollmentDetailDto?> GetEnrollmentAsync(int id, int actingUserId, bool isAdmin);
    Task<EnrollmentDetailDto?> CompleteLessonAsync(int id, int lessonId, int actingUserId, bool isAdmin);
    Task<QuizSubmissionDto?> SubmitQuizAttemptAsync(int id, int lessonId, int partId, IReadOnlyList<QuizAnswerInputDto> answers, int actingUserId, bool isAdmin);
    Task<CourseCoverDto?> GetCourseCoverAsync(int courseId);
    Task<CoursePreviewDto?> GetCoursePreviewAsync(int courseId, int actingUserId);
}
