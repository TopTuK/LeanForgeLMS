using FluentValidation;
using LF.AppDomain.Entities.Email;
using LF.Application.ModelDto.Email;

namespace LF.WebApi.Endpoints;

public sealed record SendTestEmailRequest(string To);

public sealed record EmailStatusResponse(
    int Id,
    string To,
    string Status,
    int Attempts,
    DateTime NextAttemptAt,
    string? LastError,
    DateTime CreatedAt,
    DateTime? SentAt)
{
    internal static EmailStatusResponse From(EmailStatusDto dto) => new(
        dto.Id,
        dto.ToAddress,
        dto.Status.ToString(),
        dto.Attempts,
        dto.NextAttemptAt,
        dto.LastError,
        dto.CreatedAt,
        dto.SentAt);
}

public sealed class SendTestEmailRequestValidator : AbstractValidator<SendTestEmailRequest>
{
    public SendTestEmailRequestValidator()
    {
        RuleFor(x => x.To).NotEmpty().EmailAddress().MaximumLength(EmailMessage.MaxAddressLength);
    }
}
