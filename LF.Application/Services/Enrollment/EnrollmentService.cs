using LF.AppDomain.Entities.Course;
using LF.AppDomain.Models.Course.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Course;
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
            .Where(c => c.IsPublished && c.CreatedByUserId != actingUserId && !enrolledCourseIds.Contains(c.Id));

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

        if (course.CreatedByUserId == actingUserId)
            throw new SelfEnrollmentException("You cannot enroll in a course you created.");

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

        var lesson = course.Chapters.SelectMany(ch => ch.Lessons).FirstOrDefault(l => l.Id == lessonId);
        if (lesson is null)
            throw new InvalidOperationException($"Lesson {lessonId} not found on this course.");

        if (lesson.Parts.Any(p => p.PartType == LessonPartType.Quiz))
            throw new QuizCompletionRequiredException("This lesson contains a quiz and can only be completed by passing it.");

        var totalLessons = course.Chapters.Sum(ch => ch.Lessons.Count);
        var changed = enrollment.CompleteLesson(lessonId);
        enrollment.RecalculateCompletion(totalLessons, _timeProvider.GetUtcNow().UtcDateTime);

        if (changed)
            await _dbContext.SaveChangesAsync();

        return ToDetailDto(enrollment, course);
    }

    public async Task<QuizSubmissionDto?> SubmitQuizAttemptAsync(
        int id, int lessonId, int partId, IReadOnlyList<QuizAnswerInputDto> answers, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("EnrollmentService::SubmitQuizAttemptAsync: called with Id={Id} LessonId={LessonId} PartId={PartId} ActingUserId={ActingUserId}",
            id, lessonId, partId, actingUserId);

        var enrollment = await _dbContext.Enrollments.FirstOrDefaultAsync(e => e.Id == id);
        if (enrollment is null)
            return null;

        EnsureOwnership(enrollment, actingUserId, isAdmin);

        var course = await LoadCourseAsync(enrollment.CourseId);
        if (course is null)
            return null;

        var lesson = course.Chapters.SelectMany(ch => ch.Lessons).FirstOrDefault(l => l.Id == lessonId);
        var quizPart = lesson?.Parts.FirstOrDefault(p => p.Id == partId && p.PartType == LessonPartType.Quiz);
        if (quizPart is null)
            throw new InvalidOperationException($"Quiz part {partId} not found on lesson {lessonId}.");

        var selectedByQuestionId = answers.ToDictionary(a => a.QuestionId, IReadOnlyList<int> (a) => a.SelectedOptionIds);
        var attempt = QuizAttempt.Grade(quizPart, selectedByQuestionId, enrollment.Id, lessonId, _timeProvider.GetUtcNow());

        _dbContext.QuizAttempts.Add(attempt);

        if (attempt.Passed)
        {
            var totalLessons = course.Chapters.Sum(ch => ch.Lessons.Count);
            enrollment.CompleteLesson(lessonId);
            enrollment.RecalculateCompletion(totalLessons, _timeProvider.GetUtcNow().UtcDateTime);
        }

        await _dbContext.SaveChangesAsync();

        var result = new QuizAttemptResultDto
        {
            ScorePercent = attempt.ScorePercent,
            Passed = attempt.Passed,
            Questions = [.. quizPart.QuizQuestions.Select(q => new QuizQuestionResultDto
            {
                QuestionId = q.Id,
                IsCorrect = q.IsAnsweredCorrectly(selectedByQuestionId.TryGetValue(q.Id, out var selected) ? selected : []),
                CorrectOptionIds = [.. q.Options.Where(o => o.IsCorrect).Select(o => o.Id)],
            })],
        };

        return new QuizSubmissionDto { Result = result, Enrollment = ToDetailDto(enrollment, course) };
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

    public async Task<CoursePreviewDto?> GetCoursePreviewAsync(int courseId, int actingUserId)
    {
        _logger.LogInformation("EnrollmentService::GetCoursePreviewAsync: called with CourseId={CourseId} ActingUserId={ActingUserId}", courseId, actingUserId);

        var course = await LoadCourseAsync(courseId);
        if (course is null || !course.IsPublished)
            return null;

        var enrollment = await _dbContext.Enrollments.AsNoTracking()
            .FirstOrDefaultAsync(e => e.CourseId == courseId && e.UserId == actingUserId);

        return new CoursePreviewDto
        {
            Id = course.Id,
            Title = course.Title,
            ShortIntroduction = course.ShortIntroduction,
            Description = course.Description,
            CategoryId = course.CategoryId,
            CategoryName = course.Category.Name,
            LessonCount = course.Chapters.Sum(ch => ch.Lessons.Count),
            CoverType = course.CoverType,
            CoverColor = course.CoverColor,
            CoverImageKey = course.CoverImageStorageObject?.ObjectKey,
            CoverImageContentType = course.CoverImageStorageObject?.ContentType,
            IsEnrolled = enrollment is not null,
            EnrollmentId = enrollment?.Id,
            Chapters = [.. course.Chapters
                .OrderBy(ch => ch.SortOrder)
                .Select(ch => new CoursePreviewChapterDto
                {
                    Id = ch.Id,
                    Title = ch.Title,
                    SortOrder = ch.SortOrder,
                    Lessons = [.. ch.Lessons
                        .OrderBy(l => l.SortOrder)
                        .Select(ToPreviewLessonDto)],
                })],
        };
    }

    // Non-preview lessons must never carry Content/Parts past this point — this is the only
    // read path a not-yet-enrolled student can reach, so the omission here is the enforcement.
    private static CoursePreviewLessonDto ToPreviewLessonDto(LF.AppDomain.Entities.Course.Lesson lesson) => new()
    {
        Id = lesson.Id,
        Title = lesson.Title,
        SortOrder = lesson.SortOrder,
        IncludeInPreview = lesson.IncludeInPreview,
        Content = lesson.IncludeInPreview ? lesson.Content : null,
        Parts = lesson.IncludeInPreview
            ? [.. lesson.Parts
                .OrderBy(p => p.SortOrder)
                .Select(p => new LessonPartDto
                {
                    Id = p.Id,
                    PartType = p.PartType,
                    SortOrder = p.SortOrder,
                    Html = p.Html,
                    StorageObjectId = p.StorageObjectId,
                    StorageObjectKey = p.StorageObject?.ObjectKey,
                    StorageObjectContentType = p.StorageObject?.ContentType,
                    QuizPassThresholdPercent = p.QuizPassThresholdPercent,
                    QuizQuestions = [.. p.QuizQuestions
                        .OrderBy(q => q.SortOrder)
                        .Select(q => new QuizQuestionDto
                        {
                            Id = q.Id,
                            Text = q.Text,
                            QuestionType = q.QuestionType,
                            SortOrder = q.SortOrder,
                            Options = [.. q.Options
                                .OrderBy(o => o.SortOrder)
                                .Select(o => new QuizOptionDto
                                {
                                    Id = o.Id,
                                    Text = o.Text,
                                    IsCorrect = o.IsCorrect,
                                    SortOrder = o.SortOrder,
                                })],
                        })],
                    Files = [.. p.Files
                        .OrderBy(f => f.SortOrder)
                        .Select(f => new LessonPartFileDto
                        {
                            Id = f.Id,
                            FileName = f.FileName,
                            SortOrder = f.SortOrder,
                            StorageObjectId = f.StorageObjectId,
                            StorageObjectKey = f.StorageObject.ObjectKey,
                            StorageObjectContentType = f.StorageObject.ContentType,
                            StorageObjectSizeBytes = f.StorageObject.SizeBytes,
                        })],
                })]
            : [],
    };

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
                        Parts = [.. l.Parts
                            .OrderBy(p => p.SortOrder)
                            .Select(p => new LessonPartDto
                            {
                                Id = p.Id,
                                PartType = p.PartType,
                                SortOrder = p.SortOrder,
                                Html = p.Html,
                                StorageObjectId = p.StorageObjectId,
                                StorageObjectKey = p.StorageObject?.ObjectKey,
                                StorageObjectContentType = p.StorageObject?.ContentType,
                                QuizPassThresholdPercent = p.QuizPassThresholdPercent,
                                QuizQuestions = [.. p.QuizQuestions
                                    .OrderBy(q => q.SortOrder)
                                    .Select(q => new QuizQuestionDto
                                    {
                                        Id = q.Id,
                                        Text = q.Text,
                                        QuestionType = q.QuestionType,
                                        SortOrder = q.SortOrder,
                                        Options = [.. q.Options
                                            .OrderBy(o => o.SortOrder)
                                            .Select(o => new QuizOptionDto
                                            {
                                                Id = o.Id,
                                                Text = o.Text,
                                                IsCorrect = o.IsCorrect,
                                                SortOrder = o.SortOrder,
                                            })],
                                    })],
                                Files = [.. p.Files
                                    .OrderBy(f => f.SortOrder)
                                    .Select(f => new LessonPartFileDto
                                    {
                                        Id = f.Id,
                                        FileName = f.FileName,
                                        SortOrder = f.SortOrder,
                                        StorageObjectId = f.StorageObjectId,
                                        StorageObjectKey = f.StorageObject.ObjectKey,
                                        StorageObjectContentType = f.StorageObject.ContentType,
                                        StorageObjectSizeBytes = f.StorageObject.SizeBytes,
                                    })],
                            })],
                    })],
            })],
    };
}
