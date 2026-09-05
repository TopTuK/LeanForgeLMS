using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LF.Application.Common.Exceptions;
using LF.Application.ModelDto.Payment;
using LF.Application.Services.Payment;

namespace LF.PaymentService.Services;

public class RpcPaymentService(ILogger<RpcPaymentService> logger, IPaymentOrderService paymentOrderService)
    : PaymentServiceRpc.PaymentServiceRpcBase
{
    private readonly ILogger<RpcPaymentService> _logger = logger;
    private readonly IPaymentOrderService _paymentOrderService = paymentOrderService;

    public override async Task<PaymentOrderReply> CreatePaymentOrder(CreatePaymentOrderRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcPaymentService::CreatePaymentOrder: called with EnrollmentId={EnrollmentId} UserId={UserId}",
            request.EnrollmentId, request.UserId);

        var dto = new CreatePaymentOrderDto
        {
            EnrollmentId = request.EnrollmentId,
            UserId = request.UserId,
            Amount = decimal.Parse(request.Amount, CultureInfo.InvariantCulture),
            Description = request.Description,
        };

        try
        {
            var order = await _paymentOrderService.CreateOrderAsync(dto);
            return ToReply(order);
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<PaymentOrderReply> GetPaymentOrder(GetPaymentOrderRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcPaymentService::GetPaymentOrder: called with OrderId={OrderId} ActingUserId={ActingUserId}",
            request.OrderId, request.ActingUserId);

        var order = await _paymentOrderService.GetOrderAsync(request.OrderId, request.ActingUserId)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Payment order {request.OrderId} not found."));

        return ToReply(order);
    }

    public override async Task<ConfirmPaymentReply> ConfirmPayment(ConfirmPaymentRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcPaymentService::ConfirmPayment: called with InvId={InvId} OutSum={OutSum}", request.InvId, request.OutSum);

        var callback = new PaymentCallbackDto
        {
            OutSum = request.OutSum,
            InvId = request.InvId,
            SignatureValue = request.SignatureValue,
            ShpParams = request.ShpParams.ToDictionary(kv => kv.Key, kv => kv.Value),
        };

        try
        {
            var confirmation = await _paymentOrderService.ConfirmAsync(callback);
            return new ConfirmPaymentReply
            {
                OrderId = confirmation.OrderId,
                EnrollmentId = confirmation.EnrollmentId,
                AmountPaid = confirmation.AmountPaid.ToString(CultureInfo.InvariantCulture),
                WasNewlyPaid = confirmation.WasNewlyPaid,
            };
        }
        catch (PaymentSignatureException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (PaymentOrderNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (PaymentAmountMismatchException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    public override async Task<PaymentOrderReply> FailPayment(FailPaymentRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcPaymentService::FailPayment: called with OrderId={OrderId}", request.OrderId);

        try
        {
            var order = await _paymentOrderService.MarkFailedAsync(request.OrderId);
            return ToReply(order);
        }
        catch (PaymentOrderNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static PaymentOrderReply ToReply(PaymentOrderDto dto)
    {
        var reply = new PaymentOrderReply
        {
            Id = dto.Id,
            EnrollmentId = dto.EnrollmentId,
            UserId = dto.UserId,
            Amount = dto.Amount.ToString(CultureInfo.InvariantCulture),
            Description = dto.Description,
            Status = (PaymentOrderStatus)(int)dto.Status,
            CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(dto.CreatedAt, DateTimeKind.Utc)),
            PaymentUrl = dto.PaymentUrl ?? string.Empty,
        };

        if (dto.PaidAt is { } paidAt)
            reply.PaidAt = Timestamp.FromDateTime(DateTime.SpecifyKind(paidAt, DateTimeKind.Utc));

        return reply;
    }
}
