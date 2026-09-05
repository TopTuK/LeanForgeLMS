namespace LF.Application.ModelDto.PaymentReporting;

public sealed class PagedCoursePaymentsDto
{
    public IReadOnlyList<CoursePaymentReportRowDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public decimal TotalAmount { get; init; }
}
