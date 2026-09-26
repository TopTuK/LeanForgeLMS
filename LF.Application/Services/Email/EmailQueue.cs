using LF.AppDomain.Entities.Email;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Email;

internal sealed class EmailQueue(
    ILogger<EmailQueue> logger,
    IAppDbContext dbContext,
    TimeProvider timeProvider) : IEmailQueue
{
    private readonly ILogger<EmailQueue> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<EmailStatusDto> EnqueueAsync(EnqueueEmailDto dto, CancellationToken cancellationToken = default)
    {
        var message = EmailMessage.Create(
            dto.ToAddress,
            dto.ToName,
            dto.Subject,
            dto.HtmlBody,
            dto.TextBody,
            _timeProvider.GetUtcNow().UtcDateTime,
            dto.NotBefore);

        _dbContext.EmailMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("EmailQueue::EnqueueAsync: queued EmailId={EmailId} Subject={Subject} NextAttemptAt={NextAttemptAt}",
            message.Id, message.Subject, message.NextAttemptAt);

        return ToDto(message);
    }

    public async Task<EmailStatusDto?> GetStatusAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.EmailMessages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        return message is null ? null : ToDto(message);
    }

    private static EmailStatusDto ToDto(EmailMessage message) => new()
    {
        Id = message.Id,
        ToAddress = message.ToAddress,
        Status = message.Status,
        Attempts = message.Attempts,
        NextAttemptAt = message.NextAttemptAt,
        LastError = message.LastError,
        CreatedAt = message.CreatedAt,
        SentAt = message.SentAt,
    };
}
