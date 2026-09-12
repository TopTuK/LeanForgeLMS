namespace LF.Application.ModelDto.Enrollment;

public sealed class RemoveEnrollmentResultDto
{
    public int UserId { get; init; }

    // True when the student actually paid, so the caller can surface that in the confirmation.
    // No refund is issued — the CoursePayment ledger row survives for reporting.
    public bool WasPaid { get; init; }
    public decimal PricePaid { get; init; }
}
