using LF.AppDomain.Models.Course.Enums;

namespace LF.Application.ModelDto.Promo;

public sealed class CreatePromoCodeDto
{
    public string Code { get; init; } = null!;
    public PromoCodeDiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public int? CourseId { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public int? MaxRedemptions { get; init; }
}
