namespace LF.Application.ModelDto.Payment;

public sealed class PaymentConfirmationDto
{
    public int OrderId { get; init; }
    public int EnrollmentId { get; init; }
    public decimal AmountPaid { get; init; }

    // false when this callback was a replay of an already-settled order (still a success for the caller).
    public bool WasNewlyPaid { get; init; }
}
