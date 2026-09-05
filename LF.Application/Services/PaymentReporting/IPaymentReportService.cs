using LF.Application.ModelDto.PaymentReporting;

namespace LF.Application.Services.PaymentReporting;

public interface IPaymentReportService
{
    // Writes the denormalized ledger row for a settled payment order. Idempotent.
    Task RecordCoursePaymentAsync(int paymentOrderId, CancellationToken cancellationToken = default);

    // Backfills ledger rows for any settled payment order that has none. Returns the number inserted.
    Task<int> ReconcileAsync(CancellationToken cancellationToken = default);

    Task<PagedCoursePaymentsDto> ListAsync(int page, int pageSize, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CoursePaymentReportRowDto>> GetReportRowsAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
}
