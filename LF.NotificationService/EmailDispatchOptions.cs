namespace LF.NotificationService;

internal sealed class EmailDispatchOptions
{
    public const string SectionName = "EmailDispatch";

    // Quartz cron (seconds field first). Default: every 30 seconds.
    public string Cron { get; set; } = "0/30 * * * * ?";

    public int BatchSize { get; set; } = 20;

    // Total delivery attempts before a message is marked Failed.
    public int MaxAttempts { get; set; } = 6;
}
