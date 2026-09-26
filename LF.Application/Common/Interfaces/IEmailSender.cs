using LF.Application.ModelDto.Email;

namespace LF.Application.Common.Interfaces;

// SMTP transport abstraction. The MailKit implementation lives in LF.Infrastructure.
// Implementations must translate every transport failure into EmailDeliveryException.
public interface IEmailSender
{
    Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default);
}
