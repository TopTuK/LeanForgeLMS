using LF.AppDomain.Models.Payment.Enums;

namespace LF.Application.ModelDto.Payment;

public sealed class PaymentOrderDto
{
    public int Id { get; init; }
    public int EnrollmentId { get; init; }
    public int UserId { get; init; }
    public decimal Amount { get; init; }
    public string Description { get; init; } = null!;
    public PaymentOrderStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? PaidAt { get; init; }

    // Only populated on the create-order response — the provider checkout URL the browser redirects to.
    public string? PaymentUrl { get; init; }
}
