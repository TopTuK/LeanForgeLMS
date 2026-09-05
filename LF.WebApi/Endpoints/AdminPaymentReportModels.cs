namespace LF.WebApi.Endpoints;

public sealed record CoursePaymentRowResponse(
    int Id,
    int PaymentOrderId,
    DateTime PaidAt,
    string StudentName,
    string StudentEmail,
    string CourseTitle,
    decimal Amount,
    string? PromoCode,
    string Provider,
    string? ProviderOperationId);

public sealed record PagedCoursePaymentsResponse(
    IReadOnlyList<CoursePaymentRowResponse> Items,
    int TotalCount,
    decimal TotalAmount,
    int Page,
    int PageSize);
