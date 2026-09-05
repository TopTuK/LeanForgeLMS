using System.Globalization;
using Grpc.Core;
using LF.Application.Common.Exceptions;
using LF.Application.ModelDto.Payment;
using LF.Application.Services.Payment;
using LF.PaymentService;
using Microsoft.Extensions.Logging;
using AppPaymentOrderStatus = LF.AppDomain.Models.Payment.Enums.PaymentOrderStatus;

namespace LF.Infrastructure.Services.Payment;

internal sealed class GrpcPaymentService(
    ILogger<GrpcPaymentService> logger,
    PaymentServiceRpc.PaymentServiceRpcClient paymentServiceRpcClient) : IGrpcPaymentService
{
    private readonly ILogger<GrpcPaymentService> _logger = logger;
    private readonly PaymentServiceRpc.PaymentServiceRpcClient _paymentServiceRpcClient = paymentServiceRpcClient;

    public async Task<PaymentOrderDto> CreatePaymentOrderAsync(CreatePaymentOrderDto dto)
    {
        _logger.LogInformation("GrpcPaymentService::CreatePaymentOrderAsync: called with EnrollmentId={EnrollmentId} UserId={UserId}",
            dto.EnrollmentId, dto.UserId);

        var request = new CreatePaymentOrderRequest
        {
            EnrollmentId = dto.EnrollmentId,
            UserId = dto.UserId,
            Amount = dto.Amount.ToString(CultureInfo.InvariantCulture),
            Description = dto.Description,
        };

        try
        {
            var reply = await _paymentServiceRpcClient.CreatePaymentOrderAsync(request);
            return ToDto(reply);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
        {
            throw new ArgumentException(ex.Status.Detail);
        }
    }

    public async Task<PaymentOrderDto?> GetPaymentOrderAsync(int orderId, int actingUserId)
    {
        _logger.LogInformation("GrpcPaymentService::GetPaymentOrderAsync: called with OrderId={OrderId} ActingUserId={ActingUserId}", orderId, actingUserId);

        try
        {
            var reply = await _paymentServiceRpcClient.GetPaymentOrderAsync(new GetPaymentOrderRequest { OrderId = orderId, ActingUserId = actingUserId });
            return ToDto(reply);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<PaymentConfirmationDto> ConfirmPaymentAsync(PaymentCallbackDto callback)
    {
        _logger.LogInformation("GrpcPaymentService::ConfirmPaymentAsync: called with InvId={InvId}", callback.InvId);

        var request = new ConfirmPaymentRequest
        {
            OutSum = callback.OutSum,
            InvId = callback.InvId,
            SignatureValue = callback.SignatureValue,
        };
        foreach (var (key, value) in callback.ShpParams)
            request.ShpParams[key] = value;

        try
        {
            var reply = await _paymentServiceRpcClient.ConfirmPaymentAsync(request);
            return new PaymentConfirmationDto
            {
                OrderId = reply.OrderId,
                EnrollmentId = reply.EnrollmentId,
                AmountPaid = decimal.Parse(reply.AmountPaid, CultureInfo.InvariantCulture),
                WasNewlyPaid = reply.WasNewlyPaid,
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            throw new PaymentSignatureException(ex.Status.Detail);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw new PaymentOrderNotFoundException(ex.Status.Detail);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.FailedPrecondition)
        {
            throw new PaymentAmountMismatchException(ex.Status.Detail);
        }
    }

    public async Task<PaymentOrderDto> FailPaymentAsync(int orderId)
    {
        _logger.LogInformation("GrpcPaymentService::FailPaymentAsync: called with OrderId={OrderId}", orderId);

        try
        {
            var reply = await _paymentServiceRpcClient.FailPaymentAsync(new FailPaymentRequest { OrderId = orderId });
            return ToDto(reply);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw new PaymentOrderNotFoundException(ex.Status.Detail);
        }
    }

    private static PaymentOrderDto ToDto(PaymentOrderReply reply) => new()
    {
        Id = reply.Id,
        EnrollmentId = reply.EnrollmentId,
        UserId = reply.UserId,
        Amount = string.IsNullOrEmpty(reply.Amount) ? 0m : decimal.Parse(reply.Amount, CultureInfo.InvariantCulture),
        Description = reply.Description,
        Status = (AppPaymentOrderStatus)(int)reply.Status,
        CreatedAt = reply.CreatedAt?.ToDateTime() ?? default,
        PaidAt = reply.PaidAt?.ToDateTime(),
        PaymentUrl = string.IsNullOrEmpty(reply.PaymentUrl) ? null : reply.PaymentUrl,
    };
}
