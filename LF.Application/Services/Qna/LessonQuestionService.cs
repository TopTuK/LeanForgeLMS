using LF.AppDomain.Entities.Qna;
using LF.AppDomain.Models.Qna.Enums;
using LF.Application.Common.Access;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Qna;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Qna;

internal sealed class LessonQuestionService(
    ILogger<LessonQuestionService> logger,
    IAppDbContext dbContext,
    TimeProvider timeProvider) : ILessonQuestionService
{
    private readonly ILogger<LessonQuestionService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<LessonQuestionThreadDto?> AskAsync(AskQuestionDto question, int studentUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(question);

        _logger.LogInformation("LessonQuestionService::AskAsync: called with LessonId={LessonId} StudentUserId={StudentUserId}",
            question.LessonId, studentUserId);

        var courseId = await ResolveCourseIdAsync(question.LessonId, cancellationToken);
        if (courseId is null)
            return null;

        if (!await _dbContext.IsEnrolledAsync(courseId.Value, studentUserId, cancellationToken))
            throw new QuestionAuthorizationException("Only students enrolled in this course can ask questions about its lessons.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var created = LessonQuestion.Ask(courseId.Value, question.LessonId, studentUserId, question.Title, question.Body, now);

        _dbContext.LessonQuestions.Add(created);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectThreadAsync(created.Id, studentUserId, cancellationToken);
    }

    public async Task<PagedLessonQuestionsDto> ListAsync(
        int actingUserId,
        LessonQuestionScope scope,
        int? courseId,
        LessonQuestionStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LessonQuestionService::ListAsync: called with ActingUserId={ActingUserId} Scope={Scope} CourseId={CourseId} Status={Status} Page={Page} PageSize={PageSize}",
            actingUserId, scope, courseId, status, page, pageSize);

        var query = scope == LessonQuestionScope.AsStudent
            ? _dbContext.LessonQuestions.AsNoTracking().Where(q => q.StudentUserId == actingUserId)
            : StaffQuestions(actingUserId);

        if (courseId is { } filterCourseId)
            query = query.Where(q => q.CourseId == filterCourseId);

        if (status is { } filterStatus)
            query = query.Where(q => q.Status == filterStatus);

        return await PageAsync(query, actingUserId, page, pageSize, cancellationToken);
    }

    public async Task<PagedLessonQuestionsDto> ListForLessonAsync(
        int lessonId,
        int actingUserId,
        bool isAdmin,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LessonQuestionService::ListForLessonAsync: called with LessonId={LessonId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            lessonId, actingUserId, isAdmin);

        var query = VisibleQuestions(actingUserId, isAdmin).Where(q => q.LessonId == lessonId);
        return await PageAsync(query, actingUserId, page, pageSize, cancellationToken);
    }

    public async Task<LessonQuestionThreadDto?> GetThreadAsync(int questionId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LessonQuestionService::GetThreadAsync: called with QuestionId={QuestionId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            questionId, actingUserId, isAdmin);

        var question = await _dbContext.LessonQuestions.AsNoTracking().FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken);
        if (question is null)
            return null;

        await EnsureAccessAsync(question, actingUserId, isAdmin, cancellationToken);

        // Opening a thread is what clears its badge, so this runs before the projection computes
        // HasUnread — the caller gets back a thread that is already marked read.
        await MarkSeenAsync(question, actingUserId, cancellationToken);

        return await ProjectThreadAsync(questionId, actingUserId, cancellationToken);
    }

    public async Task<LessonQuestionThreadDto?> PostMessageAsync(
        int questionId,
        PostQuestionMessageDto message,
        int actingUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        _logger.LogInformation("LessonQuestionService::PostMessageAsync: called with QuestionId={QuestionId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            questionId, actingUserId, isAdmin);

        var question = await LoadForUpdateAsync(questionId, cancellationToken);
        if (question is null)
            return null;

        await EnsureAccessAsync(question, actingUserId, isAdmin, cancellationToken);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        question.AddMessage(actingUserId, ResolveAuthorRole(question, actingUserId, isAdmin), message.Body, now);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // The author has by definition seen their own message. HasUnread already ignores it via
        // LastMessageAuthorUserId, but advancing the marker keeps the two in step for whoever
        // replies next.
        await MarkSeenAsync(question, actingUserId, cancellationToken);

        return await ProjectThreadAsync(questionId, actingUserId, cancellationToken);
    }

    public async Task<LessonQuestionThreadDto?> SetStatusAsync(int questionId, bool close, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LessonQuestionService::SetStatusAsync: called with QuestionId={QuestionId} Close={Close} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            questionId, close, actingUserId, isAdmin);

        var question = await LoadForUpdateAsync(questionId, cancellationToken);
        if (question is null)
            return null;

        await EnsureAccessAsync(question, actingUserId, isAdmin, cancellationToken);

        if (close)
            question.Close();
        else
            question.Reopen();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectThreadAsync(questionId, actingUserId, cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(int actingUserId, CancellationToken cancellationToken = default)
    {
        var staffCourseIds = StaffCourseIds(actingUserId);

        return await _dbContext.LessonQuestions
            .AsNoTracking()
            .Where(q => q.StudentUserId == actingUserId || staffCourseIds.Contains(q.CourseId))
            .Where(q => q.LastMessageAuthorUserId != actingUserId)
            .Where(q => !_dbContext.LessonQuestionReadMarkers.Any(m =>
                m.LessonQuestionId == q.Id && m.UserId == actingUserId && m.LastSeenAt >= q.LastMessageAt))
            .CountAsync(cancellationToken);
    }

    // Lesson carries only ChapterId, so the owning course is two levels up. Returns null when the
    // lesson doesn't exist, which the caller turns into a 404.
    private async Task<int?> ResolveCourseIdAsync(int lessonId, CancellationToken cancellationToken)
    {
        var courseId = await _dbContext.Courses
            .AsNoTracking()
            .Where(c => c.Chapters.Any(ch => ch.Lessons.Any(l => l.Id == lessonId)))
            .Select(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return courseId == 0 ? null : courseId;
    }

    private IQueryable<int> StaffCourseIds(int actingUserId) =>
        _dbContext.Courses
            .AsNoTracking()
            .Where(c => c.CreatedByUserId == actingUserId)
            .Select(c => c.Id)
            .Union(_dbContext.CourseInstructors
                .AsNoTracking()
                .Where(i => i.UserId == actingUserId)
                .Select(i => i.CourseId));

    private IQueryable<LessonQuestion> StaffQuestions(int actingUserId)
    {
        var staffCourseIds = StaffCourseIds(actingUserId);
        return _dbContext.LessonQuestions.AsNoTracking().Where(q => staffCourseIds.Contains(q.CourseId));
    }

    private IQueryable<LessonQuestion> VisibleQuestions(int actingUserId, bool isAdmin)
    {
        var all = _dbContext.LessonQuestions.AsNoTracking();
        if (isAdmin)
            return all;

        var staffCourseIds = StaffCourseIds(actingUserId);
        return all.Where(q => q.StudentUserId == actingUserId || staffCourseIds.Contains(q.CourseId));
    }

    private async Task<PagedLessonQuestionsDto> PageAsync(
        IQueryable<LessonQuestion> query,
        int viewerUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(q => q.LastMessageAt)
            .ThenByDescending(q => q.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToSummaries(_dbContext, viewerUserId)
            .ToListAsync(cancellationToken);

        return new PagedLessonQuestionsDto { Items = items, TotalCount = totalCount };
    }

    private Task<LessonQuestionThreadDto?> ProjectThreadAsync(int questionId, int viewerUserId, CancellationToken cancellationToken) =>
        _dbContext.LessonQuestions
            .AsNoTracking()
            .Where(q => q.Id == questionId)
            .ToThreads(_dbContext, viewerUserId)
            .FirstOrDefaultAsync(cancellationToken);

    // Messages are included because Reopen() inspects them and AddMessage appends to them; an
    // unloaded backing collection would make both silently wrong.
    private Task<LessonQuestion?> LoadForUpdateAsync(int questionId, CancellationToken cancellationToken) =>
        _dbContext.LessonQuestions
            .Include(q => q.Messages)
            .FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken);

    private async Task EnsureAccessAsync(LessonQuestion question, int actingUserId, bool isAdmin, CancellationToken cancellationToken)
    {
        if (isAdmin || question.StudentUserId == actingUserId)
            return;

        if (!await _dbContext.IsTeachingStaffAsync(question.CourseId, actingUserId, cancellationToken))
            throw new QuestionAuthorizationException("You do not have access to this question.");
    }

    // The capacity someone writes in, not their global role: the student who asked stays a student
    // even if they also happen to be an instructor elsewhere on the platform.
    private static QuestionAuthorRole ResolveAuthorRole(LessonQuestion question, int actingUserId, bool isAdmin) =>
        question.StudentUserId == actingUserId ? QuestionAuthorRole.Student
        : isAdmin ? QuestionAuthorRole.Admin
        : QuestionAuthorRole.Instructor;

    private async Task MarkSeenAsync(LessonQuestion question, int userId, CancellationToken cancellationToken)
    {
        // Stamped at the later of now and the thread's last message: "seen" means "seen everything
        // currently in this thread". Stamping plain wall-clock time would leave a thread unread
        // whenever the last message landed a tick after the reader's clock.
        var seenAt = _timeProvider.GetUtcNow().UtcDateTime;
        if (question.LastMessageAt > seenAt)
            seenAt = question.LastMessageAt;

        var marker = await _dbContext.LessonQuestionReadMarkers
            .FirstOrDefaultAsync(m => m.LessonQuestionId == question.Id && m.UserId == userId, cancellationToken);

        if (marker is null)
            _dbContext.LessonQuestionReadMarkers.Add(LessonQuestionReadMarker.Create(question.Id, userId, seenAt));
        else
            marker.MarkSeen(seenAt);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Two tabs opening the same thread race to insert the first marker; the loser's row
            // already exists with an equivalent timestamp, so there's nothing left to do.
            _logger.LogWarning(ex, "LessonQuestionService::MarkSeenAsync: concurrent marker insert for question {QuestionId} user {UserId}",
                question.Id, userId);
        }
    }
}
