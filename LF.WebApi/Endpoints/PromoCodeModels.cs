using FluentValidation;
using LF.AppDomain.Models.Course.Enums;

namespace LF.WebApi.Endpoints;

public sealed record CreatePromoCodeRequest(
    string Code,
    string DiscountType,
    decimal DiscountValue,
    int? CourseId,
    DateTime? ExpiresAt,
    int? MaxRedemptions);

public sealed class CreatePromoCodeRequestValidator : AbstractValidator<CreatePromoCodeRequest>
{
    public CreatePromoCodeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);

        RuleFor(x => x.DiscountType).Must(t => Enum.TryParse<PromoCodeDiscountType>(t, ignoreCase: true, out _))
            .WithMessage("Discount type must be Percentage or FixedAmount.");

        RuleFor(x => x.DiscountValue).InclusiveBetween(1, 100)
            .When(x => string.Equals(x.DiscountType, nameof(PromoCodeDiscountType.Percentage), StringComparison.OrdinalIgnoreCase))
            .WithMessage("A percentage discount must be between 1 and 100.");

        RuleFor(x => x.DiscountValue).GreaterThan(0)
            .When(x => string.Equals(x.DiscountType, nameof(PromoCodeDiscountType.FixedAmount), StringComparison.OrdinalIgnoreCase))
            .WithMessage("A fixed discount must be greater than zero.");

        RuleFor(x => x.CourseId).GreaterThan(0).When(x => x.CourseId is not null);
        RuleFor(x => x.MaxRedemptions).GreaterThan(0).When(x => x.MaxRedemptions is not null);
        RuleFor(x => x.ExpiresAt).GreaterThan(DateTime.UtcNow).When(x => x.ExpiresAt is not null)
            .WithMessage("Expiry must be in the future.");
    }
}

public sealed record PromoCodeResponse(
    int Id,
    string Code,
    string DiscountType,
    decimal DiscountValue,
    int? CourseId,
    string? CourseTitle,
    DateTime? ExpiresAt,
    int? MaxRedemptions,
    int RedemptionCount,
    bool IsActive,
    DateTime CreatedAt);

public sealed record PagedPromoCodesResponse(IReadOnlyList<PromoCodeResponse> Items, int TotalCount, int Page, int PageSize);
