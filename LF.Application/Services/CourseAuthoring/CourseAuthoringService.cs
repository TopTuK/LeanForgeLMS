using LF.Application.ModelDto.Course;
using LF.Application.Services.Course;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.CourseAuthoring;

internal sealed class CourseAuthoringService(ILogger<CourseAuthoringService> logger, IGrpcCourseService grpcCourseService) : ICourseAuthoringService
{
    private readonly ILogger<CourseAuthoringService> _logger = logger;
    private readonly IGrpcCourseService _grpcCourseService = grpcCourseService;

    public async Task<CourseDetailDto> CreateCourseAsync(CreateCourseDto dto, int createdByUserId)
    {
        _logger.LogInformation("CourseAuthoringService::CreateCourseAsync: called with Title={Title} CreatedByUserId={CreatedByUserId}", dto.Title, createdByUserId);

        return await _grpcCourseService.CreateCourseAsync(dto, createdByUserId);
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

    public async Task<CourseDetailDto?> PublishCourseAsync(int courseId, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseAuthoringService::PublishCourseAsync: called with CourseId={CourseId} ActingUserId={ActingUserId}", courseId, actingUserId);

        return await _grpcCourseService.PublishCourseAsync(courseId, actingUserId, isAdmin);
    }
}
