namespace LF.NotificationService;

internal sealed class CourseUpdateDigestOptions
{
    public const string SectionName = "CourseUpdateDigest";

    // Quartz cron (seconds field first). Default: every 5 minutes.
    public string Cron { get; set; } = "0 0/5 * * * ?";

    // A course's digest goes out only after its lessons have been untouched this long, so one
    // editing session produces one email instead of one per save.
    public int QuietPeriodMinutes { get; set; } = 30;

    public int MaxCoursesPerRun { get; set; } = 20;
}
