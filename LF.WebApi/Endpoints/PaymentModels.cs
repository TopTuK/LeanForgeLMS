using FluentValidation;

namespace LF.WebApi.Endpoints;

public sealed record CheckoutRequest(int CourseId, string? PromoCode = null);

public sealed class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutRequestValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0);
        RuleFor(x => x.PromoCode).MaximumLength(64).When(x => !string.IsNullOrEmpty(x.PromoCode));
    }
}

// PaymentUrl is null when the enrollment needs no payment (free course / already active) — the SPA
// then goes straight to the course instead of redirecting to the provider.
public sealed record CheckoutResponse(int EnrollmentId, int? OrderId, string? PaymentUrl, string Status);

public sealed record PaymentOrderResponse(
    int Id,
    int EnrollmentId,
    decimal Amount,
    string Status,
    DateTime CreatedAt,
    DateTime? PaidAt);
