using LF.Application.ModelDto.Enrollment;
using LF.Application.Services.Enrollment;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.EnrollmentLearning;

internal sealed class EnrollmentLearningService(ILogger<EnrollmentLearningService> logger, IGrpcEnrollmentService grpcEnrollmentService) : IEnrollmentLearningService
{
    private readonly ILogger<EnrollmentLearningService> _logger = logger;
    private readonly IGrpcEnrollmentService _grpcEnrollmentService = grpcEnrollmentService;

    public async Task<PagedCourseCatalogDto> BrowseCatalogAsync(int page, int pageSize, int actingUserId)
    {
        _logger.LogInformation("EnrollmentLearningService::BrowseCatalogAsync: called with Page={Page} PageSize={PageSize} ActingUserId={ActingUserId}",
            page, pageSize, actingUserId);

        return await _grpcEnrollmentService.BrowseCatalogAsync(page, pageSize, actingUserId);
    }

    public async Task<EnrollmentDetailDto> EnrollAsync(int courseId, int actingUserId)
    {
        _logger.LogInformation("EnrollmentLearningService::EnrollAsync: called with CourseId={CourseId} ActingUserId={ActingUserId}", courseId, actingUserId);

        return await _grpcEnrollmentService.EnrollAsync(courseId, actingUserId);
    }

    public async Task<IReadOnlyList<EnrollmentSummaryDto>> ListMyEnrollmentsAsync(int actingUserId, EnrollmentStatusFilter status)
    {
        _logger.LogInformation("EnrollmentLearningService::ListMyEnrollmentsAsync: called with ActingUserId={ActingUserId} Status={Status}", actingUserId, status);

        return await _grpcEnrollmentService.ListMyEnrollmentsAsync(actingUserId, status);
    }

    public async Task<EnrollmentDetailDto?> GetEnrollmentAsync(int id, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("EnrollmentLearningService::GetEnrollmentAsync: called with Id={Id} ActingUserId={ActingUserId}", id, actingUserId);

        return await _grpcEnrollmentService.GetEnrollmentAsync(id, actingUserId, isAdmin);
    }

    public async Task<EnrollmentDetailDto?> CompleteLessonAsync(int id, int lessonId, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("EnrollmentLearningService::CompleteLessonAsync: called with Id={Id} LessonId={LessonId} ActingUserId={ActingUserId}",
            id, lessonId, actingUserId);

        return await _grpcEnrollmentService.CompleteLessonAsync(id, lessonId, actingUserId, isAdmin);
    }
}
