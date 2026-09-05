using LF.Application.ModelDto.Payment;

namespace LF.Application.Services.Payment;

public interface IPaymentOrderService
{
    Task<PaymentOrderDto> CreateOrderAsync(CreatePaymentOrderDto dto);
    Task<PaymentOrderDto?> GetOrderAsync(int orderId, int actingUserId);
    Task<PaymentConfirmationDto> ConfirmAsync(PaymentCallbackDto callback);
    Task<PaymentOrderDto> MarkFailedAsync(int orderId);
}
