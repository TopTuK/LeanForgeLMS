using LF.AppDomain.Models.Email.Enums;

namespace LF.AppDomain.Entities.Email;

// Outbox row: written by any host with DB access, delivered by LF.NotificationService.
public sealed class EmailMessage
{
    public const int MaxAddressLength = 320;
    public const int MaxNameLength = 200;
    public const int MaxSubjectLength = 300;
    public const int MaxErrorLength = 2000;

    // Delay before retry N (1-based). Attempts beyond the table reuse its last entry.
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(4),
    ];

    private EmailMessage()
    {
    }

    public int Id { get; private set; }
    public string ToAddress { get; private set; } = null!;
    public string? ToName { get; private set; }
    public string Subject { get; private set; } = null!;
    public string HtmlBody { get; private set; } = null!;
    public string? TextBody { get; private set; }
    public EmailMessageStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTime NextAttemptAt { get; private set; }
    public string? LastError { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }

    public static EmailMessage Create(
        string toAddress,
        string? toName,
        string subject,
        string htmlBody,
        string? textBody,
        DateTime createdAt,
        DateTime? notBefore = null)
    {
        if (string.IsNullOrWhiteSpace(toAddress))
            throw new ArgumentException("Recipient address cannot be empty.", nameof(toAddress));

        var address = toAddress.Trim();
        if (address.Length > MaxAddressLength || !IsPlausibleAddress(address))
            throw new ArgumentException($"'{address}' is not a valid email address.", nameof(toAddress));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Email subject cannot be empty.", nameof(subject));

        var trimmedSubject = subject.Trim();
        if (trimmedSubject.Length > MaxSubjectLength)
            throw new ArgumentException($"Email subject cannot exceed {MaxSubjectLength} characters.", nameof(subject));

        if (string.IsNullOrWhiteSpace(htmlBody))
            throw new ArgumentException("Email body cannot be empty.", nameof(htmlBody));

        var name = string.IsNullOrWhiteSpace(toName) ? null : toName.Trim();
        if (name is { Length: > MaxNameLength })
            name = name[..MaxNameLength];

        return new EmailMessage
        {
            ToAddress = address,
            ToName = name,
            Subject = trimmedSubject,
            HtmlBody = htmlBody,
            TextBody = string.IsNullOrWhiteSpace(textBody) ? null : textBody,
            Status = EmailMessageStatus.Pending,
            CreatedAt = createdAt,
            NextAttemptAt = notBefore is { } scheduled && scheduled > createdAt ? scheduled : createdAt,
        };
    }

    public bool IsDue(DateTime nowUtc) => Status == EmailMessageStatus.Pending && NextAttemptAt <= nowUtc;

    public void MarkSent(DateTime nowUtc)
    {
        if (Status != EmailMessageStatus.Pending)
            throw new InvalidOperationException($"Cannot mark an email in status {Status} as sent.");

        Attempts++;
        Status = EmailMessageStatus.Sent;
        SentAt = nowUtc;
        LastError = null;
    }

    public void RecordFailure(string error, DateTime nowUtc, int maxAttempts, bool isTransient)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        if (Status != EmailMessageStatus.Pending)
            throw new InvalidOperationException($"Cannot record a delivery failure for an email in status {Status}.");

        Attempts++;
        LastError = string.IsNullOrWhiteSpace(error)
            ? "Unknown delivery error."
            : error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;

        if (!isTransient || Attempts >= maxAttempts)
        {
            Status = EmailMessageStatus.Failed;
            return;
        }

        NextAttemptAt = nowUtc + RetryDelays[Math.Min(Attempts, RetryDelays.Length) - 1];
    }

    // Deliberately loose: the SMTP server is the real authority. This only rejects obvious garbage
    // so it never reaches the outbox.
    private static bool IsPlausibleAddress(string address)
    {
        var at = address.IndexOf('@');
        return at > 0
            && at == address.LastIndexOf('@')
            && at < address.Length - 1
            && !address.Any(char.IsWhiteSpace);
    }
}
