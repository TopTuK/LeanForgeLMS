using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.Course.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Course;
using LF.Application.ModelDto.Enrollment;
using LF.Application.Services.Notifications;
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
    IHtmlSanitizer htmlSanitizer,
    IEnrollmentNotifier enrollmentNotifier,
    ICourseChangeTracker courseChangeTracker) : ICourseService
{
    private readonly ILogger<CourseService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IHtmlSanitizer _htmlSanitizer = htmlSanitizer;
    private readonly IEnrollmentNotifier _enrollmentNotifier = enrollmentNotifier;
    private readonly ICourseChangeTracker _courseChangeTracker = courseChangeTracker;

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

        // Manual enrollment is admin-only: it is the sole way into a Managed (private) course,
        // so it must not be reachable by a course owner acting on their own course.
        if (!isAdmin)
            throw new CourseAuthorizationException("Only an administrator can enroll a student in a course.");

        if (!course.IsPublished)
            throw new InvalidOperationException("Cannot add a student to an unpublished course.");

        if (course.CreatedByUserId == targetUserId)
            throw new InvalidOperationException("The course owner cannot be enrolled as a student.");

        var alreadyEnrolled = await _dbContext.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == targetUserId);
        if (alreadyEnrolled)
            throw new InvalidOperationException("The user is already enrolled in this course.");

        var enrollment = DomainEnrollment.Create(courseId, targetUserId, _timeProvider.GetUtcNow().UtcDateTime, EnrollmentStatus.Active, 0m);
        _dbContext.Enrollments.Add(enrollment);
        await _enrollmentNotifier.StageEnrollmentConfirmationAsync(targetUserId, course.Title);
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

        var lesson = chapter.AddLesson(dto.Title, _htmlSanitizer.Sanitize(dto.Content), dto.IncludeInPreview);
        await _courseChangeTracker.TrackLessonChangeAsync(course, lesson, CourseContentChangeKind.LessonAdded);
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

        // Non-short-circuiting '|': both edits must apply. The preview flag only affects the public
        // course page, not what enrolled students see, so it is not announced.
        var contentChanged = lesson.Rename(dto.Title) | lesson.UpdateContent(_htmlSanitizer.Sanitize(dto.Content));
        lesson.SetIncludeInPreview(dto.IncludeInPreview);
        if (contentChanged)
            await _courseChangeTracker.TrackLessonChangeAsync(course, lesson, CourseContentChangeKind.LessonUpdated);

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

        if (lesson.ReplaceParts(inputs))
            await _courseChangeTracker.TrackLessonChangeAsync(course, lesson, CourseContentChangeKind.LessonUpdated);

        await _dbContext.SaveChangesAsync();

        return course.Adapt<CourseDetailDto>();
    }

    public async Task<UpdateCourseDetailsResultDto?> UpdateCourseDetailsAsync(int courseId, UpdateCourseDetailsDto dto, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("CourseService::UpdateCourseDetailsAsync: called with CourseId={CourseId} CategoryId={CategoryId} ActingUserId={ActingUserId}",
            courseId, dto.CategoryId, actingUserId);

        var course = await LoadCourseForMutationAsync(courseId);
        if (course is null)
            return null;

        EnsureOwnership(course, actingUserId, isAdmin);

        var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == dto.CategoryId);
        if (category is null)
            throw new ArgumentException($"Category {dto.CategoryId} not found.", nameof(dto));

        // Runs first: it is the draft-only guard, so a published course is rejected before its cover is touched.
        course.UpdateDetails(dto.Title, dto.ShortIntroduction, _htmlSanitizer.Sanitize(dto.Description), category,
            dto.PricingType, dto.Price, dto.EnrollmentMode);

        var previousImage = course.CoverImageStorageObject;

        switch (dto.CoverType)
        {
            case CourseCoverType.Color when dto.CoverColor is { } color:
                course.SetColorCover(color);
                break;
            case CourseCoverType.Image when dto.CoverImageStorageObjectId is { } storageObjectId:
                if (storageObjectId != previousImage?.Id)
                {
                    var storageObject = await _dbContext.StorageObjects.FirstOrDefaultAsync(s => s.Id == storageObjectId);
                    if (storageObject is null)
                        throw new ArgumentException($"Storage object {storageObjectId} not found.", nameof(dto));
                    course.SetImageCover(storageObject);
                }
                break;
            case CourseCoverType.Image when previousImage is null:
                throw new ArgumentException("An image cover requires an uploaded image.", nameof(dto));
            case CourseCoverType.None:
                course.ClearCover();
                break;
        }

        List<string> orphanedKeys = [];
        if (previousImage is not null && course.CoverImageStorageObjectId != previousImage.Id)
        {
            _dbContext.StorageObjects.Remove(previousImage);
            orphanedKeys.Add(previousImage.ObjectKey);
        }

        await _dbContext.SaveChangesAsync();

        return new UpdateCourseDetailsResultDto
        {
            Course = course.Adapt<CourseDetailDto>(),
            OrphanedStorageObjectKeys = orphanedKeys,
        };
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

    // Admin-only (enforced by the AdminOnly endpoint policy). Enrollments stay untouched; while the
    // course is unpublished EnrollmentService withholds its content from enrolled students.
    public async Task<UnpublishCourseResultDto?> UnpublishCourseAsync(int courseId, int actingUserId)
    {
        _logger.LogInformation("CourseService::UnpublishCourseAsync: called with CourseId={CourseId} ActingUserId={ActingUserId}", courseId, actingUserId);

        var course = await _dbContext.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        if (course is null)
            return null;

        course.Unpublish();

        var affectedEnrollmentCount = await _dbContext.Enrollments.CountAsync(e => e.CourseId == courseId);
        await _dbContext.SaveChangesAsync();

        return new UnpublishCourseResultDto { AffectedEnrollmentCount = affectedEnrollmentCount };
    }

    // Admin-only (enforced by the AdminOnly endpoint policy). The removal order below is dictated by
    // the schema, not by preference: PromoCode -> Course and Enrollment -> PromoCode are both
    // DeleteBehavior.Restrict, so a course-scoped promo code aborts the whole delete unless its
    // enrollments go first. Chapters/lessons/parts and QuizAttempts cascade on their own.
    // CoursePayment is deliberately left alone — that ledger is built to outlive the rows it snapshots.
    public async Task<DeleteCourseResultDto?> DeleteCourseAsync(int courseId, int actingUserId, bool force)
    {
        _logger.LogInformation("CourseService::DeleteCourseAsync: called with CourseId={CourseId} ActingUserId={ActingUserId} Force={Force}",
            courseId, actingUserId, force);

        var course = await LoadCourseForMutationAsync(courseId);
        if (course is null)
            return null;

        var enrollments = await _dbContext.Enrollments.Where(e => e.CourseId == courseId).ToListAsync();

        // A price on a PendingPayment row is only an intent to pay — no money changed hands.
        var paidCount = enrollments.Count(e => e.Status == EnrollmentStatus.Active && e.PricePaid > 0m);
        if (paidCount > 0 && !force)
        {
            throw new CourseDeletionBlockedException(
                $"{paidCount} student(s) have paid for this course. Deleting it removes their access without a refund.");
        }

        var enrollmentIds = enrollments.Select(e => e.Id).ToList();

        // Orphaned PaymentOrders are not just untidy: EnrollmentId carries no FK, so a late Robokassa
        // webhook would otherwise try to activate an enrollment that no longer exists.
        var paymentOrders = await _dbContext.PaymentOrders.Where(o => enrollmentIds.Contains(o.EnrollmentId)).ToListAsync();
        var promoCodes = await _dbContext.PromoCodes.Where(p => p.CourseId == courseId).ToListAsync();

        var storageObjects = CollectStorageObjects(course);

        _dbContext.Enrollments.RemoveRange(enrollments);
        _dbContext.PaymentOrders.RemoveRange(paymentOrders);
        _dbContext.PromoCodes.RemoveRange(promoCodes);
        _dbContext.Courses.Remove(course);
        _dbContext.StorageObjects.RemoveRange(storageObjects);

        await _dbContext.SaveChangesAsync();

        return new DeleteCourseResultDto
        {
            RemovedEnrollmentCount = enrollments.Count,
            PaidEnrollmentCount = paidCount,
            StorageObjectKeys = [.. storageObjects.Select(s => s.ObjectKey)],
        };
    }

    // Every StorageObject the course alone referenced. These are Restrict on the principal side, so
    // nothing cascades them — without this the rows and their MinIO blobs leak on every delete.
    private static List<StorageObject> CollectStorageObjects(DomainCourse course)
    {
        var parts = course.Chapters.SelectMany(ch => ch.Lessons).SelectMany(l => l.Parts).ToList();

        return [.. new[] { course.CoverImageStorageObject }
            .Concat(parts.Select(p => p.StorageObject))
            .Concat(parts.SelectMany(p => p.Files).Select(f => f.StorageObject))
            .OfType<StorageObject>()
            .DistinctBy(s => s.Id)];
    }

    public async Task<PagedCourseEnrollmentsDto?> ListCourseEnrollmentsAsync(int courseId, int actingUserId, int page, int pageSize)
    {
        _logger.LogInformation("CourseService::ListCourseEnrollmentsAsync: called with CourseId={CourseId} ActingUserId={ActingUserId} Page={Page} PageSize={PageSize}",
            courseId, actingUserId, page, pageSize);

        var course = await _dbContext.Courses.AsNoTracking()
            .Include(c => c.Chapters)
            .ThenInclude(ch => ch.Lessons)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course is null)
            return null;

        var totalLessonCount = course.Chapters.Sum(ch => ch.Lessons.Count);
        var query = _dbContext.Enrollments.AsNoTracking().Where(e => e.CourseId == courseId);

        var totalCount = await query.CountAsync();
        var enrollments = await query
            .OrderByDescending(e => e.EnrolledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedCourseEnrollmentsDto
        {
            TotalCount = totalCount,
            Items = [.. enrollments.Select(e => new CourseEnrollmentDto
            {
                Id = e.Id,
                UserId = e.UserId,
                Status = e.Status,
                PricePaid = e.PricePaid,
                EnrolledAt = e.EnrolledAt,
                CompletedAt = e.CompletedAt,
                TotalLessonCount = totalLessonCount,
                CompletedLessonCount = e.CompletedLessonIds.Length,
                ProgressPercent = e.ProgressPercent(totalLessonCount),
            })],
        };
    }

    // Admin-only. No refund is attempted — the payment stack has no refund path at all, and the
    // CoursePayment ledger row stays put so the money is still reported.
    public async Task<RemoveEnrollmentResultDto?> RemoveEnrollmentAsync(int courseId, int enrollmentId, int actingUserId)
    {
        _logger.LogInformation("CourseService::RemoveEnrollmentAsync: called with CourseId={CourseId} EnrollmentId={EnrollmentId} ActingUserId={ActingUserId}",
            courseId, enrollmentId, actingUserId);

        var enrollment = await _dbContext.Enrollments.FirstOrDefaultAsync(e => e.Id == enrollmentId && e.CourseId == courseId);
        if (enrollment is null)
            return null;

        var paymentOrders = await _dbContext.PaymentOrders.Where(o => o.EnrollmentId == enrollmentId).ToListAsync();

        _dbContext.Enrollments.Remove(enrollment);
        _dbContext.PaymentOrders.RemoveRange(paymentOrders);
        await _dbContext.SaveChangesAsync();

        return new RemoveEnrollmentResultDto
        {
            UserId = enrollment.UserId,
            WasPaid = enrollment.Status == EnrollmentStatus.Active && enrollment.PricePaid > 0m,
            PricePaid = enrollment.PricePaid,
        };
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
