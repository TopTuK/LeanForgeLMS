namespace LF.Application.Services.Notifications;

public interface IEnrollmentNotifier
{
    // Adds the "you're enrolled" email to the shared DbContext WITHOUT saving: the caller's
    // SaveChangesAsync commits it atomically with the enrollment, so a rolled-back enrollment
    // never sends mail and a committed one never loses it.
    Task StageEnrollmentConfirmationAsync(int userId, string courseTitle, CancellationToken cancellationToken = default);
}
