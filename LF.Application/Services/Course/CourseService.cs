using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.Course.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Course;
using LF.Application.ModelDto.Enrollment;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DomainCourse = LF.AppDomain.Entities.Course.Course;
using DomainEnrollment = LF.AppDomain.Entities.Course.Enrollment;

namespace LF.Application.Services.Course;

internal sealed class CourseService(
    ILogger<CourseService> logger,
    IAppDbContext dbContext,
    TimeProvider timeProvider,
    IHtmlSanitizer htmlSanitizer) : ICourseService
{
    private readonly ILogger<CourseService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IHtmlSanitizer _htmlSanitizer = htmlSanitizer;

    public async Task<CourseDetailDto> CreateCourseAsync(CreateCourseDto dto, int createdByUserId)
    {
        _logger.LogInformation("CourseService::CreateCourseAsync: called with Title={Title} CategoryId={CategoryId} CreatedByUserId={CreatedByUserId}",
            dto.Title, dto.CategoryId, createdByUserId);

        var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == dto.CategoryId);
        if (category is null)
            throw new ArgumentException($"Category {dto.CategoryId} not found.", nameof(dto));

        // Description is authored as rich HTML (TipTap); strip any script-bearing markup before it is stored.
        var course = DomainCourse.Create(dto.Title, dto.ShortIntroduction, _htmlSanitizer.Sanitize(dto.Description), category, createdByUserId,
            _timeProvider.GetUtcNow().UtcDateTime, dto.PricingType, dto.Price, dto.EnrollmentMode);

        switch (dto.CoverType)
        {
            case CourseCoverType.Color when dto.CoverColor is { } color:
                course.SetColorCover(color);
                break;
            case CourseCoverType.Image when dto.CoverImageStorageObjectId is { } storageObjectId:
                var storageObject = await _dbContext.StorageObjects.FirstOrDefaultAsync(s => s.Id == storageObjectId);
                if (storageObject is null)
                    throw new ArgumentException($"Storage object {storageObjectId} not found.", nameof(dto));
                course.SetImageCover(storageObject);
                break;
        }

        _dbContext.Courses.Add(course);
        await _dbContext.SaveChangesAsync();

        return course.Adapt<CourseDetailDto>();
    }

    public async Task<EnrollmentSummaryDto?> EnrollUserAsync(int courseId, int targetUserId, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseService::EnrollUserAsync: called with CourseId={CourseId} TargetUserId={TargetUserId} ActingUserId={ActingUserId}",
            courseId, targetUserId, actingUserId);

        var course = await _dbContext.Courses.AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Chapters).ThenInclude(ch => ch.Lessons)
            .Include(c => c.CoverImageStorageObject)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course is null)
            return null;

        EnsureOwnership(course, actingUserId, isAdmin);

        if (!course.IsPublished)
            throw new InvalidOperationException("Cannot add a student to an unpublished course.");

        if (course.CreatedByUserId == targetUserId)
            throw new InvalidOperationException("The course owner cannot be enrolled as a student.");

        var alreadyEnrolled = await _dbContext.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == targetUserId);
        if (alreadyEnrolled)
            throw new InvalidOperationException("The user is already enrolled in this course.");

        var enrollment = DomainEnrollment.Create(courseId, targetUserId, _timeProvider.GetUtcNow().UtcDateTime, EnrollmentStatus.Active, 0m);
        _dbContext.Enrollments.Add(enrollment);
        await _dbContext.SaveChangesAsync();

        var totalLessons = course.Chapters.Sum(ch => ch.Lessons.Count);
        return new EnrollmentSummaryDto
        {
            Id = enrollment.Id,
            CourseId = course.Id,
            CourseTitle = course.Title,
            CourseShortIntroduction = course.ShortIntroduction,
            CategoryName = course.Category.Name,
            Status = enrollment.Status,
            PricePaid = enrollment.PricePaid,
            TotalLessonCount = totalLessons,
            CompletedLessonCount = 0,
            ProgressPercent = 0,
            EnrolledAt = enrollment.EnrolledAt,
            CompletedAt = null,
            CoverType = course.CoverType,
            CoverColor = course.CoverColor,
            CoverImageKey = course.CoverImageStorageObject?.ObjectKey,
            CoverImageContentType = course.CoverImageStorageObject?.ContentType,
        };
    }

    public async Task<CourseDetailDto?> GetCourseAsync(int id, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseService::GetCourseAsync: called with Id={CourseId} ActingUserId={ActingUserId}", id, actingUserId);

        var course = await LoadCourseForReadAsync(id);
        if (course is null)
            return null;

        if (!isAdmin && course.CreatedByUserId != actingUserId)
            throw new CourseAuthorizationException("You do not have access to this course.");

        return course.Adapt<CourseDetailDto>();
    }

    public async Task<PagedCoursesDto> ListCoursesAsync(int page, int pageSize, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseService::ListCoursesAsync: called with Page={Page} PageSize={PageSize} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            page, pageSize, actingUserId, isAdmin);

        IQueryable<DomainCourse> query = _dbContext.Courses.AsNoTracking().Include(c => c.Category).Include(c => c.Chapters).Include(c => c.CoverImageStorageObject);
        if (!isAdmin)
            query = query.Where(c => c.CreatedByUserId == actingUserId);

        var totalCount = await query.CountAsync();
        var courses = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedCoursesDto { Items = courses.Adapt<List<CourseSummaryDto>>(), TotalCount = totalCount };
    }

    public async Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync()
    {
        _logger.LogInformation("CourseService::ListCategoriesAsync: called");

        var categories = await _dbContext.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        return categories.Adapt<List<CategoryDto>>();
    }

    public async Task<CategoryDto> CreateCategoryAsync(string name)
    {
        _logger.LogInformation("CourseService::CreateCategoryAsync: called with Name={Name}", name);

        var category = Category.Create(name);

        var duplicate = await _dbContext.Categories.AnyAsync(c => c.Name.ToLower() == category.Name.ToLower());
        if (duplicate)
            throw new ArgumentException($"Category '{category.Name}' already exists.", nameof(name));

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        return category.Adapt<CategoryDto>();
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        _logger.LogInformation("CourseService::DeleteCategoryAsync: called with Id={CategoryId}", id);

        var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
            return false;

        if (category.IsDefault)
            throw new CategoryProtectedException($"The default category '{category.Name}' cannot be deleted.");

        var inUse = await _dbContext.Courses.AnyAsync(c => c.CategoryId == id);
        if (inUse)
            throw new InvalidOperationException($"Category '{category.Name}' is still assigned to one or more courses.");

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<CourseDetailDto?> AddChapterAsync(int courseId, string title, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseService::AddChapterAsync: called with CourseId={CourseId} Title={Title} ActingUserId={ActingUserId}",
            courseId, title, actingUserId);

        var course = await LoadCourseForMutationAsync(courseId);
        if (course is null)
            return null;

        EnsureOwnership(course, actingUserId, isAdmin);

        course.AddChapter(title);
        await _dbContext.SaveChangesAsync();

        return course.Adapt<CourseDetailDto>();
    }

    public async Task<CourseDetailDto?> RenameChapterAsync(int courseId, int chapterId, string title, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseService::RenameChapterAsync: called with CourseId={CourseId} ChapterId={ChapterId} ActingUserId={ActingUserId}",
            courseId, chapterId, actingUserId);

        var course = await LoadCourseForMutationAsync(courseId);
        if (course is null)
            return null;

        EnsureOwnership(course, actingUserId, isAdmin);

        var chapter = course.Chapters.FirstOrDefault(c => c.Id == chapterId);
        if (chapter is null)
            return null;

        chapter.Rename(title);
        await _dbContext.SaveChangesAsync();

        return course.Adapt<CourseDetailDto>();
    }

    public async Task<CourseDetailDto?> MoveChapterAsync(int courseId, int chapterId, MoveDirection direction, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseService::MoveChapterAsync: called with CourseId={CourseId} ChapterId={ChapterId} Direction={Direction} ActingUserId={ActingUserId}",
            courseId, chapterId, direction, actingUserId);

        var course = await LoadCourseForMutationAsync(courseId);
        if (course is null)
            return null;

        EnsureOwnership(course, actingUserId, isAdmin);

        if (course.Chapters.All(c => c.Id != chapterId))
            return null;

        if (direction == MoveDirection.Up)
            course.MoveChapterUp(chapterId);
        else
            course.MoveChapterDown(chapterId);

        await _dbContext.SaveChangesAsync();

        return course.Adapt<CourseDetailDto>();
    }

    public async Task<CourseDetailDto?> AddLessonAsync(int courseId, int chapterId, AddLessonDto dto, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseService::AddLessonAsync: called with CourseId={CourseId} ChapterId={ChapterId} ActingUserId={ActingUserId}",
            courseId, chapterId, actingUserId);

        var course = await LoadCourseForMutationAsync(courseId);
        if (course is null)
            return null;

        EnsureOwnership(course, actingUserId, isAdmin);

        var chapter = course.Chapters.FirstOrDefault(c => c.Id == chapterId);
        if (chapter is null)
            return null;

        chapter.AddLesson(dto.Title, _htmlSanitizer.Sanitize(dto.Content), dto.IncludeInPreview);
        await _dbContext.SaveChangesAsync();

        return course.Adapt<CourseDetailDto>();
    }

    public async Task<CourseDetailDto?> UpdateLessonAsync(int courseId, int chapterId, int lessonId, UpdateLessonDto dto, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseService::UpdateLessonAsync: called with CourseId={CourseId} ChapterId={ChapterId} LessonId={LessonId} ActingUserId={ActingUserId}",
            courseId, chapterId, lessonId, actingUserId);

        var course = await LoadCourseForMutationAsync(courseId);
        if (course is null)
            return null;

        EnsureOwnership(course, actingUserId, isAdmin);

        var chapter = course.Chapters.FirstOrDefault(c => c.Id == chapterId);
        var lesson = chapter?.Lessons.FirstOrDefault(l => l.Id == lessonId);
        if (lesson is null)
            return null;

        lesson.Rename(dto.Title);
        lesson.UpdateContent(_htmlSanitizer.Sanitize(dto.Content));
        lesson.SetIncludeInPreview(dto.IncludeInPreview);
        await _dbContext.SaveChangesAsync();

        return course.Adapt<CourseDetailDto>();
    }

    public async Task<CourseDetailDto?> MoveLessonAsync(int courseId, int chapterId, int lessonId, MoveDirection direction, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseService::MoveLessonAsync: called with CourseId={CourseId} ChapterId={ChapterId} LessonId={LessonId} Direction={Direction} ActingUserId={ActingUserId}",
            courseId, chapterId, lessonId, direction, actingUserId);

        var course = await LoadCourseForMutationAsync(courseId);
        if (course is null)
            return null;

        EnsureOwnership(course, actingUserId, isAdmin);

        var chapter = course.Chapters.FirstOrDefault(c => c.Id == chapterId);
        if (chapter is null || chapter.Lessons.All(l => l.Id != lessonId))
            return null;

        if (direction == MoveDirection.Up)
            chapter.MoveLessonUp(lessonId);
        else
            chapter.MoveLessonDown(lessonId);

        await _dbContext.SaveChangesAsync();

        return course.Adapt<CourseDetailDto>();
    }

    public async Task<CourseDetailDto?> RemoveLessonAsync(int courseId, int chapterId, int lessonId, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseService::RemoveLessonAsync: called with CourseId={CourseId} ChapterId={ChapterId} LessonId={LessonId} ActingUserId={ActingUserId}",
            courseId, chapterId, lessonId, actingUserId);

        var course = await LoadCourseForMutationAsync(courseId);
        if (course is null)
            return null;

        EnsureOwnership(course, actingUserId, isAdmin);

        var chapter = course.Chapters.FirstOrDefault(c => c.Id == chapterId);
        if (chapter is null || chapter.Lessons.All(l => l.Id != lessonId))
            return null;

        chapter.RemoveLesson(lessonId);
        await _dbContext.SaveChangesAsync();

        return course.Adapt<CourseDetailDto>();
    }

    public async Task<CourseDetailDto?> ReplaceLessonPartsAsync(
        int courseId, int chapterId, int lessonId, IReadOnlyList<ReplaceLessonPartInputDto> parts, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseService::ReplaceLessonPartsAsync: called with CourseId={CourseId} ChapterId={ChapterId} LessonId={LessonId} ActingUserId={ActingUserId}",
            courseId, chapterId, lessonId, actingUserId);

        var course = await LoadCourseForMutationAsync(courseId);
        if (course is null)
            return null;

        EnsureOwnership(course, actingUserId, isAdmin);

        var chapter = course.Chapters.FirstOrDefault(c => c.Id == chapterId);
        var lesson = chapter?.Lessons.FirstOrDefault(l => l.Id == lessonId);
        if (lesson is null)
            return null;

        var storageObjectIds = parts
            .SelectMany(p => (p.StorageObjectId is { } id ? [id] : Enumerable.Empty<int>())
                .Concat((p.Files ?? []).Select(f => f.StorageObjectId)))
            .Distinct()
            .ToList();

        var storageObjectsById = storageObjectIds.Count == 0
            ? []
            : await _dbContext.StorageObjects.Where(s => storageObjectIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id);

        var inputs = new List<LessonPartInput>(parts.Count);
        foreach (var part in parts)
        {
            StorageObject? storageObject = null;
            if (part.StorageObjectId is { } storageObjectId)
            {
                if (!storageObjectsById.TryGetValue(storageObjectId, out storageObject))
                    throw new ArgumentException($"Storage object {storageObjectId} not found.", nameof(parts));
            }

            var quizQuestions = part.QuizQuestions?
                .Select(q => new QuizQuestionInput(
                    q.Text,
                    q.QuestionType,
                    q.SortOrder,
                    [.. q.Options.Select(o => new QuizOptionInput(o.Text, o.IsCorrect, o.SortOrder))]))
                .ToList();

            List<LessonPartFileInput>? files = null;
            if (part.Files is { Count: > 0 })
            {
                files = part.Files.Select(f =>
                {
                    if (!storageObjectsById.TryGetValue(f.StorageObjectId, out var fileStorageObject))
                        throw new ArgumentException($"Storage object {f.StorageObjectId} not found.", nameof(parts));

                    return new LessonPartFileInput(f.FileName, fileStorageObject);
                }).ToList();
            }

            // Text parts carry rich HTML (TipTap); other part types never render Html so leave them untouched.
            var html = part.PartType == LessonPartType.Text ? _htmlSanitizer.Sanitize(part.Html) : part.Html;
            inputs.Add(new LessonPartInput(part.PartType, html, storageObject, quizQuestions, part.QuizPassThresholdPercent, files));
        }

        lesson.ReplaceParts(inputs);
        await _dbContext.SaveChangesAsync();

        return course.Adapt<CourseDetailDto>();
    }

    public async Task<CourseDetailDto?> PublishCourseAsync(int courseId, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseService::PublishCourseAsync: called with CourseId={CourseId} ActingUserId={ActingUserId}", courseId, actingUserId);

        var course = await LoadCourseForMutationAsync(courseId);
        if (course is null)
            return null;

        EnsureOwnership(course, actingUserId, isAdmin);

        course.Publish();
        await _dbContext.SaveChangesAsync();

        return course.Adapt<CourseDetailDto>();
    }

    private static void EnsureOwnership(DomainCourse course, int actingUserId, bool isAdmin)
    {
        if (!isAdmin && course.CreatedByUserId != actingUserId)
            throw new CourseAuthorizationException("You do not have access to this course.");
    }

    // Ordering by SortOrder is applied in CourseMappingConfig when projecting to DTOs, not here —
    // EF's filtered-Include ordering syntax doesn't compose cleanly with ThenInclude + AsSplitQuery
    // for a get-only IReadOnlyList<T> navigation, and the domain mutators keep the in-memory
    // _chapters/_lessons lists correctly ordered anyway, so a plain Include is sufficient.
    private Task<DomainCourse?> LoadCourseForMutationAsync(int courseId) =>
        _dbContext.Courses
            .Include(c => c.Chapters)
            .ThenInclude(ch => ch.Lessons)
            .ThenInclude(l => l.Parts)
            .ThenInclude(p => p.StorageObject)
            .Include(c => c.Chapters)
            .ThenInclude(ch => ch.Lessons)
            .ThenInclude(l => l.Parts)
            .ThenInclude(p => p.QuizQuestions)
            .ThenInclude(q => q.Options)
            .Include(c => c.Chapters)
            .ThenInclude(ch => ch.Lessons)
            .ThenInclude(l => l.Parts)
            .ThenInclude(p => p.Files)
            .ThenInclude(f => f.StorageObject)
            .Include(c => c.Category)
            .Include(c => c.CoverImageStorageObject)
            .FirstOrDefaultAsync(c => c.Id == courseId);

    private Task<DomainCourse?> LoadCourseForReadAsync(int courseId) =>
        _dbContext.Courses
            .AsNoTracking()
            .Include(c => c.Chapters)
            .ThenInclude(ch => ch.Lessons)
            .ThenInclude(l => l.Parts)
            .ThenInclude(p => p.StorageObject)
            .Include(c => c.Chapters)
            .ThenInclude(ch => ch.Lessons)
            .ThenInclude(l => l.Parts)
            .ThenInclude(p => p.QuizQuestions)
            .ThenInclude(q => q.Options)
            .Include(c => c.Chapters)
            .ThenInclude(ch => ch.Lessons)
            .ThenInclude(l => l.Parts)
            .ThenInclude(p => p.Files)
            .ThenInclude(f => f.StorageObject)
            .Include(c => c.Category)
            .Include(c => c.CoverImageStorageObject)
            .FirstOrDefaultAsync(c => c.Id == courseId);
}
