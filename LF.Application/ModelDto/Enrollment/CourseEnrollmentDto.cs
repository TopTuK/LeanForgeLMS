using LF.AppDomain.Models.Course.Enums;

namespace LF.Application.ModelDto.Enrollment;

// One student's enrollment as seen from the course side. StudentEmail/StudentFirstName/
// StudentLastName are hydrated by the caller from LF.IdentityService — LF.CourseService
// only ever knows the scalar UserId.
public sealed class CourseEnrollmentDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string? StudentEmail { get; set; }
    public string? StudentFirstName { get; set; }
    public string? StudentLastName { get; set; }
    public EnrollmentStatus Status { get; init; }
    public decimal PricePaid { get; init; }
    public DateTime EnrolledAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int TotalLessonCount { get; init; }
    public int CompletedLessonCount { get; init; }
    public int ProgressPercent { get; init; }

    // A price on a PendingPayment row is only an intent to pay, so it does not count as paid.
    public bool IsPaid => Status == EnrollmentStatus.Active && PricePaid > 0m;
}
