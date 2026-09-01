using LF.AppDomain.Models.Course.Enums;

namespace LF.Application.ModelDto.Enrollment;

public sealed class EnrollmentActivationDto
{
    public int EnrollmentId { get; init; }
    public int CourseId { get; init; }
    public EnrollmentStatus Status { get; init; }
    public decimal PricePaid { get; init; }
}
