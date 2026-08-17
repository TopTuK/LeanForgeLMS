using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Enrollment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DomainCourse = LF.AppDomain.Entities.Course.Course;
using DomainEnrollment = LF.AppDomain.Entities.Course.Enrollment;

namespace LF.Application.Services.Enrollment;

internal sealed class EnrollmentService(ILogger<EnrollmentService> logger, IAppDbContext dbContext, TimeProvider timeProvider) : IEnrollmentService
{
    private readonly ILogger<EnrollmentService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<PagedCourseCatalogDto> BrowseCatalogAsync(int page, int pageSize, int actingUserId)
    {
        _logger.LogInformation("EnrollmentService::BrowseCatalogAsync: called with Page={Page} PageSize={PageSize} ActingUserId={ActingUserId}",
            page, pageSize, actingUserId);

        var enrolledCourseIds = await _dbContext.Enrollments
            .Where(e => e.UserId == actingUserId)
            .Select(e => e.CourseId)
            .ToListAsync();

        var query = _dbContext.Courses.AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Chapters)
            .ThenInclude(ch => ch.Lessons)
            .Include(c => c.CoverImageStorageObject)
            .Where(c => c.IsPublished && !enrolledCourseIds.Contains(c.Id));

        var totalCount = await query.CountAsync();
        var courses = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = courses.Select(c => new CourseCatalogItemDto
        {
            Id = c.Id,
            Title = c.Title,
            ShortIntroduction = c.ShortIntroduction,
            CategoryId = c.CategoryId,
            CategoryName = c.Category.Name,
            LessonCount = c.Chapters.Sum(ch => ch.Lessons.Count),
            CoverType = c.CoverType,
            CoverColor = c.CoverColor,
            CoverImageKey = c.CoverImageStorageObject?.ObjectKey,
            CoverImageContentType = c.CoverImageStorageObject?.ContentType,
        }).ToList();

