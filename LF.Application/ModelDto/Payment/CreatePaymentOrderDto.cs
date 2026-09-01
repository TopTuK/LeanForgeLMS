namespace LF.Application.ModelDto.Payment;

public sealed class CreatePaymentOrderDto
{
    public int EnrollmentId { get; init; }
    public int UserId { get; init; }
    public decimal Amount { get; init; }
    public string Description { get; init; } = null!;
}
