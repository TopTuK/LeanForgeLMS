using LF.Application;
using LF.Application.Common.Options;
using LF.Infrastructure;
using LF.NotificationService;
using LF.NotificationService.Jobs;
using Quartz;
using Sentry;
using Serilog;

Microsoft.Extensions.Hosting.Extensions.CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();

    builder.Services.AddInfrastructureDatabase(builder.Configuration);
    builder.Services.AddNotificationApplication();
    builder.Services.AddInfrastructureEmail(builder.Configuration, out var smtpConfigured);

    var dispatchSection = builder.Configuration.GetSection(EmailDispatchOptions.SectionName);
    builder.Services.Configure<EmailDispatchOptions>(dispatchSection);
    var dispatchOptions = dispatchSection.Get<EmailDispatchOptions>() ?? new EmailDispatchOptions();

    var digestSection = builder.Configuration.GetSection(CourseUpdateDigestOptions.SectionName);
    builder.Services.Configure<CourseUpdateDigestOptions>(digestSection);
    var digestOptions = digestSection.Get<CourseUpdateDigestOptions>() ?? new CourseUpdateDigestOptions();

    // Links in digest emails. Fails at startup rather than queueing emails with broken links.
    builder.Services.AddOptions<AppUrlOptions>()
        .BindConfiguration(AppUrlOptions.SectionName)
        .Validate(options => options.IsValid, "App:PublicBaseUrl must be an absolute http(s) URL of the SPA.")
        .ValidateOnStart();

    builder.Services.AddQuartz(quartz =>
    {
        // Only writes outbox rows, so it runs even without SMTP; delivery waits for EmailDispatchJob.
        quartz.ScheduleJob<CourseUpdateDigestJob>(
            trigger => trigger
                .WithIdentity($"{CourseUpdateDigestJob.Key.Name}-trigger", CourseUpdateDigestJob.Key.Group)
                .WithCronSchedule(digestOptions.Cron, cron => cron.WithMisfireInstruction(CronTriggerMisfireInstruction.DoNothing)),
            job => job.WithIdentity(CourseUpdateDigestJob.Key));

        // Without SMTP settings every attempt would fail and burn the retry budget, so queued mail
        // is left Pending until the service is configured and restarted.
        if (!smtpConfigured)
            return;

        quartz.ScheduleJob<EmailDispatchJob>(
            trigger => trigger
                .WithIdentity($"{EmailDispatchJob.Key.Name}-trigger", EmailDispatchJob.Key.Group)
                .WithCronSchedule(dispatchOptions.Cron, cron => cron.WithMisfireInstruction(CronTriggerMisfireInstruction.DoNothing)),
            job => job.WithIdentity(EmailDispatchJob.Key));
    });
    builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

    var app = builder.Build();
    app.UseDefaultRequestLogging();

    if (smtpConfigured)
    {
        Log.Information("Email dispatch scheduled with cron {Cron}, batch size {BatchSize}, max attempts {MaxAttempts}",
            dispatchOptions.Cron, dispatchOptions.BatchSize, dispatchOptions.MaxAttempts);
    }
    else
    {
        Log.Warning("SMTP is not configured (Smtp:Host / Smtp:FromAddress) — email dispatch is disabled and queued emails stay Pending");
    }

    app.MapDefaultEndpoints();
    app.MapGet("/", () => "LF.NotificationService delivers queued emails in the background. It has no public API.");

    app.Run();
}
catch (Exception ex)
{
    SentrySdk.CaptureException(ex);
    Log.Fatal(ex, "LF.NotificationService application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
