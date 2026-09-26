using System.Net;
using System.Text;
using LF.AppDomain.Models.Course.Enums;
using LF.AppDomain.Models.User;
using LF.Application.Common.Email;
using LF.Application.Common.Interfaces;
using LF.Application.Common.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CourseContentChange = LF.AppDomain.Entities.Course.CourseContentChange;
using EmailMessage = LF.AppDomain.Entities.Email.EmailMessage;

namespace LF.Application.Services.Notifications;

internal sealed class CourseUpdateDigestService(
    ILogger<CourseUpdateDigestService> logger,
    IAppDbContext dbContext,
    IEmailTemplateRenderer templateRenderer,
    IOptions<AppUrlOptions> appUrlOptions,
    TimeProvider timeProvider) : ICourseUpdateDigestService
{
    // Section headings of the lesson list. Everything else a student reads lives in the templates;
    // these are here only because the list is built in code (templates have no loops).
    private static readonly Dictionary<string, (string Added, string Updated)> Headings = new()
    {
        [UserLanguage.Russian] = ("Новые уроки", "Обновлённые уроки"),
        [UserLanguage.English] = ("New lessons", "Updated lessons"),
    };

    private readonly ILogger<CourseUpdateDigestService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly IEmailTemplateRenderer _templateRenderer = templateRenderer;
    private readonly AppUrlOptions _appUrls = appUrlOptions.Value;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<CourseUpdateDigestResult> SendDueDigestsAsync(TimeSpan quietPeriod, int maxCourses, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quietPeriod, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCourses, 1);

        var cutoff = _timeProvider.GetUtcNow().UtcDateTime - quietPeriod;

        // A course is due once its most recent pending change is older than the quiet period, so an
        // author still mid-session keeps pushing the digest back instead of triggering several.
        var dueCourseIds = await _dbContext.CourseContentChanges
            .Where(c => c.NotifiedAt == null)
            .GroupBy(c => c.CourseId)
            .Where(g => g.Max(c => c.LastChangedAt) <= cutoff)
            .Select(g => g.Key)
            .OrderBy(id => id)
            .Take(maxCourses)
            .ToListAsync(cancellationToken);

        var emailsQueued = 0;
        foreach (var courseId in dueCourseIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            emailsQueued += await ProcessCourseAsync(courseId, cancellationToken);
        }

        if (dueCourseIds.Count > 0)
        {
            _logger.LogInformation("CourseUpdateDigestService::SendDueDigestsAsync: processed {Courses} course(s), queued {Emails} email(s)",
                dueCourseIds.Count, emailsQueued);
        }

        return new CourseUpdateDigestResult(dueCourseIds.Count, emailsQueued);
    }

    // One SaveChanges per course: the queued emails and the "notified" marks commit together, so a
    // crash never re-sends a digest nor loses one.
    private async Task<int> ProcessCourseAsync(int courseId, CancellationToken cancellationToken)
    {
        var changes = await _dbContext.CourseContentChanges
            .Include(c => c.Lesson)
            .Where(c => c.CourseId == courseId && c.NotifiedAt == null)
            .ToListAsync(cancellationToken);

        var course = await _dbContext.Courses.AsNoTracking()
            .Include(c => c.Chapters)
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var emailsQueued = 0;

        if (course is null || !course.IsPublished || changes.Count == 0)
        {
            // Students of an unpublished course cannot open it, so the pending changes are dropped
            // rather than announced whenever the course comes back.
            _logger.LogInformation("CourseUpdateDigestService::ProcessCourseAsync: CourseId={CourseId} not published, discarding {Count} change(s)",
                courseId, changes.Count);
        }
        else
        {
            var chapterOrder = course.Chapters.ToDictionary(ch => ch.Id, ch => ch.SortOrder);
            var ordered = changes
                .OrderBy(c => chapterOrder.GetValueOrDefault(c.Lesson.ChapterId, int.MaxValue))
                .ThenBy(c => c.Lesson.SortOrder)
                .ToList();

            emailsQueued = await QueueDigestEmailsAsync(courseId, course.Title, ordered, now, cancellationToken);
        }

        foreach (var change in changes)
            change.MarkNotified(now);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return emailsQueued;
    }

    private async Task<int> QueueDigestEmailsAsync(
        int courseId, string courseTitle, IReadOnlyList<CourseContentChange> changes, DateTime now, CancellationToken cancellationToken)
    {
        // Someone who enrolled after the last change already saw all of it.
        var latestChange = changes.Max(c => c.LastChangedAt);

        var enrollments = await _dbContext.Enrollments.AsNoTracking()
            .Where(e => e.CourseId == courseId && e.Status == EnrollmentStatus.Active && e.EnrolledAt < latestChange)
            .Select(e => new { e.Id, e.UserId })
            .ToListAsync(cancellationToken);

        if (enrollments.Count == 0)
            return 0;

        var userIds = enrollments.Select(e => e.UserId).Distinct().ToList();
        var users = await _dbContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email, u.FirstName, u.PreferredLanguage })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var listsByLanguage = new Dictionary<string, (string Html, string Text)>();
        var queued = 0;

        foreach (var enrollment in enrollments)
        {
            if (!users.TryGetValue(enrollment.UserId, out var user) || string.IsNullOrWhiteSpace(user.Email))
                continue;

            var language = UserLanguage.OrDefault(user.PreferredLanguage);
            if (!listsByLanguage.TryGetValue(language, out var lists))
                listsByLanguage[language] = lists = BuildChangeLists(changes, language);

            var rendered = _templateRenderer.Render(EmailTemplates.CourseUpdated, language, new Dictionary<string, string>
            {
                ["Name"] = string.IsNullOrWhiteSpace(user.FirstName) ? user.Email : user.FirstName,
                ["CourseTitle"] = courseTitle,
                ["CourseUrl"] = _appUrls.Link($"/courses/learn/{enrollment.Id}"),
                ["ChangesHtml"] = lists.Html,
                ["ChangesText"] = lists.Text,
            });

            try
            {
                _dbContext.EmailMessages.Add(EmailMessage.Create(
                    user.Email, user.FirstName, rendered.Subject, rendered.HtmlBody, rendered.TextBody, now));
                queued++;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "CourseUpdateDigestService::QueueDigestEmailsAsync: cannot email UserId={UserId}, skipping", user.Id);
            }
        }

        return queued;
    }

    private static (string Html, string Text) BuildChangeLists(IReadOnlyList<CourseContentChange> changes, string language)
    {
        var headings = Headings.GetValueOrDefault(language, Headings[UserLanguage.Default]);
        var html = new StringBuilder();
        var text = new StringBuilder();

        AppendSection(CourseContentChangeKind.LessonAdded, headings.Added);
        AppendSection(CourseContentChangeKind.LessonUpdated, headings.Updated);

        return (html.ToString(), text.ToString().TrimEnd());

        void AppendSection(CourseContentChangeKind kind, string heading)
        {
            var titles = changes.Where(c => c.Kind == kind).Select(c => c.Lesson.Title).ToList();
            if (titles.Count == 0)
                return;

            html.Append("<p style=\"margin:16px 0 4px 0;font-size:15px;font-weight:bold;\">")
                .Append(WebUtility.HtmlEncode(heading))
                .Append("</p><ul style=\"margin:0 0 12px 0;padding-left:20px;font-size:15px;line-height:1.6;\">");
            foreach (var title in titles)
                html.Append("<li>").Append(WebUtility.HtmlEncode(title)).Append("</li>");
            html.Append("</ul>");

            text.Append(heading).Append(':').AppendLine();
            foreach (var title in titles)
                text.Append("- ").Append(title).AppendLine();
            text.AppendLine();
        }
    }
}
