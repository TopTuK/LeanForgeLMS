using LF.Application.Services.Notifications;
using Microsoft.Extensions.Options;
using Quartz;

namespace LF.NotificationService.Jobs;

// Only queues outbox rows; EmailDispatchJob delivers them. DisallowConcurrentExecution stops an
// overlapping run from digesting the same pending changes twice.
[DisallowConcurrentExecution]
internal sealed class CourseUpdateDigestJob(
    ICourseUpdateDigestService digestService,
    IOptions<CourseUpdateDigestOptions> options) : IJob
{
    public static readonly JobKey Key = new(nameof(CourseUpdateDigestJob), "email");

    private readonly ICourseUpdateDigestService _digestService = digestService;
    private readonly CourseUpdateDigestOptions _options = options.Value;

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken) =>
        await _digestService.SendDueDigestsAsync(
            TimeSpan.FromMinutes(_options.QuietPeriodMinutes),
            _options.MaxCoursesPerRun,
            cancellationToken);
}
