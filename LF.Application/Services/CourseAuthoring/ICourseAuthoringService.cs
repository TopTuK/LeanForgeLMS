using LF.Application.ModelDto.Course;

namespace LF.Application.Services.CourseAuthoring;

public interface ICourseAuthoringService
{
    Task<CourseDetailDto> CreateCourseAsync(CreateCourseDto dto, int createdByUserId);
    Task<CourseDetailDto?> GetCourseAsync(int id, int actingUserId, bool isAdmin);
    Task<PagedCoursesDto> ListCoursesAsync(int page, int pageSize, int actingUserId, bool isAdmin);
    Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync();
    Task<CategoryDto> CreateCategoryAsync(string name);
    Task<bool> DeleteCategoryAsync(int id);
    Task<CourseDetailDto?> AddChapterAsync(int courseId, string title, int actingUserId, bool isAdmin);
    Task<CourseDetailDto?> RenameChapterAsync(int courseId, int chapterId, string title, int actingUserId, bool isAdmin);
    Task<CourseDetailDto?> MoveChapterAsync(int courseId, int chapterId, MoveDirection direction, int actingUserId, bool isAdmin);
    Task<CourseDetailDto?> AddLessonAsync(int courseId, int chapterId, AddLessonDto dto, int actingUserId, bool isAdmin);
    Task<CourseDetailDto?> UpdateLessonAsync(int courseId, int chapterId, int lessonId, UpdateLessonDto dto, int actingUserId, bool isAdmin);
    Task<CourseDetailDto?> MoveLessonAsync(int courseId, int chapterId, int lessonId, MoveDirection direction, int actingUserId, bool isAdmin);
    Task<CourseDetailDto?> RemoveLessonAsync(int courseId, int chapterId, int lessonId, int actingUserId, bool isAdmin);
    Task<CourseDetailDto?> PublishCourseAsync(int courseId, int actingUserId, bool isAdmin);
}
