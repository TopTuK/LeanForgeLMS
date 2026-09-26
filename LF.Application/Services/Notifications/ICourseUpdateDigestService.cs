namespace LF.Application.Services.Notifications;

// Driven by LF.NotificationService's scheduled job. Turns pending course content changes into one
// digest email per enrolled student, once a course has been quiet for the given period.
public interface ICourseUpdateDigestService
{
    Task<CourseUpdateDigestResult> SendDueDigestsAsync(TimeSpan quietPeriod, int maxCourses, CancellationToken cancellationToken = default);
}

public sealed record CourseUpdateDigestResult(int CoursesProcessed, int EmailsQueued);
