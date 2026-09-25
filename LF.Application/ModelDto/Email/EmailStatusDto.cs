using LF.AppDomain.Models.Email.Enums;

namespace LF.Application.ModelDto.Email;

public sealed class EmailStatusDto
{
    public int Id { get; init; }
    public string ToAddress { get; init; } = null!;
    public EmailMessageStatus Status { get; init; }
    public int Attempts { get; init; }
    public DateTime NextAttemptAt { get; init; }
    public string? LastError { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? SentAt { get; init; }
}
