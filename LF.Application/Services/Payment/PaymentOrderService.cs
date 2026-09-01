using System.Globalization;
using LF.AppDomain.Entities.Payment;
using LF.AppDomain.Models.Payment.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Payment;

internal sealed class PaymentOrderService(
    ILogger<PaymentOrderService> logger,
    IAppDbContext dbContext,
    IPaymentGateway paymentGateway,
    TimeProvider timeProvider) : IPaymentOrderService
{
    private readonly ILogger<PaymentOrderService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly IPaymentGateway _paymentGateway = paymentGateway;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<PaymentOrderDto> CreateOrderAsync(CreatePaymentOrderDto dto)
    {
        _logger.LogInformation("PaymentOrderService::CreateOrderAsync: called with EnrollmentId={EnrollmentId} UserId={UserId} Amount={Amount}",
            dto.EnrollmentId, dto.UserId, dto.Amount);

        var existing = await _dbContext.PaymentOrders
            .Where(o => o.EnrollmentId == dto.EnrollmentId && o.Status == PaymentOrderStatus.Pending)
            .OrderByDescending(o => o.Id)
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            _logger.LogInformation("PaymentOrderService::CreateOrderAsync: reusing open order Id={OrderId} for EnrollmentId={EnrollmentId}",
                existing.Id, dto.EnrollmentId);
            return ToDto(existing, _paymentGateway.BuildRedirectUrl(existing));
        }

        var order = PaymentOrder.Create(
            dto.EnrollmentId,
            dto.UserId,
            dto.Amount,
            dto.Description,
            _timeProvider.GetUtcNow().UtcDateTime);

        _dbContext.PaymentOrders.Add(order);
        await _dbContext.SaveChangesAsync();

        return ToDto(order, _paymentGateway.BuildRedirectUrl(order));
    }

    public async Task<PaymentOrderDto?> GetOrderAsync(int orderId, int actingUserId)
    {
        _logger.LogInformation("PaymentOrderService::GetOrderAsync: called with OrderId={OrderId} ActingUserId={ActingUserId}", orderId, actingUserId);

        var order = await _dbContext.PaymentOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);

        // A mismatched owner is indistinguishable from "not found" on purpose.
        return order is null || order.UserId != actingUserId ? null : ToDto(order);
    }

    public async Task<PaymentConfirmationDto> ConfirmAsync(PaymentCallbackDto callback)
    {
        _logger.LogInformation("PaymentOrderService::ConfirmAsync: called with InvId={InvId} OutSum={OutSum}", callback.InvId, callback.OutSum);

        if (!_paymentGateway.VerifyResultSignature(callback))
            throw new PaymentSignatureException($"Invalid ResultURL signature for order {callback.InvId}.");

        var order = await _dbContext.PaymentOrders.FirstOrDefaultAsync(o => o.Id == callback.InvId)
            ?? throw new PaymentOrderNotFoundException($"Payment order {callback.InvId} not found.");

        if (!decimal.TryParse(callback.OutSum, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            throw new PaymentAmountMismatchException($"Unparseable callback amount '{callback.OutSum}' for order {callback.InvId}.");

        if (decimal.Round(amount, 2) != order.Amount)
            throw new PaymentAmountMismatchException(
                $"Callback amount {amount} does not match order {order.Id} amount {order.Amount}.");

        var wasNewlyPaid = order.MarkPaid(amount, _timeProvider.GetUtcNow().UtcDateTime);
        if (wasNewlyPaid)
            await _dbContext.SaveChangesAsync();

        return new PaymentConfirmationDto
        {
            OrderId = order.Id,
            EnrollmentId = order.EnrollmentId,
            AmountPaid = order.Amount,
            WasNewlyPaid = wasNewlyPaid,
        };
    }

    public async Task<PaymentOrderDto> MarkFailedAsync(int orderId)
    {
        _logger.LogInformation("PaymentOrderService::MarkFailedAsync: called with OrderId={OrderId}", orderId);

        var order = await _dbContext.PaymentOrders.FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new PaymentOrderNotFoundException($"Payment order {orderId} not found.");

        if (order.Status == PaymentOrderStatus.Pending)
        {
            order.MarkFailed();
            await _dbContext.SaveChangesAsync();
        }

        return ToDto(order);
    }

    private static PaymentOrderDto ToDto(PaymentOrder order, string? paymentUrl = null) => new()
    {
        Id = order.Id,
        EnrollmentId = order.EnrollmentId,
        UserId = order.UserId,
        Amount = order.Amount,
        Description = order.Description,
        Status = order.Status,
        CreatedAt = order.CreatedAt,
        PaidAt = order.PaidAt,
        PaymentUrl = paymentUrl,
    };
}
