namespace LF.Application.ModelDto.PaymentReporting;

public sealed class CoursePaymentReportRowDto
{
    public int Id { get; init; }
    public int PaymentOrderId { get; init; }
    public int EnrollmentId { get; init; }
    public int UserId { get; init; }
    public int CourseId { get; init; }
    public string StudentEmail { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? PromoCode { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string? ProviderOperationId { get; init; }
    public DateTime PaidAt { get; init; }
}
