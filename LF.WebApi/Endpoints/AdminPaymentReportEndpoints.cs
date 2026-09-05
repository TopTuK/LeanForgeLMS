using System.Globalization;
using System.Security.Claims;
using LF.Application.ModelDto.PaymentReporting;
using LF.Application.Services.PaymentReporting;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

public sealed class AdminPaymentReportEndpoints : IEndpointGroup
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 200;

    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/payments").WithTags("AdminPayments").RequireAuthorization("AdminOnly");

        group.MapGet("/", async Task<Results<Ok<PagedCoursePaymentsResponse>, UnauthorizedHttpResult, ValidationProblem>>
            (int? page, int? pageSize, string? from, string? to, ClaimsPrincipal user, IPaymentReportService reportService, CancellationToken ct) =>
        {
            if (user.GetUserId() is null) return TypedResults.Unauthorized();

            if (!TryParseRange(from, to, out var fromDate, out var toDate, out var problem))
                return TypedResults.ValidationProblem(problem);

            var effectivePage = page is > 0 ? page.Value : 1;
            var effectivePageSize = pageSize is > 0 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize;

            await reportService.ReconcileAsync(ct);
            var result = await reportService.ListAsync(effectivePage, effectivePageSize, fromDate, toDate, ct);

            return TypedResults.Ok(new PagedCoursePaymentsResponse(
                [.. result.Items.Select(ToRowResponse)],
                result.TotalCount,
                result.TotalAmount,
                effectivePage,
                effectivePageSize));
        });

        group.MapGet("/report.csv", async Task<Results<FileContentHttpResult, UnauthorizedHttpResult, ValidationProblem>>
            (string? from, string? to, ClaimsPrincipal user, IPaymentReportService reportService, CancellationToken ct) =>
        {
            if (user.GetUserId() is null) return TypedResults.Unauthorized();

            if (!TryParseRange(from, to, out var fromDate, out var toDate, out var problem))
                return TypedResults.ValidationProblem(problem);

            await reportService.ReconcileAsync(ct);
            var rows = await reportService.GetReportRowsAsync(fromDate, toDate, ct);

            var csv = CsvWriter.ToCsvBytes(
                ["Paid date", "Student name", "Student email", "Course", "Amount, RUB", "Promo code", "Provider", "Provider operation id"],
                rows.Select(ToCsvRow));

            var fileName = $"course-payments-{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}.csv";
            return TypedResults.File(csv, "text/csv; charset=utf-8", fileName);
        });
    }

    private static bool TryParseRange(string? from, string? to, out DateOnly? fromDate, out DateOnly? toDate, out Dictionary<string, string[]> problem)
    {
        fromDate = null;
        toDate = null;
        problem = [];

        if (!string.IsNullOrWhiteSpace(from))
        {
            if (DateOnly.TryParse(from, CultureInfo.InvariantCulture, out var parsedFrom)) fromDate = parsedFrom;
            else problem["from"] = ["Expected a date in yyyy-MM-dd format."];
        }

        if (!string.IsNullOrWhiteSpace(to))
        {
            if (DateOnly.TryParse(to, CultureInfo.InvariantCulture, out var parsedTo)) toDate = parsedTo;
            else problem["to"] = ["Expected a date in yyyy-MM-dd format."];
        }

        if (fromDate is { } f && toDate is { } t && f > t)
            problem["to"] = ["The end date must not be before the start date."];

        return problem.Count == 0;
    }

    private static CoursePaymentRowResponse ToRowResponse(CoursePaymentReportRowDto row) => new(
        row.Id,
        row.PaymentOrderId,
        row.PaidAt,
        row.StudentName,
        row.StudentEmail,
        row.CourseTitle,
        row.Amount,
        row.PromoCode,
        row.Provider,
        row.ProviderOperationId);

    private static IReadOnlyList<string> ToCsvRow(CoursePaymentReportRowDto row) =>
    [
        row.PaidAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        row.StudentName,
        row.StudentEmail,
        row.CourseTitle,
        row.Amount.ToString("0.00", CultureInfo.InvariantCulture),
        row.PromoCode ?? string.Empty,
        row.Provider,
        row.ProviderOperationId ?? string.Empty,
    ];
}
