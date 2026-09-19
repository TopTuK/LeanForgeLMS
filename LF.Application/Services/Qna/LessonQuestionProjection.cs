using LF.AppDomain.Entities.Qna;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Qna;

namespace LF.Application.Services.Qna;

// LessonQuestion deliberately has no navigations to Course, Lesson or DbUser (users are a separate
// bounded context; the course graph is owned by LF.CourseService), so the display fields come from
// correlated sub-queries instead. EF translates these into LEFT JOINs in one round-trip.
//
// HasUnread is spelled out inline in both projections rather than factored into a helper: a method
// call inside an expression tree has no EF translation, so extracting it would silently move the
// unread check to client evaluation.
internal static class LessonQuestionProjection
{
    // Long enough for two lines in an inbox row; the full body is only sent with the thread.
    private const int PreviewLength = 160;

    public static IQueryable<LessonQuestionSummaryDto> ToSummaries(
        this IQueryable<LessonQuestion> query,
        IAppDbContext dbContext,
        int viewerUserId) =>
        query.Select(q => new LessonQuestionSummaryDto
        {
            Id = q.Id,
            CourseId = q.CourseId,
            CourseTitle = dbContext.Courses.Where(c => c.Id == q.CourseId).Select(c => c.Title).FirstOrDefault()!,
            LessonId = q.LessonId,
            LessonTitle = dbContext.Courses
                .Where(c => c.Id == q.CourseId)
                .SelectMany(c => c.Chapters)
                .SelectMany(ch => ch.Lessons)
                .Where(l => l.Id == q.LessonId)
                .Select(l => l.Title)
                .FirstOrDefault()!,
            StudentUserId = q.StudentUserId,
            StudentName = dbContext.Users
                .Where(u => u.Id == q.StudentUserId)
                .Select(u => u.FirstName + " " + u.LastName)
                .FirstOrDefault()!,
            Title = q.Title,
            Status = q.Status,
            CreatedAt = q.CreatedAt,
            LastMessageAt = q.LastMessageAt,
            MessageCount = q.Messages.Count,
            HasUnread = q.LastMessageAuthorUserId != viewerUserId
                && !dbContext.LessonQuestionReadMarkers.Any(m =>
                    m.LessonQuestionId == q.Id && m.UserId == viewerUserId && m.LastSeenAt >= q.LastMessageAt),
            LastMessagePreview = q.Messages
                .OrderByDescending(m => m.CreatedAt)
                .ThenByDescending(m => m.Id)
                .Select(m => m.IsDeleted
                    ? null
                    : m.Body.Length > PreviewLength ? m.Body.Substring(0, PreviewLength) : m.Body)
                .FirstOrDefault(),
            LastMessageAuthorRole = q.Messages
                .OrderByDescending(m => m.CreatedAt)
                .ThenByDescending(m => m.Id)
                .Select(m => m.AuthorRole)
                .FirstOrDefault(),
            StudentEnrollmentId = dbContext.Enrollments
                .Where(e => e.CourseId == q.CourseId && e.UserId == q.StudentUserId)
                .Select(e => (int?)e.Id)
                .FirstOrDefault(),
            AskedByViewer = q.StudentUserId == viewerUserId,
        });

    public static IQueryable<LessonQuestionThreadDto> ToThreads(
        this IQueryable<LessonQuestion> query,
        IAppDbContext dbContext,
        int viewerUserId) =>
        query.Select(q => new LessonQuestionThreadDto
        {
            Id = q.Id,
            CourseId = q.CourseId,
            CourseTitle = dbContext.Courses.Where(c => c.Id == q.CourseId).Select(c => c.Title).FirstOrDefault()!,
            LessonId = q.LessonId,
            LessonTitle = dbContext.Courses
                .Where(c => c.Id == q.CourseId)
                .SelectMany(c => c.Chapters)
                .SelectMany(ch => ch.Lessons)
                .Where(l => l.Id == q.LessonId)
                .Select(l => l.Title)
                .FirstOrDefault()!,
            StudentUserId = q.StudentUserId,
            StudentName = dbContext.Users
                .Where(u => u.Id == q.StudentUserId)
                .Select(u => u.FirstName + " " + u.LastName)
                .FirstOrDefault()!,
            Title = q.Title,
            Status = q.Status,
            CreatedAt = q.CreatedAt,
            LastMessageAt = q.LastMessageAt,
            MessageCount = q.Messages.Count,
            HasUnread = q.LastMessageAuthorUserId != viewerUserId
                && !dbContext.LessonQuestionReadMarkers.Any(m =>
                    m.LessonQuestionId == q.Id && m.UserId == viewerUserId && m.LastSeenAt >= q.LastMessageAt),
            LastMessagePreview = q.Messages
                .OrderByDescending(m => m.CreatedAt)
                .ThenByDescending(m => m.Id)
                .Select(m => m.IsDeleted
                    ? null
                    : m.Body.Length > PreviewLength ? m.Body.Substring(0, PreviewLength) : m.Body)
                .FirstOrDefault(),
            LastMessageAuthorRole = q.Messages
                .OrderByDescending(m => m.CreatedAt)
                .ThenByDescending(m => m.Id)
                .Select(m => m.AuthorRole)
                .FirstOrDefault(),
            StudentEnrollmentId = dbContext.Enrollments
                .Where(e => e.CourseId == q.CourseId && e.UserId == q.StudentUserId)
                .Select(e => (int?)e.Id)
                .FirstOrDefault(),
            AskedByViewer = q.StudentUserId == viewerUserId,
            Messages = q.Messages
                .OrderBy(m => m.CreatedAt)
                .ThenBy(m => m.Id)
                .Select(m => new LessonQuestionMessageDto
                {
                    Id = m.Id,
                    AuthorUserId = m.AuthorUserId,
                    AuthorName = dbContext.Users
                        .Where(u => u.Id == m.AuthorUserId)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefault()!,
                    AuthorRole = m.AuthorRole,

                    // Withheld rather than removed, so an admin deletion doesn't renumber a thread.
                    Body = m.IsDeleted ? null : m.Body,
                    CreatedAt = m.CreatedAt,
                    IsDeleted = m.IsDeleted,
                    IsMine = m.AuthorUserId == viewerUserId,
                })
                .ToList(),
        });
}
