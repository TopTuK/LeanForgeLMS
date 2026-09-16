using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Course;
using LF.Application.ModelDto.Enrollment;
using LF.Application.Services.Course;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.CourseAuthoring;

internal sealed class CourseAuthoringService(
    ILogger<CourseAuthoringService> logger,
    IGrpcCourseService grpcCourseService,
    [FromKeyedServices("storage")] IFileStorageService fileStorageService) : ICourseAuthoringService
{
    private readonly ILogger<CourseAuthoringService> _logger = logger;
    private readonly IGrpcCourseService _grpcCourseService = grpcCourseService;
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public async Task<CourseDetailDto> CreateCourseAsync(CreateCourseDto dto, int createdByUserId)
    {
        _logger.LogInformation("CourseAuthoringService::CreateCourseAsync: called with Title={Title} CreatedByUserId={CreatedByUserId}", dto.Title, createdByUserId);

        return await _grpcCourseService.CreateCourseAsync(dto, createdByUserId);
    }

    public async Task<EnrollmentSummaryDto?> EnrollUserAsync(int courseId, int targetUserId, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseAuthoringService::EnrollUserAsync: called with CourseId={CourseId} TargetUserId={TargetUserId} ActingUserId={ActingUserId}",
            courseId, targetUserId, actingUserId);

        return await _grpcCourseService.EnrollUserAsync(courseId, targetUserId, actingUserId, isAdmin);
    }

    public async Task<CourseDetailDto?> GetCourseAsync(int id, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseAuthoringService::GetCourseAsync: called with Id={CourseId} ActingUserId={ActingUserId}", id, actingUserId);

        return await _grpcCourseService.GetCourseAsync(id, actingUserId, isAdmin);
    }

    public async Task<PagedCoursesDto> ListCoursesAsync(int page, int pageSize, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseAuthoringService::ListCoursesAsync: called with Page={Page} PageSize={PageSize} ActingUserId={ActingUserId}", page, pageSize, actingUserId);

        return await _grpcCourseService.ListCoursesAsync(page, pageSize, actingUserId, isAdmin);
    }

    public async Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync()
    {
        _logger.LogInformation("CourseAuthoringService::ListCategoriesAsync: called");

        return await _grpcCourseService.ListCategoriesAsync();
    }

    public async Task<CategoryDto> CreateCategoryAsync(string name)
    {
        _logger.LogInformation("CourseAuthoringService::CreateCategoryAsync: called with Name={Name}", name);

        return await _grpcCourseService.CreateCategoryAsync(name);
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        _logger.LogInformation("CourseAuthoringService::DeleteCategoryAsync: called with Id={CategoryId}", id);

        return await _grpcCourseService.DeleteCategoryAsync(id);
    }

    public async Task<CourseDetailDto?> AddChapterAsync(int courseId, string title, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseAuthoringService::AddChapterAsync: called with CourseId={CourseId} ActingUserId={ActingUserId}", courseId, actingUserId);

        return await _grpcCourseService.AddChapterAsync(courseId, title, actingUserId, isAdmin);
    }

    public async Task<CourseDetailDto?> RenameChapterAsync(int courseId, int chapterId, string title, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseAuthoringService::RenameChapterAsync: called with CourseId={CourseId} ChapterId={ChapterId} ActingUserId={ActingUserId}",
            courseId, chapterId, actingUserId);

        return await _grpcCourseService.RenameChapterAsync(courseId, chapterId, title, actingUserId, isAdmin);
    }

    public async Task<CourseDetailDto?> MoveChapterAsync(int courseId, int chapterId, MoveDirection direction, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseAuthoringService::MoveChapterAsync: called with CourseId={CourseId} ChapterId={ChapterId} Direction={Direction} ActingUserId={ActingUserId}",
            courseId, chapterId, direction, actingUserId);

        return await _grpcCourseService.MoveChapterAsync(courseId, chapterId, direction, actingUserId, isAdmin);
    }

    public async Task<CourseDetailDto?> AddLessonAsync(int courseId, int chapterId, AddLessonDto dto, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseAuthoringService::AddLessonAsync: called with CourseId={CourseId} ChapterId={ChapterId} ActingUserId={ActingUserId}",
            courseId, chapterId, actingUserId);

        return await _grpcCourseService.AddLessonAsync(courseId, chapterId, dto, actingUserId, isAdmin);
    }

    public async Task<CourseDetailDto?> UpdateLessonAsync(int courseId, int chapterId, int lessonId, UpdateLessonDto dto, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseAuthoringService::UpdateLessonAsync: called with CourseId={CourseId} ChapterId={ChapterId} LessonId={LessonId} ActingUserId={ActingUserId}",
            courseId, chapterId, lessonId, actingUserId);

        return await _grpcCourseService.UpdateLessonAsync(courseId, chapterId, lessonId, dto, actingUserId, isAdmin);
    }

    public async Task<CourseDetailDto?> MoveLessonAsync(int courseId, int chapterId, int lessonId, MoveDirection direction, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseAuthoringService::MoveLessonAsync: called with CourseId={CourseId} ChapterId={ChapterId} LessonId={LessonId} Direction={Direction} ActingUserId={ActingUserId}",
            courseId, chapterId, lessonId, direction, actingUserId);

        return await _grpcCourseService.MoveLessonAsync(courseId, chapterId, lessonId, direction, actingUserId, isAdmin);
    }

    public async Task<CourseDetailDto?> RemoveLessonAsync(int courseId, int chapterId, int lessonId, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseAuthoringService::RemoveLessonAsync: called with CourseId={CourseId} ChapterId={ChapterId} LessonId={LessonId} ActingUserId={ActingUserId}",
            courseId, chapterId, lessonId, actingUserId);

        return await _grpcCourseService.RemoveLessonAsync(courseId, chapterId, lessonId, actingUserId, isAdmin);
    }

    public async Task<CourseDetailDto?> UpdateCourseDetailsAsync(int courseId, UpdateCourseDetailsDto dto, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseAuthoringService::UpdateCourseDetailsAsync: called with CourseId={CourseId} ActingUserId={ActingUserId}", courseId, actingUserId);

        var result = await _grpcCourseService.UpdateCourseDetailsAsync(courseId, dto, actingUserId, isAdmin);
        if (result is null)
            return null;

        await DeleteStorageObjectsAsync(result.OrphanedStorageObjectKeys);
        return result.Course;
    }

    public async Task<CourseDetailDto?> PublishCourseAsync(int courseId, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseAuthoringService::PublishCourseAsync: called with CourseId={CourseId} ActingUserId={ActingUserId}", courseId, actingUserId);

        return await _grpcCourseService.PublishCourseAsync(courseId, actingUserId, isAdmin);
    }

    public async Task<CourseDetailDto?> ReplaceLessonPartsAsync(
        int courseId, int chapterId, int lessonId, IReadOnlyList<ReplaceLessonPartInputDto> parts, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseAuthoringService::ReplaceLessonPartsAsync: called with CourseId={CourseId} ChapterId={ChapterId} LessonId={LessonId} ActingUserId={ActingUserId}",
            courseId, chapterId, lessonId, actingUserId);

        return await _grpcCourseService.ReplaceLessonPartsAsync(courseId, chapterId, lessonId, parts, actingUserId, isAdmin);
    }

    // The update has already committed, so a storage failure must not surface as a failed save.
    // A leaked blob is recoverable; a misleading error is not. The catch stays broad because the
    // concrete MinIO exception types live in LF.Infrastructure; cancellation still unwinds.
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
                _logger.LogWarning(ex, "CourseAuthoringService::UpdateCourseDetailsAsync: failed to delete storage object {ObjectKey}", objectKey);
            }
        }
    }
}
