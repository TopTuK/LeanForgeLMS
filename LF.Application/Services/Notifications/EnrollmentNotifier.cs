using LF.AppDomain.Entities.Email;
using LF.Application.Common.Email;
using LF.Application.Common.Interfaces;
using LF.Application.Common.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LF.Application.Services.Notifications;

internal sealed class EnrollmentNotifier(
    ILogger<EnrollmentNotifier> logger,
    IAppDbContext dbContext,
    IEmailTemplateRenderer templateRenderer,
    IOptions<AppUrlOptions> appUrlOptions,
    TimeProvider timeProvider) : IEnrollmentNotifier
{
    // SPA route listing the student's in-progress courses (lf.webapp router: CoursesActive).
    private const string MyCoursesPath = "/courses/active";

    private readonly ILogger<EnrollmentNotifier> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly IEmailTemplateRenderer _templateRenderer = templateRenderer;
    private readonly AppUrlOptions _appUrls = appUrlOptions.Value;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task StageEnrollmentConfirmationAsync(int userId, string courseTitle, CancellationToken cancellationToken = default)
    {
        var recipient = await _dbContext.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Email, u.FirstName, u.PreferredLanguage })
            .FirstOrDefaultAsync(cancellationToken);

        if (recipient is null || string.IsNullOrWhiteSpace(recipient.Email))
        {
            _logger.LogWarning("EnrollmentNotifier::StageEnrollmentConfirmationAsync: no email address for UserId={UserId}, skipping confirmation", userId);
            return;
        }

        var rendered = _templateRenderer.Render(EmailTemplates.EnrollmentConfirmation, recipient.PreferredLanguage, new Dictionary<string, string>
        {
            ["Name"] = string.IsNullOrWhiteSpace(recipient.FirstName) ? recipient.Email : recipient.FirstName,
            ["CourseTitle"] = courseTitle,
            ["MyCoursesUrl"] = _appUrls.Link(MyCoursesPath),
        });

        EmailMessage message;
        try
        {
            message = EmailMessage.Create(
                recipient.Email,
                recipient.FirstName,
                rendered.Subject,
                rendered.HtmlBody,
                rendered.TextBody,
                _timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (ArgumentException ex)
        {
            // A malformed address from the identity provider must never block the enrollment itself.
            _logger.LogWarning(ex, "EnrollmentNotifier::StageEnrollmentConfirmationAsync: cannot email UserId={UserId}, skipping confirmation", userId);
            return;
        }

        _dbContext.EmailMessages.Add(message);

        _logger.LogInformation("EnrollmentNotifier::StageEnrollmentConfirmationAsync: staged confirmation for UserId={UserId} Language={Language}",
            userId, recipient.PreferredLanguage ?? "(default)");
    }
}
