using LF.AppDomain.Entities.Email;
using LF.AppDomain.Models.Email.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Email;

internal sealed class EmailDispatchService(
    ILogger<EmailDispatchService> logger,
    IAppDbContext dbContext,
    IEmailSender emailSender,
    TimeProvider timeProvider) : IEmailDispatchService
{
    private readonly ILogger<EmailDispatchService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly IEmailSender _emailSender = emailSender;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<EmailDispatchResult> DispatchDueAsync(int batchSize, int maxAttempts, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var due = await _dbContext.EmailMessages
            .Where(m => m.Status == EmailMessageStatus.Pending && m.NextAttemptAt <= now)
            .OrderBy(m => m.NextAttemptAt)
            .ThenBy(m => m.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        int sent = 0, retrying = 0, failed = 0;

        foreach (var message in due)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await DeliverAsync(message, maxAttempts, cancellationToken);

            switch (message.Status)
            {
                case EmailMessageStatus.Sent: sent++; break;
                case EmailMessageStatus.Failed: failed++; break;
                default: retrying++; break;
            }

            // Saved per message so one bad row never rolls back the others. Once the SMTP server has
            // accepted the message the outcome is persisted even during shutdown, or it would be re-sent.
            await _dbContext.SaveChangesAsync(CancellationToken.None);
        }

        var result = new EmailDispatchResult(sent, retrying, failed);
        if (result.Total > 0)
        {
            _logger.LogInformation("EmailDispatchService::DispatchDueAsync: processed {Total} email(s): Sent={Sent} Retrying={Retrying} Failed={Failed}",
                result.Total, sent, retrying, failed);
        }

        return result;
    }

    private async Task DeliverAsync(EmailMessage message, int maxAttempts, CancellationToken cancellationToken)
    {
        try
        {
            await _emailSender.SendAsync(
                new OutgoingEmail(message.ToAddress, message.ToName, message.Subject, message.HtmlBody, message.TextBody),
                cancellationToken);

            message.MarkSent(_timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (EmailDeliveryException ex)
        {
            message.RecordFailure(ex.Message, _timeProvider.GetUtcNow().UtcDateTime, maxAttempts, ex.IsTransient);

            if (message.Status == EmailMessageStatus.Failed)
            {
                _logger.LogError(ex, "EmailDispatchService::DeliverAsync: EmailId={EmailId} permanently failed after {Attempts} attempt(s)",
                    message.Id, message.Attempts);
            }
            else
            {
                _logger.LogWarning(ex, "EmailDispatchService::DeliverAsync: EmailId={EmailId} attempt {Attempts} failed, retrying at {NextAttemptAt}",
                    message.Id, message.Attempts, message.NextAttemptAt);
            }
        }
    }
}