        return new PagedCourseCatalogDto { Items = items, TotalCount = totalCount };
    }

    public async Task<EnrollmentDetailDto> EnrollAsync(int courseId, int actingUserId)
    {
        _logger.LogInformation("EnrollmentService::EnrollAsync: called with CourseId={CourseId} ActingUserId={ActingUserId}", courseId, actingUserId);

        var course = await LoadCourseAsync(courseId);
        if (course is null)
            throw new InvalidOperationException($"Course {courseId} not found.");

        if (!course.IsPublished)
            throw new InvalidOperationException("Cannot enroll in an unpublished course.");

        var alreadyEnrolled = await _dbContext.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == actingUserId);
        if (alreadyEnrolled)
            throw new InvalidOperationException("Already enrolled in this course.");

        var enrollment = DomainEnrollment.Create(courseId, actingUserId, _timeProvider.GetUtcNow().UtcDateTime);
        _dbContext.Enrollments.Add(enrollment);
        await _dbContext.SaveChangesAsync();

        return ToDetailDto(enrollment, course);
    }

    public async Task<IReadOnlyList<EnrollmentSummaryDto>> ListMyEnrollmentsAsync(int actingUserId, EnrollmentStatusFilter status)
    {
        _logger.LogInformation("EnrollmentService::ListMyEnrollmentsAsync: called with ActingUserId={ActingUserId} Status={Status}", actingUserId, status);

        IQueryable<DomainEnrollment> query = _dbContext.Enrollments.AsNoTracking().Where(e => e.UserId == actingUserId);
        query = status switch
        {
            EnrollmentStatusFilter.Active => query.Where(e => e.CompletedAt == null),
            EnrollmentStatusFilter.Completed => query.Where(e => e.CompletedAt != null),
            _ => query,
        };

        var enrollments = await query.ToListAsync();
        var courseIds = enrollments.Select(e => e.CourseId).Distinct().ToList();
        var courses = await _dbContext.Courses.AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Chapters)
            .ThenInclude(ch => ch.Lessons)
            .Include(c => c.CoverImageStorageObject)
            .Where(c => courseIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        return [.. enrollments
            .Where(e => courses.ContainsKey(e.CourseId))
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e =>
            {
                var course = courses[e.CourseId];
                var totalLessons = course.Chapters.Sum(ch => ch.Lessons.Count);
                return new EnrollmentSummaryDto
                {
                    Id = e.Id,
                    CourseId = course.Id,
                    CourseTitle = course.Title,
                    CourseShortIntroduction = course.ShortIntroduction,
                    CategoryName = course.Category.Name,
                    TotalLessonCount = totalLessons,
                    CompletedLessonCount = e.CompletedLessonIds.Length,
                    ProgressPercent = e.ProgressPercent(totalLessons),
                    EnrolledAt = e.EnrolledAt,
                    CompletedAt = e.CompletedAt,
                    CoverType = course.CoverType,
                    CoverColor = course.CoverColor,
                    CoverImageKey = course.CoverImageStorageObject?.ObjectKey,
                    CoverImageContentType = course.CoverImageStorageObject?.ContentType,
                };
            })];
    }

    public async Task<EnrollmentDetailDto?> GetEnrollmentAsync(int id, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("EnrollmentService::GetEnrollmentAsync: called with Id={Id} ActingUserId={ActingUserId}", id, actingUserId);

        var enrollment = await _dbContext.Enrollments.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (enrollment is null)
            return null;

        EnsureOwnership(enrollment, actingUserId, isAdmin);

        var course = await LoadCourseAsync(enrollment.CourseId);
        return course is null ? null : ToDetailDto(enrollment, course);
    }

    public async Task<EnrollmentDetailDto?> CompleteLessonAsync(int id, int lessonId, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("EnrollmentService::CompleteLessonAsync: called with Id={Id} LessonId={LessonId} ActingUserId={ActingUserId}",
            id, lessonId, actingUserId);

        var enrollment = await _dbContext.Enrollments.FirstOrDefaultAsync(e => e.Id == id);
        if (enrollment is null)
            return null;

        EnsureOwnership(enrollment, actingUserId, isAdmin);

        var course = await LoadCourseAsync(enrollment.CourseId);
        if (course is null)
            return null;

        var lessonExists = course.Chapters.Any(ch => ch.Lessons.Any(l => l.Id == lessonId));
        if (!lessonExists)
            throw new InvalidOperationException($"Lesson {lessonId} not found on this course.");

        var totalLessons = course.Chapters.Sum(ch => ch.Lessons.Count);
        var changed = enrollment.CompleteLesson(lessonId);
        enrollment.RecalculateCompletion(totalLessons, _timeProvider.GetUtcNow().UtcDateTime);

        if (changed)
            await _dbContext.SaveChangesAsync();

        return ToDetailDto(enrollment, course);
    }

    public async Task<CourseCoverDto?> GetCourseCoverAsync(int courseId)
    {
        _logger.LogInformation("EnrollmentService::GetCourseCoverAsync: called with CourseId={CourseId}", courseId);

        // No ownership/enrollment check beyond "published" — a course can only be enrolled in once
        // published, so this single check already covers both the catalog and enrolled-course cases.
        var course = await _dbContext.Courses.AsNoTracking()
            .Include(c => c.CoverImageStorageObject)
            .FirstOrDefaultAsync(c => c.Id == courseId && c.IsPublished);

        if (course?.CoverImageStorageObject is not { } storageObject)
            return null;

        return new CourseCoverDto { CoverImageKey = storageObject.ObjectKey, CoverImageContentType = storageObject.ContentType };
    }

    private static void EnsureOwnership(DomainEnrollment enrollment, int actingUserId, bool isAdmin)
    {
        if (!isAdmin && enrollment.UserId != actingUserId)
            throw new EnrollmentAuthorizationException("You do not have access to this enrollment.");
    }

    private Task<DomainCourse?> LoadCourseAsync(int courseId) =>
        _dbContext.Courses
            .AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Chapters)
            .ThenInclude(ch => ch.Lessons)
            .FirstOrDefaultAsync(c => c.Id == courseId);

    private static EnrollmentDetailDto ToDetailDto(DomainEnrollment enrollment, DomainCourse course) => new()
    {
        Id = enrollment.Id,
        CourseId = course.Id,
        CourseTitle = course.Title,
        CourseDescription = course.Description,
        EnrolledAt = enrollment.EnrolledAt,
        CompletedAt = enrollment.CompletedAt,
        Chapters = [.. course.Chapters
            .OrderBy(ch => ch.SortOrder)
            .Select(ch => new EnrollmentChapterDto
            {
                Id = ch.Id,
                Title = ch.Title,
                SortOrder = ch.SortOrder,
                Lessons = [.. ch.Lessons
                    .OrderBy(l => l.SortOrder)
                    .Select(l => new EnrollmentLessonDto
                    {
                        Id = l.Id,
                        Title = l.Title,
                        Content = l.Content,
                        SortOrder = l.SortOrder,
                        IsCompleted = enrollment.CompletedLessonIds.Contains(l.Id),
                    })],
            })],
    };
}
