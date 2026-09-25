using LF.Application.ModelDto.Email;

namespace LF.Application.Services.Email;

// Producer side of the email outbox. Callers only write a row. LF.NotificationService delivers it.
public interface IEmailQueue
{
    Task<EmailStatusDto> EnqueueAsync(EnqueueEmailDto dto, CancellationToken cancellationToken = default);
    Task<EmailStatusDto?> GetStatusAsync(int id, CancellationToken cancellationToken = default);
}
