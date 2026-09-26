namespace LF.Application.ModelDto.Email;

public sealed record OutgoingEmail(
    string ToAddress,
    string? ToName,
    string Subject,
    string HtmlBody,
    string? TextBody);
