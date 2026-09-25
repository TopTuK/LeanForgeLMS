namespace LF.Application.Services.Email;

// Consumer side of the email outbox, driven by LF.NotificationService's scheduled job.
public interface IEmailDispatchService
{
    Task<EmailDispatchResult> DispatchDueAsync(int batchSize, int maxAttempts, CancellationToken cancellationToken = default);
}

public sealed record EmailDispatchResult(int Sent, int Retrying, int Failed)
{
    public int Total => Sent + Retrying + Failed;
}
