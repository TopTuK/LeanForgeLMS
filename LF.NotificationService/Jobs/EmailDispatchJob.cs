using LF.Application.Services.Email;
using Microsoft.Extensions.Options;
using Quartz;

namespace LF.NotificationService.Jobs;

// Single-replica design: DisallowConcurrentExecution stops a slow SMTP run from overlapping the next
// trigger, which would otherwise pick up (and double-send) the same Pending rows.
[DisallowConcurrentExecution]
internal sealed class EmailDispatchJob(
    IEmailDispatchService dispatchService,
    IOptions<EmailDispatchOptions> options) : IJob
{
    public static readonly JobKey Key = new(nameof(EmailDispatchJob), "email");

    private readonly IEmailDispatchService _dispatchService = dispatchService;
    private readonly EmailDispatchOptions _options = options.Value;

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken) =>
        await _dispatchService.DispatchDueAsync(_options.BatchSize, _options.MaxAttempts, cancellationToken);
}
