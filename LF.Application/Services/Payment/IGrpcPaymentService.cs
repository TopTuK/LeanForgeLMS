using LF.Application.ModelDto.Payment;

namespace LF.Application.Services.Payment;

// LF.WebApi-side client for LF.PaymentService (implemented in LF.Infrastructure).
public interface IGrpcPaymentService
{
    Task<PaymentOrderDto> CreatePaymentOrderAsync(CreatePaymentOrderDto dto);
    Task<PaymentOrderDto?> GetPaymentOrderAsync(int orderId, int actingUserId);
    Task<PaymentConfirmationDto> ConfirmPaymentAsync(PaymentCallbackDto callback);
    Task<PaymentOrderDto> FailPaymentAsync(int orderId);
}
