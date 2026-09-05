using LF.AppDomain.Entities.Payment;
using LF.AppDomain.Models.Payment.Enums;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.PaymentReporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.PaymentReporting;

internal sealed class PaymentReportService(
    ILogger<PaymentReportService> logger,
    IAppDbContext dbContext,
    TimeProvider timeProvider) : IPaymentReportService
{
    private readonly ILogger<PaymentReportService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task RecordCoursePaymentAsync(int paymentOrderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PaymentReportService::RecordCoursePaymentAsync: called with PaymentOrderId={PaymentOrderId}", paymentOrderId);

        var alreadyRecorded = await _dbContext.CoursePayments
            .AsNoTracking()
            .AnyAsync(p => p.PaymentOrderId == paymentOrderId, cancellationToken);
        if (alreadyRecorded)
        {
            return;
        }

        var order = await _dbContext.PaymentOrders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == paymentOrderId, cancellationToken);
        if (order is null)
        {
            _logger.LogWarning("PaymentReportService::RecordCoursePaymentAsync: payment order {PaymentOrderId} not found", paymentOrderId);
            return;
        }

        var payment = await BuildRecordAsync(order, cancellationToken);
        if (payment is null)
        {
            return;
        }

        _dbContext.CoursePayments.Add(payment);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // A concurrent webhook retry won the race on the unique PaymentOrderId index — the row exists.
            _logger.LogWarning(ex, "PaymentReportService::RecordCoursePaymentAsync: concurrent insert for order {PaymentOrderId}", paymentOrderId);
        }
    }

    public async Task<int> ReconcileAsync(CancellationToken cancellationToken = default)
    {
        var recordedOrderIds = await _dbContext.CoursePayments
            .AsNoTracking()
            .Select(p => p.PaymentOrderId)
            .ToListAsync(cancellationToken);

        var missingOrders = await _dbContext.PaymentOrders
            .AsNoTracking()
            .Where(o => o.Status == PaymentOrderStatus.Paid && !recordedOrderIds.Contains(o.Id))
            .ToListAsync(cancellationToken);

        if (missingOrders.Count == 0)
        {
            return 0;
        }

        var inserted = 0;
        foreach (var order in missingOrders)
        {
            var payment = await BuildRecordAsync(order, cancellationToken);
            if (payment is null)
            {
                continue;
            }

            _dbContext.CoursePayments.Add(payment);
            inserted++;
        }

        if (inserted > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("PaymentReportService::ReconcileAsync: backfilled {Count} course payment(s)", inserted);
        }

        return inserted;
    }

    public async Task<PagedCoursePaymentsDto> ListAsync(int page, int pageSize, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PaymentReportService::ListAsync: called with Page={Page} PageSize={PageSize} From={From} To={To}", page, pageSize, from, to);

        var query = ApplyDateFilter(_dbContext.CoursePayments.AsNoTracking(), from, to);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalAmount = totalCount == 0 ? 0m : await query.SumAsync(p => p.Amount, cancellationToken);

        var rows = await query
            .OrderByDescending(p => p.PaidAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToRowSelector)
            .ToListAsync(cancellationToken);

        return new PagedCoursePaymentsDto { Items = rows, TotalCount = totalCount, TotalAmount = totalAmount };
    }

    public async Task<IReadOnlyList<CoursePaymentReportRowDto>> GetReportRowsAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PaymentReportService::GetReportRowsAsync: called with From={From} To={To}", from, to);

        return await ApplyDateFilter(_dbContext.CoursePayments.AsNoTracking(), from, to)
            .OrderByDescending(p => p.PaidAt)
            .ThenByDescending(p => p.Id)
            .Select(ToRowSelector)
            .ToListAsync(cancellationToken);
    }

    private async Task<CoursePayment?> BuildRecordAsync(PaymentOrder order, CancellationToken cancellationToken)
    {
        var enrollment = await _dbContext.Enrollments.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == order.EnrollmentId, cancellationToken);
        if (enrollment is null)
        {
            _logger.LogWarning("PaymentReportService: enrollment {EnrollmentId} for order {OrderId} not found", order.EnrollmentId, order.Id);
            return null;
        }

        var course = await _dbContext.Courses.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == enrollment.CourseId, cancellationToken);
        var user = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == enrollment.UserId, cancellationToken);
        if (course is null || user is null)
        {
            _logger.LogWarning("PaymentReportService: course/user for order {OrderId} not found", order.Id);
            return null;
        }

        var promoCode = enrollment.PromoCodeId is { } promoId
            ? await _dbContext.PromoCodes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == promoId, cancellationToken)
            : null;

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return CoursePayment.Record(
            order.Id,
            order.EnrollmentId,
            enrollment.CourseId,
            enrollment.UserId,
            user.Email,
            $"{user.FirstName} {user.LastName}".Trim(),
            course.Title,
            order.Amount,
            promoCode?.Code,
            order.Provider,
            order.ProviderOperationId,
            order.PaidAt ?? now,
            now);
    }

    private static IQueryable<CoursePayment> ApplyDateFilter(IQueryable<CoursePayment> query, DateOnly? from, DateOnly? to)
    {
        if (from is { } fromDate)
        {
            var fromUtc = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(p => p.PaidAt >= fromUtc);
        }

        if (to is { } toDate)
        {
            var toExclusiveUtc = toDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(p => p.PaidAt < toExclusiveUtc);
        }

        return query;
    }

    private static readonly System.Linq.Expressions.Expression<Func<CoursePayment, CoursePaymentReportRowDto>> ToRowSelector = p => new CoursePaymentReportRowDto
    {
        Id = p.Id,
        PaymentOrderId = p.PaymentOrderId,
        EnrollmentId = p.EnrollmentId,
        UserId = p.UserId,
        CourseId = p.CourseId,
        StudentEmail = p.StudentEmail,
        StudentName = p.StudentName,
        CourseTitle = p.CourseTitle,
        Amount = p.Amount,
        PromoCode = p.PromoCode,
        Provider = p.Provider,
        ProviderOperationId = p.ProviderOperationId,
        PaidAt = p.PaidAt,
    };
}
