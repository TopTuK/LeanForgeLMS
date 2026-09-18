using LF.AppDomain.Entities.Qna;
using LF.AppDomain.Models.Qna.Enums;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Qna;
using LF.Application.Services.Qna;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Admin;

internal sealed class AdminLessonQuestionService(
    ILogger<AdminLessonQuestionService> logger,
    IAppDbContext dbContext,
    TimeProvider timeProvider) : IAdminLessonQuestionService
{
    private readonly ILogger<AdminLessonQuestionService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<PagedLessonQuestionsDto> ListAsync(
        int? courseId,
        LessonQuestionStatus? status,
        string? search,
        int adminUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AdminLessonQuestionService::ListAsync: called with CourseId={CourseId} Status={Status} AdminUserId={AdminUserId} Page={Page} PageSize={PageSize}",
            courseId, status, adminUserId, page, pageSize);

        var query = _dbContext.LessonQuestions.AsNoTracking();

        if (courseId is { } filterCourseId)
            query = query.Where(q => q.CourseId == filterCourseId);

        if (status is { } filterStatus)
            query = query.Where(q => q.Status == filterStatus);

        if (!string.IsNullOrWhiteSpace(search))
        {
            // ToLower rather than EF.Functions.ILike: it translates to a LOWER(...) LIKE on Postgres
            // and still evaluates correctly in the in-memory LINQ the unit tests run against.
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(q =>
                q.Title.ToLower().Contains(term)
                || _dbContext.Courses.Any(c => c.Id == q.CourseId && c.Title.ToLower().Contains(term))
                || _dbContext.Users.Any(u => u.Id == q.StudentUserId
                    && ((u.FirstName + " " + u.LastName).ToLower().Contains(term) || u.Email.ToLower().Contains(term))));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(q => q.LastMessageAt)
            .ThenByDescending(q => q.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToSummaries(_dbContext, adminUserId)
            .ToListAsync(cancellationToken);

        return new PagedLessonQuestionsDto { Items = items, TotalCount = totalCount };
    }

    public Task<LessonQuestionThreadDto?> GetThreadAsync(int questionId, int adminUserId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AdminLessonQuestionService::GetThreadAsync: called with QuestionId={QuestionId} AdminUserId={AdminUserId}", questionId, adminUserId);

        return ProjectThreadAsync(questionId, adminUserId, cancellationToken);
    }

    public async Task<LessonQuestionThreadDto?> PostMessageAsync(int questionId, PostQuestionMessageDto message, int adminUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        _logger.LogInformation("AdminLessonQuestionService::PostMessageAsync: called with QuestionId={QuestionId} AdminUserId={AdminUserId}", questionId, adminUserId);

        var question = await LoadForUpdateAsync(questionId, cancellationToken);
        if (question is null)
            return null;

        question.AddMessage(adminUserId, QuestionAuthorRole.Admin, message.Body, _timeProvider.GetUtcNow().UtcDateTime);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectThreadAsync(questionId, adminUserId, cancellationToken);
    }

    public async Task<LessonQuestionThreadDto?> DeleteMessageAsync(int questionId, int messageId, int adminUserId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AdminLessonQuestionService::DeleteMessageAsync: called with QuestionId={QuestionId} MessageId={MessageId} AdminUserId={AdminUserId}",
            questionId, messageId, adminUserId);

        var question = await LoadForUpdateAsync(questionId, cancellationToken);

        var message = question?.Messages.FirstOrDefault(m => m.Id == messageId);
        if (question is null || message is null)
            return null;

        message.SoftDelete(adminUserId, _timeProvider.GetUtcNow().UtcDateTime);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectThreadAsync(questionId, adminUserId, cancellationToken);
    }

    public async Task<bool> DeleteThreadAsync(int questionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AdminLessonQuestionService::DeleteThreadAsync: called with QuestionId={QuestionId}", questionId);

        var question = await _dbContext.LessonQuestions.FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken);
        if (question is null)
            return false;

        // Messages and read markers go with it via the cascades configured on their FKs.
        _dbContext.LessonQuestions.Remove(question);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private Task<LessonQuestion?> LoadForUpdateAsync(int questionId, CancellationToken cancellationToken) =>
        _dbContext.LessonQuestions
            .Include(q => q.Messages)
            .FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken);

    private Task<LessonQuestionThreadDto?> ProjectThreadAsync(int questionId, int viewerUserId, CancellationToken cancellationToken) =>
        _dbContext.LessonQuestions
            .AsNoTracking()
            .Where(q => q.Id == questionId)
            .ToThreads(_dbContext, viewerUserId)
            .FirstOrDefaultAsync(cancellationToken);
}
