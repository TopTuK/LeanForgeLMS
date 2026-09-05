namespace LF.AppDomain.Entities.Platform;

// Single-row table of runtime-tunable platform switches an admin can flip without a redeploy.
public sealed class PlatformSettings
{
    public const int SingletonId = 1;

    private PlatformSettings()
    {
    }

    public int Id { get; private set; }
    public bool StudentEnrollmentEnabled { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public int? UpdatedByUserId { get; private set; }

    // Ships disabled: students can browse/preview courses but cannot enroll until an admin turns it on.
    public static PlatformSettings CreateDefault(DateTime nowUtc) => new()
    {
        Id = SingletonId,
        StudentEnrollmentEnabled = false,
        UpdatedAt = nowUtc,
    };

    public void SetStudentEnrollmentEnabled(bool value, int updatedByUserId, DateTime nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(updatedByUserId, 0);

        StudentEnrollmentEnabled = value;
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = nowUtc;
    }
}
